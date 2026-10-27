namespace Codemetr.CSharp;

/// <summary>Limits of this source-only analysis; no project/NuGet completeness is implied.</summary>
public sealed record AnalysisCompleteness(
    string Scope,
    int CompilationErrorCount,
    IReadOnlyList<string> CompilationErrorIds,
    IReadOnlyList<string> UnresolvedTypes,
    IReadOnlyList<MetricDeclaration> UnmeasuredPartialDeclarations,
    IReadOnlyList<string> InactiveFiles)
{
    /// <summary>All context files with known unmeasured code, even if no declaration can safely be attributed to a selected type.</summary>
    public IReadOnlyList<AnalysisFileLimit> ContextFilesWithLimits { get; init; } = [];
}
