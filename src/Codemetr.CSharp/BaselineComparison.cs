namespace Codemetr.CSharp;

/// <summary>Result of comparing violations with a baseline.</summary>
public sealed record BaselineComparison(IReadOnlyList<CheckedViolation> Violations, IReadOnlyList<StaleEntry> Stale)
{
    /// <summary>Recorded entries this run could not verify; they stay in the baseline untouched.</summary>
    public IReadOnlyList<BaselineEntry> Unverified { get; init; } = [];

    /// <summary>True when nothing is new or worsened and the baseline needs no tightening.</summary>
    public bool Passed => Stale.Count == 0 && Violations.All(v => !v.Fails);
}
