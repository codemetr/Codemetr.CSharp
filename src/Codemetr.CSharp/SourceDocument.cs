namespace Codemetr.CSharp;

/// <summary>One C# source text with the path it is reported under.</summary>
/// <param name="Path">Report path, usually relative to the analysis root with forward slashes.</param>
/// <param name="Text">Complete source text.</param>
public sealed record SourceDocument(string Path, string Text);
