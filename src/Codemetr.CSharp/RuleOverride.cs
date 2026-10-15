namespace Codemetr.CSharp;

/// <summary>Rules applied to matching types. <c>Files</c> is a glob over report paths, <c>Type</c> a full or simple type name.</summary>
public sealed record RuleOverride
{
    /// <summary>Glob such as <c>src/Legacy/**/*.cs</c>; null matches every file.</summary>
    public string? Files { get; init; }
    /// <summary>Full or simple type name; null matches every type.</summary>
    public string? Type { get; init; }
    /// <summary>Values that replace the inherited ones.</summary>
    public GuardRules Rules { get; init; } = new();
}
