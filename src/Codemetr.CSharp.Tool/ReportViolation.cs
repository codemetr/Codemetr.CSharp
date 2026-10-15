namespace Codemetr.CSharp.Tool;

/// <summary>Flat violation row for JSON consumers.</summary>
internal sealed record ReportViolation(string Rule, string Status, string Type, string? Member, string File, int Line,
    double Value, double Limit, double? BaselineValue, string Message, string Hint);
