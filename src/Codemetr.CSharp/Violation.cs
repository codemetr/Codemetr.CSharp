namespace Codemetr.CSharp;

/// <summary>One rule breach. <see cref="Key"/> identifies it across runs for the baseline.</summary>
public sealed record Violation(string Rule, string Type, string? Member, string File, int Line, double Value, double Limit, string Message)
{
    /// <summary>Stable identity: rule, type, member and file.</summary>
    public string Key => $"{Rule}|{Type}|{Member}|{File}";

    /// <summary>Refactoring direction for this rule.</summary>
    public string Hint => RuleIds.Hint(Rule);
}
