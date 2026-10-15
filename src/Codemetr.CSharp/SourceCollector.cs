using Microsoft.Extensions.FileSystemGlobbing;

namespace Codemetr.CSharp;

/// <summary>Collects C# documents from files and directories below a root, skipping build output and generated code.</summary>
public static class SourceCollector
{
    /// <summary>Largest source file that is read; larger files are input errors, not silently skipped.</summary>
    public const long MaximumFileBytes = 4 * 1024 * 1024;
    const int GeneratedMarkerLines = 5;
    static readonly string[] SkippedDirectories = ["bin", "obj", ".git", "node_modules"];
    static readonly string[] GeneratedSuffixes = [".g.cs", ".g.i.cs", ".generated.cs", ".designer.cs"];

    /// <summary>
    /// Reads every <c>*.cs</c> below the given paths. With <paramref name="lenient"/> (file lists such as
    /// <c>git diff --name-only</c>) missing paths and non-C# files are skipped instead of failing.
    /// </summary>
    public static IReadOnlyList<SourceDocument> Collect(string root, IEnumerable<string> paths, IReadOnlyCollection<string> exclude, bool lenient = false)
    {
        root = Path.GetFullPath(root);
        var matcher = new Matcher(StringComparison.Ordinal);
        matcher.AddIncludePatterns(exclude);
        var files = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            var full = Path.GetFullPath(path, root);
            if (Directory.Exists(full)) files.UnionWith(Enumerate(full));
            else if (File.Exists(full) && full.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) files.Add(full);
            else if (!lenient) throw new FileNotFoundException(File.Exists(full) ? $"Not a C# file: {path}" : $"Path not found: {path}");
        }

        var documents = new List<SourceDocument>();
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
            if (exclude.Count > 0 && matcher.Match(relative).HasMatches) continue;
            if (GeneratedSuffixes.Any(suffix => file.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))) continue;
            if (new FileInfo(file).Length > MaximumFileBytes) throw new InvalidDataException($"Source file exceeds {MaximumFileBytes} bytes: {relative}");
            var text = File.ReadAllText(file);
            if (IsGenerated(text)) continue;
            documents.Add(new SourceDocument(relative, text));
        }
        return documents;
    }

    /// <summary>
    /// Every C# file below the context directories as compilation context: generated code included (it declares
    /// types that hand-written code uses), configuration excludes ignored, oversized files skipped.
    /// </summary>
    public static IReadOnlyList<SourceDocument> CollectContext(string root, IEnumerable<string> directories)
    {
        root = Path.GetFullPath(root);
        return directories.Select(directory => Path.GetFullPath(directory, root))
            .Select(directory => Directory.Exists(directory) ? directory : throw new DirectoryNotFoundException($"Context directory not found: {directory}"))
            .SelectMany(Enumerate).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
            .Where(file => new FileInfo(file).Length <= MaximumFileBytes)
            .Select(file => new SourceDocument(Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/'), File.ReadAllText(file)))
            .ToArray();
    }

    /// <summary>
    /// True for a document that no run would ever measure: build output, generated code or an excluded path.
    /// Its types may still be compilation context.
    /// </summary>
    public static bool IsNeverMeasured(SourceDocument document, IReadOnlyCollection<string> exclude)
    {
        if (GeneratedSuffixes.Any(suffix => document.Path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) || IsGenerated(document.Text)) return true;
        if (exclude.Count == 0) return false;
        var matcher = new Matcher(StringComparison.Ordinal);
        matcher.AddIncludePatterns(exclude);
        return matcher.Match(document.Path).HasMatches;
    }

    static IEnumerable<string> Enumerate(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*.cs")) yield return file;
        foreach (var child in Directory.EnumerateDirectories(directory))
        {
            if (SkippedDirectories.Contains(Path.GetFileName(child), StringComparer.OrdinalIgnoreCase)) continue;
            if (new DirectoryInfo(child).LinkTarget is not null) continue;
            foreach (var file in Enumerate(child)) yield return file;
        }
    }

    /// <summary>
    /// Generated code is recognised by the standard header comment <c>&lt;auto-generated&gt;</c> in the first
    /// lines (a generator that emits the marker as output text is still measured) and by file name suffixes.
    /// A <c>GeneratedCode</c> attribute or a comment mentioning it does not exclude a file: that text is too
    /// easy to add to hand-written code.
    /// </summary>
    public static bool IsGenerated(string text)
    {
        foreach (var line in text.Split('\n').Take(GeneratedMarkerLines))
        {
            var trimmed = line.TrimStart();
            if ((trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.StartsWith("/*", StringComparison.Ordinal)
                    || trimmed.StartsWith('*')) && trimmed.Contains("<auto-generated", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
