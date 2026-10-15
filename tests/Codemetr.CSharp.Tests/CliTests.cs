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
}
