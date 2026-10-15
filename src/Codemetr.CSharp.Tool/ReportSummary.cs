namespace Codemetr.CSharp.Tool;

/// <summary>Counts of a run.</summary>
internal sealed record ReportSummary(int Files, int Types, int Violations, int New, int Worsened, int Baselined, int Stale, int ParseErrors);
