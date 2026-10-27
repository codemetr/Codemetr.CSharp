namespace Codemetr.CSharp;

/// <summary>Limits of this source-only analysis; no project/NuGet completeness is implied.</summary>
public sealed record AnalysisCompleteness(
    string Scope,
    int SemanticErrorCount,
    IReadOnlyList<string> SemanticErrorIds,
    IReadOnlyList<string> UnresolvedTypes,
    IReadOnlyList<MetricDeclaration> UnmeasuredPartialDeclarations,
    IReadOnlyList<string> InactiveFiles);
