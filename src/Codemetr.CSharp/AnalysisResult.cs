namespace Codemetr.CSharp;

/// <summary>Measured types plus documents that could not be measured.</summary>
public sealed record AnalysisResult(IReadOnlyList<TypeMetrics> Types, IReadOnlyList<ParseError> ParseErrors, int DocumentCount)
{
    /// <summary>Conditions that make metrics less reliable without preventing them, such as duplicate type declarations.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>Measured documents that contain inactive <c>#if</c> code, whose absence of a violation proves nothing.</summary>
    public IReadOnlyList<string> FilesWithInactiveCode { get; init; } = [];

    /// <summary>Full names of types declared only in context documents that were not measured (generated, excluded).</summary>
    public IReadOnlySet<string> UnmeasuredTypes { get; init; } = new HashSet<string>();

    /// <summary>Restricts the result to types whose simple name, full name or full-name suffix equals <paramref name="query"/>.</summary>
    public AnalysisResult SelectTypes(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return this;
        var selected = Types.Where(type => TypeName.Matches(type, query, allowSuffix: true)).ToArray();
        return this with { Types = selected };
    }
}
