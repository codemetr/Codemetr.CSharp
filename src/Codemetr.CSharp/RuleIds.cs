namespace Codemetr.CSharp;

/// <summary>Stable rule identifiers used in reports and baselines.</summary>
public static class RuleIds
{
    /// <summary>Type line span.</summary>
    public const string TypeLines = "typeLines";
    /// <summary>Constructor arity.</summary>
    public const string ConstructorParameters = "constructorParameters";
    /// <summary>Public method count.</summary>
    public const string PublicMethods = "publicMethods";
    /// <summary>Instance field count.</summary>
    public const string InstanceFields = "instanceFields";
    /// <summary>Member cyclomatic complexity.</summary>
    public const string CyclomaticComplexity = "cyclomaticComplexity";
    /// <summary>Type maintainability index (lower is worse).</summary>
    public const string MaintainabilityIndex = "maintainabilityIndex";
    /// <summary>Type class coupling.</summary>
    public const string ClassCoupling = "classCoupling";
    /// <summary>Class depth of inheritance.</summary>
    public const string DepthOfInheritance = "depthOfInheritance";
    /// <summary>Member parameter count.</summary>
    public const string Parameters = "parameters";
    /// <summary>Member nesting depth.</summary>
    public const string NestingDepth = "nestingDepth";
    /// <summary>Catch-all type name suffix.</summary>
    public const string BannedTypeNameSuffix = "bannedTypeNameSuffix";

    /// <summary>True when a smaller value is worse.</summary>
    public static bool LowerIsWorse(string rule) => rule == MaintainabilityIndex;

    /// <summary>A short refactoring direction for the rule, phrased as the next concrete step.</summary>
    public static string Hint(string rule) => rule switch
    {
        TypeLines => "Split the type along its responsibilities; move cohesive groups of members into their own types.",
        ConstructorParameters => "Too many dependencies: the type likely owns several responsibilities. Split it, or group parameters that always travel together into one value object.",
        PublicMethods => "A wide public surface: separate the operations into smaller, role-specific types or interfaces.",
        InstanceFields => "Much mutable state in one owner: extract state that changes together into its own type.",
        CyclomaticComplexity => "Extract branches into well-named methods, use guard clauses, or replace condition chains with lookup tables or polymorphism.",
        MaintainabilityIndex => "Shorten and simplify: fewer lines, fewer branches and fewer distinct operands per type raise the index.",
        ClassCoupling => "Depends on many types: introduce a narrower abstraction or move code closer to the types it uses.",
        DepthOfInheritance => "Prefer composition over deep inheritance chains.",
        Parameters => "Introduce a parameter object or split the member; long parameter lists hide missing concepts.",
        NestingDepth => "Flatten with early returns or guard clauses, or extract the inner block into a method.",
        BannedTypeNameSuffix => "Name the type after its single responsibility; catch-all suffixes attract unrelated code.",
        _ => string.Empty,
    };
}
