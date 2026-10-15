using Codemetr.CSharp;

namespace Codemetr.CSharp.Tool;

/// <summary>Builds the report document from an analysis and its baseline comparison.</summary>
internal static class ReportBuilder
{
    /// <summary>Report format version: fields are only added within one schema; rule ids are never renamed.</summary>
    public const int ReportSchema = 1;

    static string StaleMessage(StaleEntry item) => item switch
    {
        { Unmeasured: true } => "Recorded file exists but is no longer measured (excluded, generated header or skipped); measure it again or have the owner re-create the baseline.",
        { MovedTo: { } target } => $"Recorded violation moved to {target}; run 'codemetr-csharp baseline' to update its path.",
        { CurrentValue: null } => "Recorded violation is fixed; run 'codemetr-csharp baseline' to remove it.",
        _ => "Recorded violation improved; run 'codemetr-csharp baseline' to tighten it.",
    };

    public static CodemetrReport Build(string command, CodemetrRequest request, FileSelection selection, AnalysisResult result,
        BaselineComparison comparison, string? written, List<string> warnings)
    {
        var parseErrors = result.ParseErrors;
        var rows = comparison.Violations.Select(item => new ReportViolation(item.Violation.Rule, item.Status, item.Violation.Type,
            item.Violation.Member, item.Violation.File, item.Violation.Line, item.Violation.Value, item.Violation.Limit,
            item.BaselineValue, item.Violation.Message, item.Violation.Hint)).ToArray();
        var stale = comparison.Stale.Select(item => new ReportStale(item.Entry.Rule, item.Entry.Type, item.Entry.Member, item.Entry.File,
            item.Entry.Value, item.CurrentValue, StaleMessage(item), item.MovedTo)).ToArray();
        var summary = new ReportSummary(selection.Documents.Count, result.Types.Count, rows.Length,
            rows.Count(r => r.Status == CheckedViolation.New), rows.Count(r => r.Status == CheckedViolation.Worsened),
            rows.Count(r => r.Status == CheckedViolation.Baselined), stale.Length, parseErrors.Count, selection.Skipped.Count);
        var passed = parseErrors.Count == 0 && comparison.Passed;
        var includeTypes = command == CodemetrService.Analyze || request.IncludeMetrics;
        return new CodemetrReport(ReportSchema, "codemetr-csharp", CodemetrService.Version, command, passed, summary, rows, stale, parseErrors,
            Hotspots(result.Types, request.Top), includeTypes ? result.Types : null, written, warnings.Count > 0 ? warnings : null);
    }

    // Hotspot order: complexity first, then coupling, inheritance, maintainability, size.
    static IReadOnlyList<string> Hotspots(IEnumerable<TypeMetrics> types, int top) => types
        .OrderByDescending(t => t.MaxCyclomaticComplexity).ThenByDescending(t => t.ClassCoupling)
        .ThenByDescending(t => t.DepthOfInheritance).ThenBy(t => t.MaintainabilityIndex).ThenByDescending(t => t.Lines)
        .ThenBy(t => t.FullName, StringComparer.Ordinal).Take(Math.Max(0, top))
        .Select(t => $"{t.File}:{t.Line} {t.FullName}: complexity {t.MaxCyclomaticComplexity} ({(t.MostComplexMember.Length == 0 ? "n/a" : t.MostComplexMember)}), "
            + $"maintainability {t.MaintainabilityIndex}, coupling {t.ClassCoupling}, inheritance {t.DepthOfInheritance}, lines {t.Lines}")
        .ToArray();
}
