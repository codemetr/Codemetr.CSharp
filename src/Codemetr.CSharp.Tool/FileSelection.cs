namespace Codemetr.CSharp.Tool;

/// <summary>
/// What a run measures: the analysed paths, the measured documents, listed files that no longer exist,
/// and files skipped as generated or excluded (counted so that hidden code stays visible in the report).
/// </summary>
internal sealed record FileSelection(IReadOnlyList<string> Paths, IReadOnlyList<SourceDocument> Documents,
    IReadOnlyList<string> MissingListed, IReadOnlyCollection<string> Skipped);
