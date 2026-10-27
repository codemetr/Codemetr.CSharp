namespace Codemetr.CSharp;

/// <summary>A context file whose syntax errors or inactive branches can hide declarations.</summary>
public sealed record AnalysisFileLimit(string File, bool HasSyntaxErrors, bool HasInactiveCode);
