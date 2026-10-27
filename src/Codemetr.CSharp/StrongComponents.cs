namespace Codemetr.CSharp;

/// <summary>Deterministic strongly connected groups and edge-checked witness paths.</summary>
internal static class StrongComponents
{
    public static IReadOnlyList<DependencyCycle> Cycles(IReadOnlyDictionary<string, string[]> edges)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var order = new List<string>();
        foreach (var start in edges.Keys.Order(StringComparer.Ordinal)) Finish(start, edges, visited, order);
        var reverse = edges.Keys.ToDictionary(key => key, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var pair in edges) foreach (var next in pair.Value) reverse[next].Add(pair.Key);
        visited.Clear();
        var cycles = new List<DependencyCycle>();
        foreach (var start in order.AsEnumerable().Reverse())
        {
            if (visited.Contains(start)) continue;
            var group = new List<string>();
            var pending = new Stack<string>(); pending.Push(start);
            while (pending.TryPop(out var current))
                if (visited.Add(current)) { group.Add(current); foreach (var next in reverse[current]) pending.Push(next); }
            var sorted = group.Order(StringComparer.Ordinal).ToArray();
            if (sorted.Length > 1) cycles.Add(new(sorted, Witness(sorted, edges)));
        }
        return cycles.OrderBy(cycle => cycle.Nodes[0], StringComparer.Ordinal).ToArray();
    }

    static void Finish(string start, IReadOnlyDictionary<string, string[]> edges, HashSet<string> visited, List<string> order)
    {
        var pending = new Stack<(string Node, bool Finish)>(); pending.Push((start, false));
        while (pending.TryPop(out var step))
        {
            if (step.Finish) { order.Add(step.Node); continue; }
            if (!visited.Add(step.Node)) continue;
            pending.Push((step.Node, true));
            foreach (var next in edges[step.Node].Reverse())
                if (!visited.Contains(next)) pending.Push((next, false));
        }
    }

    static IReadOnlyList<string> Witness(string[] group, IReadOnlyDictionary<string, string[]> edges)
    {
        var allowed = group.ToHashSet(StringComparer.Ordinal);
        var start = group[0];
        var pending = new Queue<string>();
        var previous = new Dictionary<string, string?>(StringComparer.Ordinal) { [start] = null };
        pending.Enqueue(start);
        while (pending.TryDequeue(out var current))
        {
            foreach (var next in edges[current].Where(allowed.Contains))
            {
                if (next == start)
                {
                    var path = new List<string> { current };
                    while (previous[path[^1]] is { } parent) path.Add(parent);
                    path.Reverse(); path.Add(start); return path;
                }
                if (previous.TryAdd(next, current)) pending.Enqueue(next);
            }
        }
        throw new InvalidOperationException("A strongly connected group has no cycle witness");
    }
}
