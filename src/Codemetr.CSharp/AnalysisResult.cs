namespace Codemetr.CSharp;

/// <summary>Measured types plus documents that could not be measured.</summary>
public sealed record AnalysisResult(IReadOnlyList<TypeMetrics> Types, IReadOnlyList<ParseError> ParseErrors, int DocumentCount)
{
    /// <summary>Whole measurable partial types; declaration-level Types remains unchanged.</summary>
    public IReadOnlyList<TypeMetrics> WholeTypes { get; init; } = [];
    /// <summary>Source-only analysis limits, including unresolved references.</summary>
    public AnalysisCompleteness? Completeness { get; init; }

    /// <summary>Advisory, scope-labelled dependency graph.</summary>
    public DependencyGraph? Dependencies { get; init; }
    /// <summary>Advisory instance-state cohesion.</summary>
    public IReadOnlyList<CohesionMetrics> Cohesion { get; init; } = [];

    /// <summary>Conditions that make metrics less reliable without preventing them, such as duplicate type declarations.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>Measured documents that contain inactive <c>#if</c> code, whose absence of a violation proves nothing.</summary>
    public IReadOnlyList<string> FilesWithInactiveCode { get; init; } = [];

    /// <summary>Restricts the result to types whose simple name, full name or full-name suffix equals <paramref name="query"/>.</summary>
    public AnalysisResult SelectTypes(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return this;
        var selected = Types.Where(type => TypeName.Matches(type, query, allowSuffix: true)).ToArray();
        var whole = WholeTypes.Where(type => TypeName.Matches(type, query, allowSuffix: true)).ToArray();
        return this with { Types = selected, WholeTypes = whole,
            Dependencies = Dependencies is null ? null : DependencyGraph.Build(whole,
                Dependencies.Types.Select(node => node.Type).Concat(Dependencies.Types.SelectMany(node => node.ContextTypes)).ToArray()),
            Cohesion = Cohesion.Where(item => whole.Any(type => type.FullName == item.Type)).ToArray() };
    }
}
