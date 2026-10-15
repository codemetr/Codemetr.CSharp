namespace Codemetr.CSharp;

/// <summary>A violation with its baseline status: new, worsened or baselined.</summary>
public sealed record CheckedViolation(Violation Violation, string Status, double? BaselineValue)
{
    /// <summary>Not in the baseline.</summary>
    public const string New = "new";
    /// <summary>In the baseline, but the value got worse.</summary>
    public const string Worsened = "worsened";
    /// <summary>In the baseline and not worse; tolerated.</summary>
    public const string Baselined = "baselined";

    /// <summary>True when this violation fails the check.</summary>
    public bool Fails => Status != Baselined;
}
