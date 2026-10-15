using System.Text.Json;

namespace Codemetr.CSharp;

/// <summary>
/// Ratchet of existing debt (<c>codemetr.baseline.json</c>). Recorded violations may stay or improve;
/// new or worsened ones fail, and fixed or improved ones ask for the baseline to be tightened.
/// </summary>
public sealed record Baseline(int Schema, BaselineEntry[] Entries)
{
    /// <summary>Conventional file name looked up in the analysis root.</summary>
    public const string FileName = "codemetr.baseline.json";
    /// <summary>Current file format.</summary>
    public const int CurrentSchema = 1;

    /// <summary>An empty baseline.</summary>
    public static Baseline Empty { get; } = new(CurrentSchema, []);

    /// <summary>Loads and validates a baseline file.</summary>
    public static Baseline Load(string path)
    {
        var baseline = JsonSerializer.Deserialize<Baseline>(File.ReadAllBytes(path), CodemetrJson.Options)
            ?? throw new InvalidDataException("Empty codemetr baseline");
        if (baseline.Schema != CurrentSchema || baseline.Entries is null)
            throw new InvalidDataException($"Unsupported codemetr baseline schema {baseline.Schema}");
        if (baseline.Entries.Select(entry => entry.Key).Distinct(StringComparer.Ordinal).Count() != baseline.Entries.Length)
            throw new InvalidDataException("Duplicate codemetr baseline entry");
        return baseline;
    }

    /// <summary>
    /// Replaces the entries inside <paramref name="scope"/> with the current violations and keeps all others,
    /// so that recording a subset of files never drops the debt of files that were not analysed.
    /// </summary>
    public Baseline Record(IEnumerable<Violation> violations, AnalysisScope scope)
    {
        var kept = Entries.Where(entry => !scope.Contains(entry.File, entry.Type));
        var current = violations.Select(v => new BaselineEntry(v.Rule, v.Type, v.Member, v.File, v.Value));
        return new(CurrentSchema, kept.Concat(current)
            .OrderBy(entry => entry.File, StringComparer.Ordinal).ThenBy(entry => entry.Key, StringComparer.Ordinal).ToArray());
    }

    /// <summary>Classifies current violations against this baseline, limited to <paramref name="scope"/>.</summary>
    public BaselineComparison Compare(IReadOnlyList<Violation> violations, AnalysisScope scope)
    {
        var recorded = Entries.ToDictionary(entry => entry.Key, StringComparer.Ordinal);
        var findings = new List<CheckedViolation>();
        var stale = new List<StaleEntry>();
        foreach (var violation in violations)
        {
            if (!recorded.TryGetValue(violation.Key, out var entry)) { findings.Add(new(violation, CheckedViolation.New, null)); continue; }
            var worse = RuleIds.LowerIsWorse(violation.Rule) ? violation.Value < entry.Value : violation.Value > entry.Value;
            var better = RuleIds.LowerIsWorse(violation.Rule) ? violation.Value > entry.Value : violation.Value < entry.Value;
            findings.Add(new(violation, worse ? CheckedViolation.Worsened : CheckedViolation.Baselined, entry.Value));
            if (better) stale.Add(new(entry, violation.Value));
        }
        var current = violations.Select(v => v.Key).ToHashSet(StringComparer.Ordinal);
        stale.AddRange(Entries.Where(entry => scope.Contains(entry.File, entry.Type) && !current.Contains(entry.Key))
            .Select(entry => new StaleEntry(entry, null)));
        return new(findings, stale.OrderBy(s => s.Entry.Key, StringComparer.Ordinal).ToArray());
    }
}
