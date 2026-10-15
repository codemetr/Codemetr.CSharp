namespace Codemetr.CSharp;

/// <summary>
/// Which files and types a run judged; baseline entries outside it are neither reported stale nor dropped.
/// A file is inside when it was analysed, when it lies below an analysed directory (so entries of deleted
/// files become stale), or when a file list named it although it no longer exists. Files that failed to
/// parse are always outside: their recorded debt must survive until they parse again.
/// </summary>
public sealed class AnalysisScope
{
    readonly HashSet<string> files;
    readonly string[] directories;
    readonly HashSet<string> unparsed;
    readonly HashSet<string>? types;

    /// <param name="files">Report paths that were analysed or explicitly listed.</param>
    /// <param name="types">Selected full type names, or null for every type.</param>
    /// <param name="directories">Analysed directories as report paths; an empty string is the whole root.</param>
    /// <param name="unparsed">Report paths of documents with syntax errors.</param>
    public AnalysisScope(IEnumerable<string> files, IEnumerable<string>? types, IEnumerable<string>? directories = null, IEnumerable<string>? unparsed = null)
    {
        this.files = new(files, StringComparer.Ordinal);
        this.types = types is null ? null : new(types, StringComparer.Ordinal);
        this.directories = (directories ?? []).Select(directory => directory.TrimEnd('/')).ToArray();
        this.unparsed = new(unparsed ?? [], StringComparer.Ordinal);
    }

    // The root ("") covers everything inside it, not paths that leave it ("../lib/L.cs").
    static bool Below(string file, string directory) => directory.Length == 0
        ? !file.StartsWith("../", StringComparison.Ordinal) && file != ".."
        : file.StartsWith(directory + "/", StringComparison.Ordinal);

    /// <summary>True when the entry's file was judged by this run and, with a type selection, its type was selected.</summary>
    public bool Contains(string file, string type) =>
        !unparsed.Contains(file)
        && (files.Contains(file) || directories.Any(directory => Below(file, directory)))
        && (types is null || types.Contains(type));
}
