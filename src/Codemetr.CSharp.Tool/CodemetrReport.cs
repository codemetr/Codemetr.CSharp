namespace Codemetr.CSharp.Tool;

/// <summary>JSON document returned by every command.</summary>
internal sealed record CodemetrReport(
    int Schema,
    string Tool,
    string Version,
    string Command,
    bool Passed,
    ReportSummary Summary,
    IReadOnlyList<ReportViolation> Violations,
    IReadOnlyList<ReportStale> Stale,
    IReadOnlyList<Codemetr.CSharp.ParseError> ParseErrors,
    IReadOnlyList<string> Hotspots,
    IReadOnlyList<Codemetr.CSharp.TypeMetrics>? Types,
    string? BaselineWritten = null,
    IReadOnlyList<string>? Warnings = null,
    IReadOnlyList<Codemetr.CSharp.TypeMetrics>? WholeTypes = null,
    Codemetr.CSharp.AnalysisCompleteness? Completeness = null,
    Codemetr.CSharp.DependencyGraph? Dependencies = null,
    IReadOnlyList<Codemetr.CSharp.CohesionMetrics>? Cohesion = null,
    MutationReport? Mutation = null);
