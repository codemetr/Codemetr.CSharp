namespace Codemetr.CSharp;

/// <summary>
/// A baseline entry that must be updated: fixed (<see cref="CurrentValue"/> null), improved, or moved to
/// another file (<see cref="MovedTo"/>) with a value that is not worse. An <see cref="Unmeasured"/> entry
/// belongs to a file that exists but is no longer measured; it fails the check and is never removed by tightening.
/// </summary>
public sealed record StaleEntry(BaselineEntry Entry, double? CurrentValue, string? MovedTo = null, bool Unmeasured = false);
