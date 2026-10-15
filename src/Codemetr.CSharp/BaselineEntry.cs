namespace Codemetr.CSharp;

/// <summary>A tolerated violation with the value it had when it was recorded.</summary>
public sealed record BaselineEntry(string Rule, string Type, string? Member, string File, double Value)
{
    /// <summary>Same identity as <see cref="Violation.Key"/>.</summary>
    public string Key => $"{Rule}|{Type}|{Member}|{File}";
}
