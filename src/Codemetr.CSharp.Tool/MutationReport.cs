namespace Codemetr.CSharp.Tool;

/// <summary>Imported report identity and status counts; does not attest correspondence to current source.</summary>
internal sealed record MutationReport(string File, string Sha256, string SchemaVersion, int Detected, int Undetected,
    int Excluded, double? Score, IReadOnlyDictionary<string, int> StatusCounts, string Scope);
