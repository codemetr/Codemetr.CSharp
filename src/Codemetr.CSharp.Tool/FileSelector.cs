namespace Codemetr.CSharp.Tool;

/// <summary>Turns paths and file lists of a request into the documents a run measures.</summary>
internal static class FileSelector
{
    public static FileSelection Select(string root, CodemetrRequest request, CodemetrConfig config, List<string> warnings)
    {
        // git quotes paths with special characters ("src/\303\234.cs"); such a line would silently match nothing.
        var quoted = request.ListedFiles.FirstOrDefault(path => path.Length > 1 && path[0] == '"' && path[^1] == '"');
        if (quoted is not null)
            throw new InvalidDataException($"Quoted path in file list: {quoted}; list files with 'git -c core.quotePath=false diff --name-only ...'");
        var listed = request.ListedFiles.Select(path => path.Replace('\\', '/')).ToArray();
        IReadOnlyList<string> paths = request.Paths.Count > 0 ? request.Paths : request.HasFileList ? [] : ["."];
        var skipped = new SortedSet<string>(StringComparer.Ordinal);
        var documents = Collect(root, paths, listed, config, skipped);
        var listedSources = listed.Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToArray();
        var missing = listedSources.Where(path => !File.Exists(Path.GetFullPath(path, root))).Select(path => CodemetrService.Relative(root, Path.GetFullPath(path, root))).ToArray();
        if (listedSources.Length > 0 && missing.Length == listedSources.Length)
            warnings.Add($"None of the {listedSources.Length} listed C# files exists below {root}; check --root (file lists from git are relative to the repository root).");
        return new FileSelection(paths, documents, missing, skipped);
    }

    static IReadOnlyList<SourceDocument> Collect(string root, IReadOnlyList<string> paths, IReadOnlyList<string> listed, CodemetrConfig config, ICollection<string> skipped)
    {
        var documents = new List<SourceDocument>();
        if (paths.Count > 0) documents.AddRange(SourceCollector.Collect(root, paths, config.Exclude, skipped: skipped));
        if (listed.Count > 0) documents.AddRange(SourceCollector.Collect(root, listed, config.Exclude, lenient: true, skipped: skipped));
        return documents.DistinctBy(document => document.Path, StringComparer.Ordinal).ToArray();
    }
}
