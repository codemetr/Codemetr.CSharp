namespace Codemetr.CSharp;

/// <summary>Measured types plus documents that could not be measured.</summary>
public sealed record AnalysisResult(IReadOnlyList<TypeMetrics> Types, IReadOnlyList<ParseError> ParseErrors, int DocumentCount)
{
    /// <summary>Restricts the result to types whose simple name, full name or full-name suffix equals <paramref name="query"/>.</summary>
    public AnalysisResult SelectTypes(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return this;
        var selected = Types.Where(type => type.Name == query || type.FullName == query
            || type.FullName.EndsWith("." + query, StringComparison.Ordinal)).ToArray();
        return this with { Types = selected };
    }
}
