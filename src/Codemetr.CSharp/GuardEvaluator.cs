namespace Codemetr.CSharp;

/// <summary>Compares measured types with the effective rules of a configuration.</summary>
public static class GuardEvaluator
{
    /// <summary>All violations, ordered by file, line, rule and member.</summary>
    public static IReadOnlyList<Violation> Evaluate(IEnumerable<TypeMetrics> types, CodemetrConfig config)
    {
        ArgumentNullException.ThrowIfNull(types);
        ArgumentNullException.ThrowIfNull(config);
        var violations = new List<Violation>();
        foreach (var type in types)
        {
            var rules = config.RulesFor(type);
            void Max(string rule, int value, int? limit, string what)
            {
                if (limit is { } max && value > max)
                    violations.Add(new(rule, type.FullName, null, type.File, type.Line, value, max, $"{type.FullName}: {what} {value} exceeds max {max}."));
            }
            Max(RuleIds.TypeLines, type.Lines, rules.MaxTypeLines, "line span");
            Max(RuleIds.ConstructorParameters, type.ConstructorParameterCount, rules.MaxConstructorParameters, "constructor arity");
            Max(RuleIds.PublicMethods, type.PublicMethodCount, rules.MaxPublicMethods, "public method count");
            Max(RuleIds.InstanceFields, type.InstanceFieldCount, rules.MaxInstanceFields, "instance field count");
            Max(RuleIds.ClassCoupling, type.ClassCoupling, rules.MaxClassCoupling, "class coupling");
            Max(RuleIds.DepthOfInheritance, type.DepthOfInheritance, rules.MaxDepthOfInheritance, "depth of inheritance");
            if (rules.MinMaintainabilityIndex is { } min && type.MaintainabilityIndex < min)
                violations.Add(new(RuleIds.MaintainabilityIndex, type.FullName, null, type.File, type.Line, type.MaintainabilityIndex, min,
                    $"{type.FullName}: maintainability index {type.MaintainabilityIndex} is below min {min}."));
            var suffix = rules.BannedTypeNameSuffixes?.FirstOrDefault(banned => type.Name.EndsWith(banned, StringComparison.Ordinal));
            if (suffix is not null)
                violations.Add(new(RuleIds.BannedTypeNameSuffix, type.FullName, null, type.File, type.Line, 1, 0,
                    $"{type.FullName}: type name uses the catch-all suffix '{suffix}'."));
            foreach (var member in type.Members)
            {
                void MemberMax(string rule, int value, int? limit, string what)
                {
                    if (limit is { } max && value > max)
                        violations.Add(new(rule, type.FullName, member.Name, type.File, member.Line, value, max,
                            $"{type.FullName}.{member.Name}: {what} {value} exceeds max {max}."));
                }
                MemberMax(RuleIds.CyclomaticComplexity, member.CyclomaticComplexity, rules.MaxCyclomaticComplexity, "cyclomatic complexity");
                MemberMax(RuleIds.Parameters, member.ParameterCount, rules.MaxParameters, "parameter count");
                MemberMax(RuleIds.NestingDepth, member.NestingDepth, rules.MaxNestingDepth, "nesting depth");
            }
        }
        return violations.OrderBy(v => v.File, StringComparer.Ordinal).ThenBy(v => v.Line)
            .ThenBy(v => v.Rule, StringComparer.Ordinal).ThenBy(v => v.Member, StringComparer.Ordinal).ToArray();
    }
}
