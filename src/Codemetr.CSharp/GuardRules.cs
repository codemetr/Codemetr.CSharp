namespace Codemetr.CSharp;

/// <summary>
/// Thresholds. A null value means "inherit" in overrides and "rule disabled" in the merged result.
/// Upper limits are inclusive: a value equal to the limit passes.
/// </summary>
public sealed record GuardRules
{
    /// <summary>Line span of a type declaration.</summary>
    public int? MaxTypeLines { get; init; }
    /// <summary>Largest constructor arity of a class.</summary>
    public int? MaxConstructorParameters { get; init; }
    /// <summary>Public methods declared directly in a type.</summary>
    public int? MaxPublicMethods { get; init; }
    /// <summary>Instance fields declared directly in a type.</summary>
    public int? MaxInstanceFields { get; init; }
    /// <summary>Cyclomatic complexity of any single member.</summary>
    public int? MaxCyclomaticComplexity { get; init; }
    /// <summary>Lower bound of the maintainability index of a type.</summary>
    public int? MinMaintainabilityIndex { get; init; }
    /// <summary>Distinct referenced types of a type.</summary>
    public int? MaxClassCoupling { get; init; }
    /// <summary>Base classes above a class.</summary>
    public int? MaxDepthOfInheritance { get; init; }
    /// <summary>Parameters of any single member.</summary>
    public int? MaxParameters { get; init; }
    /// <summary>Control-flow nesting of any single member.</summary>
    public int? MaxNestingDepth { get; init; }
    /// <summary>Optional upper bound on member body code lines; disabled by default.</summary>
    public int? MaxMemberCodeLines { get; init; }
    /// <summary>Optional upper bound on syntactic member statements; disabled by default.</summary>
    public int? MaxMemberStatements { get; init; }
    /// <summary>Type name suffixes that signal a catch-all owner.</summary>
    public string[]? BannedTypeNameSuffixes { get; init; }

    /// <summary>
    /// Defaults. Type-level values come from guards in use on an existing production code base
    /// (600 lines, arity 6, 10 public methods, 8 fields, complexity 25, maintainability 15, coupling 40,
    /// inheritance 3). Parameter and nesting limits are codemetr's own conservative choices.
    /// </summary>
    public static GuardRules Default { get; } = new()
    {
        MaxTypeLines = 600,
        MaxConstructorParameters = 6,
        MaxPublicMethods = 10,
        MaxInstanceFields = 8,
        MaxCyclomaticComplexity = 25,
        MinMaintainabilityIndex = 15,
        MaxClassCoupling = 40,
        MaxDepthOfInheritance = 3,
        MaxParameters = 7,
        MaxNestingDepth = 4,
        BannedTypeNameSuffixes = ["Helper", "Helpers", "Util", "Utils", "Utilities", "Manager", "Processor"],
    };

    /// <summary>Returns these rules with every non-null value of <paramref name="other"/> applied on top.</summary>
    public GuardRules Merge(GuardRules? other) => other is null ? this : new()
    {
        MaxTypeLines = other.MaxTypeLines ?? MaxTypeLines,
        MaxConstructorParameters = other.MaxConstructorParameters ?? MaxConstructorParameters,
        MaxPublicMethods = other.MaxPublicMethods ?? MaxPublicMethods,
        MaxInstanceFields = other.MaxInstanceFields ?? MaxInstanceFields,
        MaxCyclomaticComplexity = other.MaxCyclomaticComplexity ?? MaxCyclomaticComplexity,
        MinMaintainabilityIndex = other.MinMaintainabilityIndex ?? MinMaintainabilityIndex,
        MaxClassCoupling = other.MaxClassCoupling ?? MaxClassCoupling,
        MaxDepthOfInheritance = other.MaxDepthOfInheritance ?? MaxDepthOfInheritance,
        MaxParameters = other.MaxParameters ?? MaxParameters,
        MaxNestingDepth = other.MaxNestingDepth ?? MaxNestingDepth,
        MaxMemberCodeLines = other.MaxMemberCodeLines ?? MaxMemberCodeLines,
        MaxMemberStatements = other.MaxMemberStatements ?? MaxMemberStatements,
        BannedTypeNameSuffixes = other.BannedTypeNameSuffixes ?? BannedTypeNameSuffixes,
    };
}
