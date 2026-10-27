namespace Codemetr.CSharp;

/// <summary>Directed dependencies within the selected, unambiguous source-type graph.</summary>
public sealed record DependencyNode(string Type, string Namespace, IReadOnlyList<string> Incoming,
    IReadOnlyList<string> Outgoing, IReadOnlyList<string> ContextTypes, IReadOnlyList<string> ExternalResolved,
    IReadOnlyList<string> Unresolved)
{
    /// <summary>Observed incoming dependencies; filtering can reduce this number.</summary>
    public int FanIn => Incoming.Count;
    /// <summary>Outgoing dependencies to selected source types.</summary>
    public int FanOut => Outgoing.Count;
}
