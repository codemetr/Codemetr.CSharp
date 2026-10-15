namespace Codemetr.CSharp.Tests;

public sealed class BaselineTests
{
    static Violation V(string rule, string type, double value, string file = "a.cs", string? member = "M") =>
        new(rule, type, member, file, 1, value, 5, "x");

    static readonly AnalysisScope AllOfA = new(["a.cs"], null);

    [Fact]
    public void ClassifiesNewWorsenedAndBaselined()
    {
        var baseline = Baseline.Initial([V(RuleIds.CyclomaticComplexity, "T", 8), V(RuleIds.CyclomaticComplexity, "U", 8)]);
        var comparison = baseline.Compare([V(RuleIds.CyclomaticComplexity, "T", 8), V(RuleIds.CyclomaticComplexity, "U", 9), V(RuleIds.Parameters, "T", 9)], AllOfA);
        Assert.Equal([CheckedViolation.Baselined, CheckedViolation.Worsened, CheckedViolation.New], comparison.Violations.Select(v => v.Status));
        Assert.False(comparison.Passed);
        Assert.True(baseline.Compare([V(RuleIds.CyclomaticComplexity, "T", 8), V(RuleIds.CyclomaticComplexity, "U", 8)], AllOfA).Passed);
    }

    [Fact]
    public void FixedOrImprovedEntriesAreStaleSoTheRatchetTightens()
    {
        var baseline = Baseline.Initial([V(RuleIds.CyclomaticComplexity, "T", 8), V(RuleIds.MaintainabilityIndex, "U", 10, member: null)]);
        var comparison = baseline.Compare([V(RuleIds.MaintainabilityIndex, "U", 12, member: null)], AllOfA);
        Assert.Equal([(RuleIds.CyclomaticComplexity, (double?)null), (RuleIds.MaintainabilityIndex, 12d)],
            comparison.Stale.Select(s => (s.Entry.Rule, s.CurrentValue)).OrderBy(s => s.Rule, StringComparer.Ordinal));
        Assert.False(comparison.Passed);
    }

    [Fact]
    public void StalenessAndRecordingStayInsideTheAnalysedScope()
    {
        var baseline = Baseline.Initial([V(RuleIds.CyclomaticComplexity, "T", 8, "a.cs"), V(RuleIds.CyclomaticComplexity, "Other", 8, "b.cs")]);
        Assert.True(baseline.Compare([V(RuleIds.CyclomaticComplexity, "T", 8, "a.cs")], AllOfA).Passed);
        Assert.True(baseline.Compare([], new AnalysisScope(["a.cs"], ["Unrelated"])).Passed);
        var rerecorded = baseline.Tighten(baseline.Compare([], AllOfA));
        Assert.Equal(["Other"], rerecorded.Entries.Select(e => e.Type));
    }

    [Fact]
    public void LoadRejectsUnknownSchemaAndIncompleteEntries()
    {
        using var dir = new TempDirectory();
        Assert.Throws<InvalidDataException>(() => Baseline.Load(dir.Write("a.json", """{ "schema": 2, "entries": [] }""")));
        Assert.Throws<InvalidDataException>(() => Baseline.Load(dir.Write("b.json", """{ "schema": 1, "entries": [ null ] }""")));
        Assert.Throws<InvalidDataException>(() => Baseline.Load(dir.Write("c.json", """{ "schema": 1, "entries": [ { "rule": "parameters", "file": "a.cs", "value": 9 } ] }""")));
    }

    [Fact]
    public void SharedKeysAreMatchedAsAMultisetSoNoViolationHidesBehindAnother()
    {
        // Two same-named local functions in one method share a key.
        var baseline = Baseline.Initial([V(RuleIds.CyclomaticComplexity, "T", 9), V(RuleIds.CyclomaticComplexity, "T", 6)]);
        Assert.Equal(2, baseline.Entries.Length);
        Assert.True(baseline.Compare([V(RuleIds.CyclomaticComplexity, "T", 6), V(RuleIds.CyclomaticComplexity, "T", 9)], AllOfA).Passed);
        var grown = baseline.Compare([V(RuleIds.CyclomaticComplexity, "T", 9), V(RuleIds.CyclomaticComplexity, "T", 9)], AllOfA);
        Assert.Equal([CheckedViolation.Baselined, CheckedViolation.Worsened], grown.Violations.Select(v => v.Status));
        var third = baseline.Compare([V(RuleIds.CyclomaticComplexity, "T", 9), V(RuleIds.CyclomaticComplexity, "T", 6), V(RuleIds.CyclomaticComplexity, "T", 6)], AllOfA);
        Assert.Single(third.Violations, v => v.Status == CheckedViolation.New);
    }

    [Fact]
    public void EntriesBelowAnAnalysedDirectoryBecomeStaleWhenTheirFileIsGone()
    {
        var baseline = Baseline.Initial([V(RuleIds.CyclomaticComplexity, "Gone", 8, "src/Gone.cs"), V(RuleIds.CyclomaticComplexity, "Kept", 8, "lib/Kept.cs")]);
        var comparison = baseline.Compare([], new AnalysisScope([], null, directories: ["src"]));
        Assert.Equal(["Gone"], comparison.Stale.Select(s => s.Entry.Type));
        Assert.True(baseline.Compare([], new AnalysisScope(["src/Gone.cs"], null, unparsed: ["src/Gone.cs"])).Passed);
    }

    [Fact]
    public void TheRootScopeDoesNotReachOutsideTheRoot()
    {
        var baseline = Baseline.Initial([V(RuleIds.CyclomaticComplexity, "L", 8, "../lib/L.cs")]);
        Assert.True(baseline.Compare([], new AnalysisScope([], null, directories: [""])).Passed);
    }

    [Fact]
    public void TighteningRemovesFixedLowersImprovedAndNeverAdds()
    {
        var baseline = Baseline.Initial([V(RuleIds.CyclomaticComplexity, "Fixed", 8), V(RuleIds.CyclomaticComplexity, "Better", 9), V(RuleIds.CyclomaticComplexity, "Same", 7)]);
        var comparison = baseline.Compare([V(RuleIds.CyclomaticComplexity, "Better", 6), V(RuleIds.CyclomaticComplexity, "Same", 7), V(RuleIds.CyclomaticComplexity, "Fresh", 9)], AllOfA);
        var tightened = baseline.Tighten(comparison);
        Assert.Equal([("Better", 6d), ("Same", 7d)], tightened.Entries.Select(e => (e.Type, e.Value)).OrderBy(e => e.Type, StringComparer.Ordinal));
    }

    [Fact]
    public void AMovedFileKeepsItsDebtOnlyWhenTheOldFileIsGoneAndTheValueIsNotWorse()
    {
        var baseline = Baseline.Initial([V(RuleIds.CyclomaticComplexity, "T", 8, "old/A.cs")]);
        var scope = new AnalysisScope(["old/A.cs", "new/A.cs"], null);
        var moved = baseline.Compare([V(RuleIds.CyclomaticComplexity, "T", 8, "new/A.cs")], scope, fileExists: path => path != "old/A.cs");
        Assert.Equal(CheckedViolation.Baselined, Assert.Single(moved.Violations).Status);
        Assert.Equal("new/A.cs", Assert.Single(moved.Stale).MovedTo);
        Assert.Equal("new/A.cs", Assert.Single(baseline.Tighten(moved).Entries).File);

        var worse = baseline.Compare([V(RuleIds.CyclomaticComplexity, "T", 9, "new/A.cs")], scope, fileExists: path => path != "old/A.cs");
        Assert.Equal(CheckedViolation.New, Assert.Single(worse.Violations).Status);
        var copied = baseline.Compare([V(RuleIds.CyclomaticComplexity, "T", 8, "new/A.cs")], scope, fileExists: _ => true);
        Assert.Equal(CheckedViolation.New, Assert.Single(copied.Violations).Status);
    }
}
