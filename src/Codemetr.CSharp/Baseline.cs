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
        if (baseline.Entries.Any(entry => entry is null || entry.Rule is null || entry.Type is null || entry.File is null))
            throw new InvalidDataException("Incomplete codemetr baseline entry");
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
            .OrderBy(entry => entry.File, StringComparer.Ordinal).ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .ThenBy(entry => entry.Value).ToArray());
    }

    /// <summary>
    /// Classifies current violations against this baseline, limited to <paramref name="scope"/>.
    /// Entries sharing one key (for example two same-named local functions) are matched as a multiset,
    /// worst current value against worst recorded value, so no violation can hide behind another.
    /// </summary>
    public BaselineComparison Compare(IReadOnlyList<Violation> violations, AnalysisScope scope)
    {
        var findings = new List<(int Index, CheckedViolation Finding)>();
        var stale = new List<StaleEntry>();
        var recorded = Entries.Where(entry => scope.Contains(entry.File, entry.Type))
            .ToLookup(entry => entry.Key, StringComparer.Ordinal);
        foreach (var group in violations.Select((violation, index) => (Violation: violation, Index: index)).GroupBy(item => item.Violation.Key, StringComparer.Ordinal))
        {
            var lowerIsWorse = RuleIds.LowerIsWorse(group.First().Violation.Rule);
            var current = (lowerIsWorse ? group.OrderBy(item => item.Violation.Value) : group.OrderByDescending(item => item.Violation.Value)).ToArray();
            var previous = (lowerIsWorse ? recorded[group.Key].OrderBy(e => e.Value) : recorded[group.Key].OrderByDescending(e => e.Value)).ToArray();
            for (var index = 0; index < current.Length; index++)
            {
                var (violation, position) = current[index];
                if (index >= previous.Length) { findings.Add((position, new(violation, CheckedViolation.New, null))); continue; }
                var (value, before) = (violation.Value, previous[index].Value);
                var worse = lowerIsWorse ? value < before : value > before;
                var better = lowerIsWorse ? value > before : value < before;
                findings.Add((position, new(violation, worse ? CheckedViolation.Worsened : CheckedViolation.Baselined, before)));
                if (better) stale.Add(new(previous[index], value));
            }
            stale.AddRange(previous.Skip(current.Length).Select(entry => new StaleEntry(entry, null)));
        }
        var violated = violations.Select(v => v.Key).ToHashSet(StringComparer.Ordinal);
        stale.AddRange(recorded.Where(group => !violated.Contains(group.Key)).SelectMany(group => group).Select(entry => new StaleEntry(entry, null)));
        return new(findings.OrderBy(item => item.Index).Select(item => item.Finding).ToArray(),
            stale.OrderBy(s => s.Entry.Key, StringComparer.Ordinal).ThenBy(s => s.Entry.Value).ToArray());
    }
}
