using System.Text.Json;

namespace Codemetr.CSharp;

/// <summary>
/// Ratchet of existing debt (<c>codemetr.baseline.json</c>). Recorded violations may stay or improve;
/// new or worsened ones fail, and fixed, improved or moved ones ask for the baseline to be tightened.
/// Once a baseline exists it is only ever tightened (<see cref="Tighten"/>), never extended.
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

    /// <summary>First baseline of a code base: every current violation becomes tolerated debt.</summary>
    public static Baseline Initial(IEnumerable<Violation> violations) =>
        new(CurrentSchema, Sorted(violations.Select(v => new BaselineEntry(v.Rule, v.Type, v.Member, v.File, v.Value))));

    /// <summary>
    /// Applies the stale entries of <paramref name="comparison"/>: fixed entries are removed, improved values
    /// lowered and moved entries re-pointed. Nothing is ever added or raised, so following the tool's advice
    /// to update the baseline can only shrink the tolerated debt.
    /// </summary>
    public Baseline Tighten(BaselineComparison comparison)
    {
        var entries = Entries.ToList();
        foreach (var stale in comparison.Stale)
        {
            var index = entries.IndexOf(stale.Entry);
            if (index < 0 || stale.Unmeasured) continue;
            if (stale.CurrentValue is null) entries.RemoveAt(index);
            else entries[index] = stale.Entry with { Value = stale.CurrentValue.Value, File = stale.MovedTo ?? stale.Entry.File };
        }
        return new(CurrentSchema, Sorted(entries));
    }

    static BaselineEntry[] Sorted(IEnumerable<BaselineEntry> entries) => entries
        .OrderBy(entry => entry.File, StringComparer.Ordinal).ThenBy(entry => entry.Key, StringComparer.Ordinal)
        .ThenBy(entry => entry.Value).ToArray();

    /// <summary>
    /// Classifies current violations against this baseline, limited to <paramref name="scope"/>.
    /// Entries sharing one key (for example two same-named local functions) are matched as a multiset,
    /// worst current value against worst recorded value, so no violation can hide behind another.
    /// A violation that is new only because its file was renamed is matched to the recorded entry when the
    /// recorded file no longer exists (<paramref name="fileExists"/>), rule, type and member are equal and
    /// the value is not worse; the entry is then reported as moved.
    /// </summary>
    public BaselineComparison Compare(IReadOnlyList<Violation> violations, AnalysisScope scope, Func<string, bool>? fileExists = null)
    {
        var findings = new List<(int Index, CheckedViolation Finding)>();
        var stale = new List<StaleEntry>();
        var vanished = new List<BaselineEntry>();
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
                findings.Add((position, new(violation, Worse(violation.Rule, value, before) ? CheckedViolation.Worsened : CheckedViolation.Baselined, before)));
                if (Worse(violation.Rule, before, value)) stale.Add(new(previous[index], value));
            }
            vanished.AddRange(previous.Skip(current.Length));
        }
        var violated = violations.Select(v => v.Key).ToHashSet(StringComparer.Ordinal);
        vanished.AddRange(recorded.Where(group => !violated.Contains(group.Key)).SelectMany(group => group));
        // Only a proven fix may remove debt: unmeasured files fail, unverifiable ones stay as they are.
        var unverified = new List<BaselineEntry>();
        foreach (var entry in vanished)
        {
            switch (scope.Judge(entry.File, entry.Type, fileExists))
            {
                case EntryJudgement.Fixed: stale.Add(new StaleEntry(entry, null)); break;
                case EntryJudgement.Unmeasured: stale.Add(new StaleEntry(entry, null, Unmeasured: true)); break;
                default: unverified.Add(entry); break;
            }
        }
        if (fileExists is not null) MatchMoves(findings, stale, unverified, fileExists);
        return new(findings.OrderBy(item => item.Index).Select(item => item.Finding).ToArray(),
            stale.OrderBy(s => s.Entry.Key, StringComparer.Ordinal).ThenBy(s => s.Entry.Value).ToArray()) { Unverified = unverified };
    }

    static bool Worse(string rule, double value, double than) => RuleIds.LowerIsWorse(rule) ? value < than : value > than;

    // Every file with top-level statements reports type Program: such entries carry no identity to follow.
    const string AnonymousEntryType = "Program";

    static void MatchMoves(List<(int Index, CheckedViolation Finding)> findings, List<StaleEntry> stale, List<BaselineEntry> unverified, Func<string, bool> fileExists)
    {
        bool Moves(BaselineEntry entry, Violation violation) => entry.Rule == violation.Rule && entry.Type == violation.Type
            && entry.Member == violation.Member && entry.File != violation.File
            && !Worse(violation.Rule, violation.Value, entry.Value) && !fileExists(entry.File);
        for (var index = 0; index < findings.Count; index++)
        {
            var (position, finding) = findings[index];
            var violation = finding.Violation;
            if (finding.Status != CheckedViolation.New || violation.Type == AnonymousEntryType) continue;
            BaselineEntry entry;
            var fixedOrigin = stale.FindIndex(candidate => candidate is { CurrentValue: null, MovedTo: null, Unmeasured: false } && Moves(candidate.Entry, violation));
            if (fixedOrigin >= 0) { entry = stale[fixedOrigin].Entry; stale.RemoveAt(fixedOrigin); }
            else
            {
                // A deleted file in a file-list run is unverified; the rename target in the same run settles it.
                var unverifiedOrigin = unverified.FindIndex(candidate => Moves(candidate, violation));
                if (unverifiedOrigin < 0) continue;
                entry = unverified[unverifiedOrigin];
                unverified.RemoveAt(unverifiedOrigin);
            }
            findings[index] = (position, new(violation, CheckedViolation.Baselined, entry.Value));
            stale.Add(new StaleEntry(entry, violation.Value, violation.File));
        }
    }
}
