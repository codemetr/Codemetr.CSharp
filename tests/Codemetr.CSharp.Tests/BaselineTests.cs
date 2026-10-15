namespace Codemetr.CSharp.Tests;

public sealed class BaselineTests
{
    static Violation V(string rule, string type, double value, string file = "a.cs", string? member = "M") =>
        new(rule, type, member, file, 1, value, 5, "x");

    static readonly AnalysisScope AllOfA = new(["a.cs"], null);

    [Fact]
    public void ClassifiesNewWorsenedAndBaselined()
    {
        var baseline = Baseline.Empty.Record([V(RuleIds.CyclomaticComplexity, "T", 8), V(RuleIds.CyclomaticComplexity, "U", 8)], AllOfA);
        var comparison = baseline.Compare([V(RuleIds.CyclomaticComplexity, "T", 8), V(RuleIds.CyclomaticComplexity, "U", 9), V(RuleIds.Parameters, "T", 9)], AllOfA);
        Assert.Equal([CheckedViolation.Baselined, CheckedViolation.Worsened, CheckedViolation.New], comparison.Violations.Select(v => v.Status));
        Assert.False(comparison.Passed);
        Assert.True(baseline.Compare([V(RuleIds.CyclomaticComplexity, "T", 8), V(RuleIds.CyclomaticComplexity, "U", 8)], AllOfA).Passed);
    }

    [Fact]
    public void FixedOrImprovedEntriesAreStaleSoTheRatchetTightens()
    {
        var baseline = Baseline.Empty.Record([V(RuleIds.CyclomaticComplexity, "T", 8), V(RuleIds.MaintainabilityIndex, "U", 10, member: null)], AllOfA);
        var comparison = baseline.Compare([V(RuleIds.MaintainabilityIndex, "U", 12, member: null)], AllOfA);
        Assert.Equal([(RuleIds.CyclomaticComplexity, (double?)null), (RuleIds.MaintainabilityIndex, 12d)],
            comparison.Stale.Select(s => (s.Entry.Rule, s.CurrentValue)).OrderBy(s => s.Rule, StringComparer.Ordinal));
        Assert.False(comparison.Passed);
    }

    [Fact]
    public void StalenessAndRecordingStayInsideTheAnalysedScope()
    {
        var baseline = Baseline.Empty.Record([V(RuleIds.CyclomaticComplexity, "T", 8, "a.cs"), V(RuleIds.CyclomaticComplexity, "Other", 8, "b.cs")],
            new AnalysisScope(["a.cs", "b.cs"], null));
        Assert.True(baseline.Compare([V(RuleIds.CyclomaticComplexity, "T", 8, "a.cs")], AllOfA).Passed);
        Assert.True(baseline.Compare([], new AnalysisScope(["a.cs"], ["Unrelated"])).Passed);
        var rerecorded = baseline.Record([], AllOfA);
        Assert.Equal(["Other"], rerecorded.Entries.Select(e => e.Type));
    }

    [Fact]
    public void LoadRejectsUnknownSchemaAndDuplicates()
    {
        using var dir = new TempDirectory();
        Assert.Throws<InvalidDataException>(() => Baseline.Load(dir.Write("a.json", """{ "schema": 2, "entries": [] }""")));
        var entry = """{ "rule": "parameters", "type": "T", "member": "M", "file": "a.cs", "value": 9 }""";
        Assert.Throws<InvalidDataException>(() => Baseline.Load(dir.Write("b.json", $$"""{ "schema": 1, "entries": [ {{entry}}, {{entry}} ] }""")));
    }
}
