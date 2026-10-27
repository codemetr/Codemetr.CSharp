namespace Codemetr.CSharp;

/// <summary>Connected instance-method groups through shared own fields or direct this-calls; advisory.</summary>
public sealed record CohesionMetrics(string Type, bool Applicable, string Reason, int MethodCount,
    int InstanceFieldCount, IReadOnlyList<IReadOnlyList<string>> Groups);
