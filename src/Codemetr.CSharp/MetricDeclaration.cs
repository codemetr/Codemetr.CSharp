namespace Codemetr.CSharp;

/// <summary>A contributing declaration; excluded parts are disclosed but not treated as measured.</summary>
public sealed record MetricDeclaration(string File, int Line, bool Measured, int StartOffset = 0);
