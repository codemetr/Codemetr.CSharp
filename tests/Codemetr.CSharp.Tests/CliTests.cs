using System.Text.Json;
using Codemetr.CSharp.Tool;

namespace Codemetr.CSharp.Tests;

public sealed class CliTests
{
    const string Complex = "class Complex { int M(int a) { if (a == 1) return 1; if (a == 2) return 2; if (a == 3) return 3; return 0; } }";

    static async Task<(int Exit, string Out, string Err)> Run(string stdin, params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = await Program.RunAsync(args, new StringReader(stdin), output, error);
        return (exit, output.ToString(), error.ToString());
    }

    static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement;

    [Fact]
    public async Task CheckExitCodesFollowTheGuardConvention()
    {
        using var dir = new TempDirectory();
        dir.Write("src/Simple.cs", "class Simple { }");
        dir.Write("src/Complex.cs", Complex);
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 } }""");

        var clean = await Run("", "check", "src/Simple.cs", "--root", dir.Path);
        Assert.Equal(0, clean.Exit);
        Assert.True(Json(clean.Out).GetProperty("passed").GetBoolean());

        var failing = await Run("", "check", "--root", dir.Path);
        Assert.Equal(1, failing.Exit);
        var violation = Json(failing.Out).GetProperty("violations")[0];
        Assert.Equal("cyclomaticComplexity", violation.GetProperty("rule").GetString());
        Assert.Equal("new", violation.GetProperty("status").GetString());
        Assert.False(string.IsNullOrEmpty(violation.GetProperty("hint").GetString()));

        dir.Write("src/Broken.cs", "class Broken {");
        Assert.Equal(2, (await Run("", "check", "--root", dir.Path)).Exit);
        Assert.Equal(2, (await Run("", "check", "missing", "--root", dir.Path)).Exit);
        Assert.Equal(2, (await Run("", "check", "--bogus")).Exit);
    }

    [Fact]
    public async Task BaselineRatchetsDebtAndAsksToTightenAfterAFix()
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 } }""");
        dir.Write("src/Complex.cs", Complex);
        dir.Write("src/Other.cs", Complex.Replace("class Complex", "class Other"));

        Assert.Equal(1, (await Run("", "check", "--root", dir.Path)).Exit);
        var recorded = await Run("", "baseline", "--root", dir.Path);
        Assert.Equal(0, recorded.Exit);
        Assert.True(File.Exists(Path.Combine(dir.Path, "codemetr.baseline.json")));
        Assert.Equal(0, (await Run("", "check", "--root", dir.Path)).Exit);

        // A changed-files run sees only Complex.cs; Other.cs's debt is neither stale nor dropped.
        dir.Write("src/Complex.cs", "class Complex { int M(int a) => a; }");
        var partial = await Run("src/Complex.cs\nREADME.md\n", "check", "--files-from", "-", "--root", dir.Path, "--format", "text");
        Assert.Equal(1, partial.Exit);
        Assert.Contains("[stale]", partial.Out);
        Assert.DoesNotContain("Other", partial.Out);

        Assert.Equal(0, (await Run("src/Complex.cs\n", "baseline", "--files-from", "-", "--root", dir.Path)).Exit);
        var baseline = Baseline.Load(Path.Combine(dir.Path, "codemetr.baseline.json"));
        Assert.Equal(["Other"], baseline.Entries.Select(e => e.Type));
        Assert.Equal(0, (await Run("", "check", "--root", dir.Path)).Exit);
    }

    [Fact]
    public async Task AnalyzeReportsMetricsForASelectedType()
    {
        using var dir = new TempDirectory();
        dir.Write("A.cs", "namespace N; class Target { void M(bool b) { if (b) { } } } class Other { }");
        var result = await Run("", "analyze", "--type", "Target", "--root", dir.Path);
        Assert.Equal(0, result.Exit);
        var type = Assert.Single(Json(result.Out).GetProperty("types").EnumerateArray());
        Assert.Equal("N.Target", type.GetProperty("fullName").GetString());
        Assert.Equal(2, type.GetProperty("maxCyclomaticComplexity").GetInt32());
        Assert.Equal(2, (await Run("", "analyze", "--type", "Nope", "--root", dir.Path)).Exit);
    }

    [Fact]
    public async Task TypeSelectionNeitherDropsNorStalesTheDebtOfOtherTypes()
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 } }""");
        dir.Write("src/Both.cs", Complex.Replace("class Complex", "class A") + "\n" + Complex.Replace("class Complex", "class B"));
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        Assert.Equal(0, (await Run("", "check", "--type", "A", "--root", dir.Path)).Exit);
        Assert.Equal(0, (await Run("", "baseline", "--type", "A", "--root", dir.Path)).Exit);
        Assert.Equal(["A", "B"], Baseline.Load(Path.Combine(dir.Path, "codemetr.baseline.json")).Entries.Select(e => e.Type).Order(StringComparer.Ordinal));
        Assert.Equal(0, (await Run("", "check", "--root", dir.Path)).Exit);
    }

    [Fact]
    public async Task OverloadsKeepSeparateBaselineEntries()
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 } }""");
        const string twoOverloads = "class O { int M(int a) { if (a == 1) return 1; if (a == 2) return 2; if (a == 3) return 3; return 0; } int M(string s) => s.Length; }";
        dir.Write("O.cs", twoOverloads);
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        dir.Write("O.cs", twoOverloads.Replace("int M(string s) => s.Length;", "int M(string s) { if (s == \"a\") return 1; if (s == \"b\") return 2; if (s == \"c\") return 3; return 0; }"));
        var grown = await Run("", "check", "--root", dir.Path);
        Assert.Equal(1, grown.Exit);
        var violation = Json(grown.Out).GetProperty("violations").EnumerateArray().Single(v => v.GetProperty("status").GetString() == "new");
        Assert.Equal("M(string)", violation.GetProperty("member").GetString());
    }

    [Fact]
    public async Task DeletedFilesMakeTheirEntriesStaleAndEmptyFileListsCheckNothing()
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 } }""");
        var file = dir.Write("src/Complex.cs", Complex);
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        Assert.Equal(0, (await Run("", "check", "--files-from", "-", "--root", dir.Path)).Exit);
        Assert.Equal(0, Json((await Run("", "check", "--files-from", "-", "--root", dir.Path)).Out).GetProperty("summary").GetProperty("files").GetInt32());
        File.Delete(file);
        Assert.Contains("[stale]", (await Run("", "check", "--root", dir.Path, "--format", "text")).Out);
        // A file list may lack the rename target: the deleted file's entry stays until a full run settles it.
        var listed = await Run("src\\Complex.cs\n", "check", "--files-from", "-", "--root", dir.Path, "--format", "text");
        Assert.Equal(0, listed.Exit);
        Assert.Contains("could not be verified", listed.Out);
    }

    [Fact]
    public async Task WrongRootForAFileListIsReportedAsAWarning()
    {
        using var dir = new TempDirectory();
        dir.Write("src/A.cs", "class A { }");
        var result = await Run("src/A.cs\n", "check", "--files-from", "-", "--root", Path.Combine(dir.Path, "src"));
        Assert.Contains("--root", Json(result.Out).GetProperty("warnings")[0].GetString());
        Assert.Contains("warning", result.Err);
    }

    [Fact]
    public async Task MalformedInputsExitWithTwoInsteadOfCrashing()
    {
        using var dir = new TempDirectory();
        dir.Write("A.cs", "class A { }");
        dir.Write("codemetr.json", """{ "overrides": null }""");
        Assert.Equal(2, (await Run("", "check", "--root", dir.Path)).Exit);
        dir.Write("codemetr.json", "{ }");
        dir.Write("codemetr.baseline.json", """{ "schema": 1, "entries": [ null ] }""");
        Assert.Equal(2, (await Run("", "check", "--root", dir.Path)).Exit);
        File.Delete(Path.Combine(dir.Path, "codemetr.baseline.json"));
        Assert.Equal(2, (await Run("", "check", "--files-from", dir.Path, "--root", dir.Path)).Exit);
    }

    [Fact]
    public async Task ReportKeepsGenericTypeNamesReadable()
    {
        using var dir = new TempDirectory();
        dir.Write("A.cs", "class Repo<T> { }");
        Assert.Contains("\"Repo<T>\"", (await Run("", "analyze", "--root", dir.Path)).Out);
    }

    [Fact]
    public async Task ConfiguredContextKeepsAnotherCodeTreeOutOfTypeResolution()
    {
        using var dir = new TempDirectory();
        dir.Write("app/Repo.cs", "namespace App; public class Repo { public static Repo Open() => new(); }");
        dir.Write("app/Service.cs", "namespace App; class Service { void M() { var r = Repo.Open(); } }");
        dir.Write("legacy/Repo.cs", "namespace App; public class Repo { }");
        string Coupling(string output) => string.Join(",", Json(output).GetProperty("types").EnumerateArray()
            .Single(t => t.GetProperty("name").GetString() == "Service").GetProperty("coupledTypes").EnumerateArray().Select(e => e.GetString()));
        dir.Write("codemetr.json", """{ "context": ["app"] }""");
        Assert.Equal("App.Repo", Coupling((await Run("", "analyze", "app/Service.cs", "--root", dir.Path)).Out));
        Assert.Equal("App.Repo", Coupling((await Run("app/Service.cs\n", "analyze", "--files-from", "-", "--root", dir.Path)).Out));
        dir.Write("codemetr.json", """{ "context": ["missing"] }""");
        Assert.Equal(2, (await Run("", "analyze", "--root", dir.Path)).Exit);
    }

    [Fact]
    public async Task AnEmptyFileListWithATypeFilterIsNotAnError()
    {
        using var dir = new TempDirectory();
        dir.Write("A.cs", "class A { }");
        Assert.Equal(0, (await Run("", "check", "--files-from", "-", "--type", "A", "--root", dir.Path)).Exit);
    }

    [Fact]
    public async Task AnExistingBaselineOnlyTightensAndRefusesNewDebt()
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 } }""");
        dir.Write("src/A.cs", Complex.Replace("class Complex", "class A"));
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        var path = Path.Combine(dir.Path, "codemetr.baseline.json");
        var before = File.ReadAllText(path);
        dir.Write("src/B.cs", Complex.Replace("class Complex", "class B"));
        var refused = await Run("", "baseline", "--root", dir.Path, "--format", "text");
        Assert.Equal(1, refused.Exit);
        Assert.Contains("only tightens", refused.Out);
        Assert.Equal(before, File.ReadAllText(path));
        Assert.Equal(1, (await Run("", "check", "--root", dir.Path)).Exit);
    }

    [Fact]
    public async Task ARenamedFileHealsThroughABaselineUpdateWithoutAcceptingNewDebt()
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 } }""");
        var old = dir.Write("src/A.cs", Complex);
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        File.Move(old, Path.Combine(dir.Path, "src/Moved.cs"));
        var check = await Run("src/A.cs\nsrc/Moved.cs\n", "check", "--files-from", "-", "--root", dir.Path, "--format", "text");
        Assert.Equal(1, check.Exit);
        Assert.Contains("moved to src/Moved.cs", check.Out);
        Assert.Contains("0 new", check.Out);
        Assert.Equal(0, (await Run("src/A.cs\nsrc/Moved.cs\n", "baseline", "--files-from", "-", "--root", dir.Path)).Exit);
        Assert.Equal(["src/Moved.cs"], Baseline.Load(Path.Combine(dir.Path, "codemetr.baseline.json")).Entries.Select(e => e.File));
        Assert.Equal(0, (await Run("", "check", "--root", dir.Path)).Exit);
    }

    [Fact]
    public async Task OnlyRealGeneratedHeadersExcludeAFile()
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 } }""");
        dir.Write("src/G.cs", "// [GeneratedCode] mentioned in a comment\n" + Complex);
        Assert.Equal(1, (await Run("", "check", "--root", dir.Path)).Exit);
        dir.Write("src/G.cs", "// <auto-generated/>\n" + Complex);
        Assert.Equal(0, (await Run("", "check", "--root", dir.Path)).Exit);
    }

    [Fact]
    public async Task InactivePreprocessorCodeAndQuotedPathsAreNotSilentlyIgnored()
    {
        using var dir = new TempDirectory();
        dir.Write("src/P.cs", "class P { int M(int a) {\n#if NET10_0_OR_GREATER\nif (a == 1) return 1;\n#endif\nreturn 0; } }");
        var result = await Run("", "check", "--root", dir.Path);
        Assert.Contains("inactive #if", Json(result.Out).GetProperty("warnings")[0].GetString());
        Assert.Equal(1, Json(result.Out).GetProperty("schema").GetInt32());
        Assert.Equal(2, (await Run("\"src/\\303\\234.cs\"\n", "check", "--files-from", "-", "--root", dir.Path)).Exit);
    }

    [Fact]
    public async Task TextOutputNamesTheRecordedValueAndAnalyzeDoesNotClaimFailure()
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 } }""");
        dir.Write("src/A.cs", Complex);
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        dir.Write("src/A.cs", Complex.Replace("return 3;", "return 3; if (a == 4) return 4;"));
        var check = await Run("", "check", "--root", dir.Path, "--format", "text");
        Assert.Contains("[worsened from 4]", check.Out);
        var analyze = await Run("", "analyze", "--root", dir.Path, "--format", "text");
        Assert.StartsWith("codemetr-csharp analyze: 1 violation(s)", analyze.Out);
        Assert.Equal(0, analyze.Exit);
    }

    [Theory]
    [InlineData("header")]
    [InlineData("suffix")]
    [InlineData("exclude")]
    [InlineData("headerAndRename")]
    public async Task TakingAFileWithDebtOutOfTheMeasurementFailsAndKeepsTheDebt(string how)
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 } }""");
        var file = dir.Write("src/A.cs", Complex);
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        var baselinePath = Path.Combine(dir.Path, "codemetr.baseline.json");
        var before = File.ReadAllText(baselinePath);
        switch (how)
        {
            case "header": dir.Write("src/A.cs", "// <auto-generated/>\n" + Complex); break;
            case "suffix": File.Move(file, Path.Combine(dir.Path, "src/A.g.cs")); break;
            case "headerAndRename": dir.Write("src/A.cs", "// <auto-generated/>\n" + Complex.Replace("class Complex", "class Renamed")); break;
            default: dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 }, "exclude": ["src/A.cs"] }"""); break;
        }
        var check = await Run("", "check", "--root", dir.Path, "--format", "text");
        Assert.Equal(1, check.Exit);
        Assert.Contains("no longer measured", check.Out);
        Assert.Equal(1, (await Run("", "baseline", "--root", dir.Path)).Exit);
        Assert.Equal(before, File.ReadAllText(baselinePath));
    }

    [Fact]
    public async Task DebtHiddenInInactivePreprocessorCodeStaysInTheBaseline()
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 } }""");
        dir.Write("src/A.cs", Complex);
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        dir.Write("src/A.cs", "class Complex {\n#if NET10_0_OR_GREATER\n" + Complex["class Complex {".Length..^1] + "\n#endif\n}");
        var check = await Run("", "check", "--root", dir.Path, "--format", "text");
        Assert.Contains("could not be verified", check.Out);
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        Assert.Single(Baseline.Load(Path.Combine(dir.Path, "codemetr.baseline.json")).Entries);
    }

    [Fact]
    public async Task TopLevelProgramsAreNeverTakenForAMovedFile()
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 2 } }""");
        var old = dir.Write("tools/Old/Program.cs", "var a = args.Length; if (a > 1) return 1; if (a > 2) return 2; return 0;");
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        File.Delete(old);
        dir.Write("tools/New/Program.cs", "var b = args.Length; if (b > 5) return 5; if (b > 6) return 6; return 0;");
        var check = await Run("", "check", "--root", dir.Path, "--format", "text");
        Assert.Contains("[new]", check.Out);
        Assert.Equal(1, (await Run("", "baseline", "--root", dir.Path)).Exit);
    }

    [Fact]
    public async Task ARenameListedWithOnlyTheOldPathKeepsTheDebtForTheFullRun()
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 } }""");
        var old = dir.Write("src/A.cs", Complex);
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        File.Move(old, Path.Combine(dir.Path, "src/Moved.cs"));
        Assert.Equal(0, (await Run("src/A.cs\n", "baseline", "--files-from", "-", "--root", dir.Path)).Exit);
        Assert.Equal(["src/A.cs"], Baseline.Load(Path.Combine(dir.Path, "codemetr.baseline.json")).Entries.Select(e => e.File));
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        Assert.Equal(["src/Moved.cs"], Baseline.Load(Path.Combine(dir.Path, "codemetr.baseline.json")).Entries.Select(e => e.File));
    }

    [Fact]
    public async Task MovingAMemberIntoAGeneratedPartialPartDoesNotRemoveItsDebt()
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 } }""");
        const string body = "if (a == 1) return 1; if (a == 2) return 2; if (a == 3) return 3; return 0;";
        dir.Write("src/A.cs", "partial class A { int M(int a) { " + body + " } }");
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        dir.Write("src/A.cs", "partial class A { }");
        dir.Write("src/A.Impl.cs", "// <auto-generated/>\npartial class A { int M(int a) { " + body + " } }");
        var check = await Run("", "check", "--root", dir.Path, "--format", "text");
        Assert.Equal(1, check.Exit);
        Assert.Contains("no longer measured", check.Out);
        Assert.Contains("1 skipped", check.Out);
        Assert.Equal(1, (await Run("", "baseline", "--root", dir.Path)).Exit);
        Assert.Single(Baseline.Load(Path.Combine(dir.Path, "codemetr.baseline.json")).Entries);
    }

    [Theory]
    [InlineData("src/Sub/obj/A.cs", "")]
    [InlineData("gen/A.cs", "// <auto-generated/>\n")]
    public async Task MovingATypeOutOfSightDoesNotRemoveItsDebt(string target, string header)
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 }, "context": ["src"] }""");
        var file = dir.Write("src/A.cs", Complex);
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        File.Delete(file);
        dir.Write(target, header + Complex);
        Assert.Equal(1, (await Run("", "check", "--root", dir.Path)).Exit);
        Assert.Equal(1, (await Run("", "baseline", "--root", dir.Path)).Exit);
        Assert.Single(Baseline.Load(Path.Combine(dir.Path, "codemetr.baseline.json")).Entries);
    }

    [Fact]
    public async Task ADeliberateExcludeDoesNotBlockUnrelatedTightening()
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 } }""");
        dir.Write("src/A.cs", Complex.Replace("class Complex", "class A"));
        dir.Write("src/B.cs", Complex.Replace("class Complex", "class B"));
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 }, "exclude": ["src/A.cs"] }""");
        dir.Write("src/B.cs", "class B { int M(int a) => a; }");
        Assert.Equal(1, (await Run("", "baseline", "--root", dir.Path)).Exit);
        Assert.Equal(["A"], Baseline.Load(Path.Combine(dir.Path, "codemetr.baseline.json")).Entries.Select(e => e.Type));
    }

    [Fact]
    public async Task ATypeLevelEntryFollowsItsTypeIntoBuildOutput()
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxInstanceFields": 1 } }""");
        const string fields = "class Wide { int a, b, c; }";
        var file = dir.Write("src/Wide.cs", fields);
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        File.Delete(file);
        dir.Write("src/obj/Wide.cs", fields);
        Assert.Equal(1, (await Run("", "check", "--root", dir.Path)).Exit);
        Assert.Single(Baseline.Load(Path.Combine(dir.Path, "codemetr.baseline.json")).Entries);
    }

    [Fact]
    public async Task ATypeThatMovedAndWasFixedOnTheWayIsFixed()
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxCyclomaticComplexity": 3 } }""");
        var file = dir.Write("src/A.cs", Complex);
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        File.Delete(file);
        dir.Write("src/B.cs", "class Complex { int M(int a) => a; }");
        Assert.Contains("is fixed", (await Run("", "check", "--root", dir.Path, "--format", "text")).Out);
        Assert.Equal(0, (await Run("", "baseline", "--root", dir.Path)).Exit);
        Assert.Empty(Baseline.Load(Path.Combine(dir.Path, "codemetr.baseline.json")).Entries);
    }
}
