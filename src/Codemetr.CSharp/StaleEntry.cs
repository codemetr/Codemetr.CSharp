namespace Codemetr.CSharp;

/// <summary>A baseline entry that is fixed (<see cref="CurrentValue"/> null) or improved and should be tightened.</summary>
public sealed record StaleEntry(BaselineEntry Entry, double? CurrentValue);
