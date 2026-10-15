namespace Codemetr.CSharp;

/// <summary>Which files and types a run actually looked at; baseline staleness is judged only inside it.</summary>
public sealed class AnalysisScope(IEnumerable<string> files, IEnumerable<string>? types)
{
    readonly HashSet<string> files = new(files, StringComparer.Ordinal);
    readonly HashSet<string>? types = types is null ? null : new(types, StringComparer.Ordinal);

    /// <summary>Scope of an analysis result after an optional type selection.</summary>
    /// <remarks>Documents with parse errors are outside the scope: their recorded debt is neither dropped nor reported stale.</remarks>
    public static AnalysisScope Of(IReadOnlyList<SourceDocument> documents, AnalysisResult result, bool typeSelected)
    {
        var unparsed = result.ParseErrors.Select(error => error.File).ToHashSet(StringComparer.Ordinal);
        return new(documents.Select(d => d.Path).Where(path => !unparsed.Contains(path)),
            typeSelected ? result.Types.Select(t => t.FullName) : null);
    }

    /// <summary>True when the entry's file was analysed and, with a type selection, its type was selected.</summary>
    public bool Contains(string file, string type) => files.Contains(file) && (types is null || types.Contains(type));
}
