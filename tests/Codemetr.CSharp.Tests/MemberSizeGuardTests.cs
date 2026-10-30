using System.Text.Json;
using Codemetr.CSharp.Tool;

namespace Codemetr.CSharp.Tests;

public sealed class MemberSizeGuardTests
{
    [Fact]
    public void RulesAreDisabledByDefaultAndInclusiveWhenSelected()
    {
        var type = Source.Analyze("class C { void M() { int x = 0; x++; } }").Types;
        Assert.Null(GuardRules.Default.MaxMemberCodeLines);
        Assert.Null(GuardRules.Default.MaxMemberStatements);
        Assert.Empty(GuardEvaluator.Evaluate(type, new CodemetrConfig()));
        Assert.Empty(GuardEvaluator.Evaluate(type, new CodemetrConfig { Rules = new() { MaxMemberCodeLines = 1, MaxMemberStatements = 2 } }));
        var violations = GuardEvaluator.Evaluate(type, new CodemetrConfig { Rules = new() { MaxMemberCodeLines = 0, MaxMemberStatements = 1 } });
        Assert.Equal(new[] { "memberCodeLines", "memberStatements" }, violations.Select(v => v.Rule));
        Assert.All(violations, v => { Assert.Equal("M()", v.Member); Assert.NotEmpty(v.Hint); });
    }

    [Fact]
    public void FileOverridesRetainPartialMemberProvenance()
    {
        using var dir = new TempDirectory();
        var config = CodemetrConfig.Load(dir.Write("codemetr.json", """
            { "rules": { "maxMemberStatements": 0, "maxMemberCodeLines": 0 },
              "overrides": [{ "files": "allowed.cs", "rules": { "maxMemberStatements": 2, "maxMemberCodeLines": 1 } }] }
            """));
        var result = CodeAnalyzer.Analyze([
            new SourceDocument("allowed.cs", "partial class C { void M() { int x = 0; x++; } }"),
            new SourceDocument("strict.cs", "partial class C { void N() { int x = 0; x++; } }")]);
        var rows = GuardEvaluator.EvaluateWhole(result.WholeTypes, config);
        Assert.Equal(2, rows.Count);
        Assert.All(rows, v => { Assert.Equal("strict.cs", v.File); Assert.Equal("N()", v.Member); });
        Assert.All(result.WholeTypes.Single().Members, m => Assert.Equal((1, 2), (m.CodeLines, m.StatementCount)));
    }

    [Fact]
    public async Task OptionalRuleSupportsTheExistingBaselineRatchet()
    {
        using var dir = new TempDirectory();
        dir.Write("codemetr.json", """{ "rules": { "maxMemberStatements": 1 } }""");
        dir.Write("a.cs", "class C { void M() { int x = 0; x++; } }");
        Assert.Equal(1, (await Run(dir.Path, "check")).Exit);
        Assert.Equal(0, (await Run(dir.Path, "baseline")).Exit);
        dir.Write("a.cs", "class C { void M() { int x = 0; x++; x++; } }");
        var worse = await Run(dir.Path, "check");
        Assert.Equal(1, worse.Exit);
        using (var json = JsonDocument.Parse(worse.Text))
            Assert.Equal("worsened", json.RootElement.GetProperty("violations")[0].GetProperty("status").GetString());
        Assert.Equal(1, (await Run(dir.Path, "baseline")).Exit);
        dir.Write("a.cs", "class C { void M() { } }");
        var fixedResult = await Run(dir.Path, "check");
        Assert.Equal(1, fixedResult.Exit);
        using var fixedJson = JsonDocument.Parse(fixedResult.Text);
        Assert.Single(fixedJson.RootElement.GetProperty("stale").EnumerateArray());
        Assert.Equal(0, (await Run(dir.Path, "baseline")).Exit);
        Assert.Equal(0, (await Run(dir.Path, "check")).Exit);
    }

    static async Task<(int Exit, string Text)> Run(string root, string command)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = await Program.RunAsync([command, "--root", root], new StringReader(""), output, error);
        return (exit, output.ToString());
    }
}
