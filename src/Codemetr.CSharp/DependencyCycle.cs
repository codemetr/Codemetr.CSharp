namespace Codemetr.CSharp;

/// <summary>A strongly connected group and one actual closed cycle path (first node repeated at end).</summary>
public sealed record DependencyCycle(IReadOnlyList<string> Nodes, IReadOnlyList<string> Witness);
