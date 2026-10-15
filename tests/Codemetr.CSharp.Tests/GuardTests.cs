namespace Codemetr.CSharp.Tests;

public sealed class GuardTests
{
    static string Branches(int count) =>
        "class C { int M(int a) { " + string.Join(" ", Enumerable.Range(0, count).Select(i => $"if (a == {i}) return {i};")) + " return -1; } }";

    [Fact]
    public void LimitsAreInclusive()
    {
        var config = new CodemetrConfig { Rules = new GuardRules { MaxCyclomaticComplexity = 5 } };
        Assert.Empty(GuardEvaluator.Evaluate(Source.Analyze(Branches(4)).Types, config));
        var violation = Assert.Single(GuardEvaluator.Evaluate(Source.Analyze(Branches(5)).Types, config));
        Assert.Equal((RuleIds.CyclomaticComplexity, "M(int)", 6d, 5d), (violation.Rule, violation.Member, violation.Value, violation.Limit));
        Assert.NotEmpty(violation.Hint);

        var fields = new CodemetrConfig { Rules = new GuardRules { MaxInstanceFields = 2 } };
        Assert.Empty(GuardEvaluator.Evaluate(Source.Analyze("class F { int a, b; }").Types, fields));
        Assert.Single(GuardEvaluator.Evaluate(Source.Analyze("class F { int a, b, c; }").Types, fields));
    }

    [Fact]
    public void ReportsTypeRulesMaintainabilityAndBannedSuffix()
    {
        var config = new CodemetrConfig { Rules = new GuardRules { MaxInstanceFields = 1, MinMaintainabilityIndex = 101 } };
        var violations = GuardEvaluator.Evaluate(Source.Analyze("class OrderManager { int a, b; }").Types, config);
        Assert.Equal([RuleIds.BannedTypeNameSuffix, RuleIds.InstanceFields, RuleIds.MaintainabilityIndex], violations.Select(v => v.Rule).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void OverridesApplyByFileGlobAndTypeAndLaterOnesWin()
    {
        using var dir = new TempDirectory();
        var path = dir.Write("codemetr.json", """
            {
              // comments are allowed
              "rules": { "maxCyclomaticComplexity": 3 },
              "overrides": [
                { "files": "legacy/**", "rules": { "maxCyclomaticComplexity": 10 } },
                { "type": "Strict", "rules": { "maxCyclomaticComplexity": 1 } }
              ],
              "exclude": ["**/Generated/**"]
            }
            """);
        var config = CodemetrConfig.Load(path);
        var types = CodeAnalyzer.Analyze([new SourceDocument("legacy/Old.cs", Branches(5)), new SourceDocument("src/New.cs", Branches(5).Replace("class C", "class N")),
            new SourceDocument("legacy/Strict.cs", "class Strict { void M(bool b) { if (b) { } } }")]).Types;
        Assert.Equal(["N", "Strict"], GuardEvaluator.Evaluate(types, config).Select(v => v.Type).Order(StringComparer.Ordinal));
        Assert.Equal(["**/Generated/**"], config.Exclude);
    }

    [Fact]
    public void ConfigurationRejectsUnknownMembersAndEmptyOverrides()
    {
        using var dir = new TempDirectory();
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => CodemetrConfig.Load(dir.Write("a.json", """{ "rules": { "maxComplexity": 3 } }""")));
        Assert.Throws<InvalidDataException>(() => CodemetrConfig.Load(dir.Write("b.json", """{ "overrides": [ { "rules": { } } ] }""")));
    }
}
