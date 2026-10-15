namespace Codemetr.CSharp.Tool;

/// <summary>What to analyse: paths below a root plus optional type, config and baseline.</summary>
internal sealed record CodemetrRequest
{
    public required string Root { get; init; }
    public IReadOnlyList<string> Paths { get; init; } = [];
    /// <summary>File list such as <c>git diff --name-only</c>; missing and non-C# entries are skipped.</summary>
    public IReadOnlyList<string> ListedFiles { get; init; } = [];
    /// <summary>True when a file list was given, even an empty one: then nothing else is analysed by default.</summary>
    public bool HasFileList { get; init; }
    public string? Type { get; init; }
    public string? ConfigPath { get; init; }
    public string? BaselinePath { get; init; }
    public bool IncludeMetrics { get; init; }
    public int Top { get; init; } = CodemetrService.DefaultTop;
}
