namespace Codemetr.CSharp;

/// <summary>A syntax error that prevents measuring a document. Such documents produce no metrics.</summary>
public sealed record ParseError(string File, int Line, string Id, string Message);
