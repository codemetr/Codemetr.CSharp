namespace Codemetr.CSharp;

/// <summary>Metrics of one type declaration. Nested types are measured as their own entries.</summary>
/// <param name="Name">Simple name.</param>
/// <param name="FullName">Namespace, containing types and name, for example <c>Shop.Orders.Order.Line</c>.</param>
/// <param name="Kind">class, struct, interface, enum, record or record struct.</param>
/// <param name="File">Report path of the declaring document.</param>
/// <param name="Line">1-based start line of the declaration.</param>
/// <param name="Lines">Line span of the declaration including nested types.</param>
/// <param name="LogicalLines">Lines with code tokens other than braces, excluding nested types.</param>
/// <param name="MaxCyclomaticComplexity">Highest member complexity, 1 when the type has no executable member.</param>
/// <param name="MostComplexMember">Name of the member with <paramref name="MaxCyclomaticComplexity"/>, empty if none.</param>
/// <param name="TotalCyclomaticComplexity">Sum of member complexities.</param>
/// <param name="MaintainabilityIndex">Visual-Studio-style index 0..100 from Halstead volume, total complexity and logical lines.</param>
/// <param name="ClassCoupling">Distinct other types referenced by this type's own code.</param>
/// <param name="CoupledTypes">The referenced types; unresolved names carry a leading <c>?</c>.</param>
/// <param name="DepthOfInheritance">Base classes above this class, excluding System.Object.</param>
/// <param name="ConstructorParameterCount">Largest constructor arity of a class, including a primary constructor.</param>
/// <param name="PublicMethodCount">Public methods declared directly in this type.</param>
/// <param name="InstanceFieldCount">Non-static, non-const fields declared directly in this type.</param>
/// <param name="Members">Per-member metrics.</param>
public sealed record TypeMetrics(
    string Name,
    string FullName,
    string Kind,
    string File,
    int Line,
    int Lines,
    int LogicalLines,
    int MaxCyclomaticComplexity,
    string MostComplexMember,
    int TotalCyclomaticComplexity,
    int MaintainabilityIndex,
    int ClassCoupling,
    IReadOnlyList<string> CoupledTypes,
    int DepthOfInheritance,
    int ConstructorParameterCount,
    int PublicMethodCount,
    int InstanceFieldCount,
    IReadOnlyList<MemberMetrics> Members)
{
    /// <summary>Coupling names from successful binding only; unresolved names remain explicitly prefixed.</summary>
    public IReadOnlyList<string> StrictCoupledTypes { get; init; } = [];
    /// <summary>Declared namespace, excluding containing type names.</summary>
    public string Namespace { get; init; } = "";

    /// <summary>All source locations contributing to this measurement.</summary>
    public IReadOnlyList<MetricDeclaration> Declarations { get; init; } = [];
}
