namespace Codemetr.CSharp.Tool;

/// <summary>Baseline entry that should be tightened: fixed (currentValue null) or improved.</summary>
internal sealed record ReportStale(string Rule, string Type, string? Member, string File, double BaselineValue, double? CurrentValue, string Message, string? MovedTo = null);
