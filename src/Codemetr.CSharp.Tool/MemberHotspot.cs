namespace Codemetr.CSharp.Tool;

/// <summary>A measured member's source identity, size and static complexity; no inferred test coverage.</summary>
internal sealed record MemberHotspot(string Type, string Member, string Kind, string File, int Line,
    int CodeLines, int StatementCount, int CyclomaticComplexity, int CognitiveComplexity,
    int NestingDepth, int ParameterCount);
