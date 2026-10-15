namespace Codemetr.CSharp;

/// <summary>One declaration of a type: its file, whether any run could measure that file, and its member names.</summary>
public sealed record DeclarationSite(string File, bool NeverMeasured, IReadOnlyList<string> Members);
