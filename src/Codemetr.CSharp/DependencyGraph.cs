namespace Codemetr.CSharp;

/// <summary>Source-level dependencies; never a claim about runtime DI, plugins, project or package graphs.</summary>
public sealed record DependencyGraph(string Scope, IReadOnlyList<DependencyNode> Types,
    IReadOnlyList<DependencyCycle> TypeCycles, IReadOnlyList<DependencyCycle> NamespaceCycles,
    IReadOnlyList<string> AmbiguousTypes)
{
    /// <summary>Builds the induced selected graph, excluding ambiguous duplicate type names.</summary>
    public static DependencyGraph Build(IReadOnlyList<TypeMetrics> metrics, IReadOnlyCollection<string>? contextTypes = null)
    {
        var groups = metrics.GroupBy(type => type.FullName, StringComparer.Ordinal).ToArray();
        var ambiguous = groups.Where(group => group.Count() != 1).Select(group => group.Key).Order(StringComparer.Ordinal).ToArray();
        var unique = groups.Where(group => group.Count() == 1).Select(group => group.Single()).ToArray();
        var names = unique.Select(type => type.FullName).ToHashSet(StringComparer.Ordinal);
        var context = (contextTypes ?? []).ToHashSet(StringComparer.Ordinal);
        var edges = unique.ToDictionary(type => type.FullName,
            type => type.StrictCoupledTypes.Where(name => names.Contains(name) && name != type.FullName).Order(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        var nodes = unique.Select(type => Node(type, names, context, edges))
            .OrderBy(node => node.Type, StringComparer.Ordinal).ToArray();
        var namespaces = nodes.GroupBy(node => node.Namespace, StringComparer.Ordinal).ToDictionary(group => group.Key,
            group => group.SelectMany(node => node.Outgoing).Select(name => unique.Single(type => type.FullName == name).Namespace)
                .Where(ns => ns != group.Key).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        return new("induced selected source types; fan-in and cycle absence are scope-limited", nodes,
            StrongComponents.Cycles(edges), StrongComponents.Cycles(namespaces), ambiguous);
    }

    static DependencyNode Node(TypeMetrics type, HashSet<string> names, HashSet<string> context, Dictionary<string, string[]> edges)
    {
        var coupled = type.StrictCoupledTypes;
        return new(type.FullName, type.Namespace,
            edges.Where(pair => pair.Value.Contains(type.FullName, StringComparer.Ordinal)).Select(pair => pair.Key).Order(StringComparer.Ordinal).ToArray(),
            edges[type.FullName],
            coupled.Where(name => !names.Contains(name) && context.Contains(name)).Order(StringComparer.Ordinal).ToArray(),
            coupled.Where(name => !name.StartsWith('?') && !names.Contains(name) && !context.Contains(name)).Order(StringComparer.Ordinal).ToArray(),
            coupled.Where(name => name.StartsWith('?')).Order(StringComparer.Ordinal).ToArray());
    }
}
