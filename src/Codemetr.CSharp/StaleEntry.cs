namespace Codemetr.CSharp;

/// <summary>
/// A baseline entry that must be updated: fixed (<see cref="CurrentValue"/> null), improved, or moved to
/// another file (<see cref="MovedTo"/>) with a value that is not worse.
/// </summary>
public sealed record StaleEntry(BaselineEntry Entry, double? CurrentValue, string? MovedTo = null);
