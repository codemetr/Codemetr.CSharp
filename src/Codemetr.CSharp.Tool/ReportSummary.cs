namespace Codemetr.CSharp.Tool;

/// <summary>Counts of a run.</summary>
/// <remarks><c>Skipped</c> counts files left out as generated or excluded; code there is never measured.</remarks>
internal sealed record ReportSummary(int Files, int Types, int Violations, int New, int Worsened, int Baselined, int Stale, int ParseErrors, int Skipped);
