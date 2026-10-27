namespace Codemetr.CSharp;

/// <summary>
/// Which files and types a run judged; baseline entries outside it are neither reported stale nor dropped.
/// A file is inside when it was analysed, when it lies below an analysed directory (so entries of deleted
/// files become stale), or when a file list named it although it no longer exists. Files that failed to
/// parse are always outside: their recorded debt must survive until they parse again.
/// <see cref="Judge"/> says why a recorded entry without a current violation has none, so that only a
/// proven fix can remove debt.
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

    /// <summary>Report paths that were actually measured; null means every file inside the scope was measured.</summary>
    public IReadOnlySet<string>? Measured { get; init; }

    /// <summary>Measured paths whose inactive #if code may hide a violation.</summary>
    public IReadOnlySet<string> Uncertain { get; init; } = new HashSet<string>();

    /// <summary>Counterpart-only files where only selected logical types, not unrelated declarations, were measured.</summary>
    public IReadOnlyDictionary<string, IReadOnlySet<string>> RestrictedFileTypes { get; init; } = new Dictionary<string, IReadOnlySet<string>>();

    /// <summary>Where types are declared outside the measured files; without it, no move out of sight is detected.</summary>
    public DeclarationIndex? Declarations { get; init; }

    /// <summary>
    /// Classifies a recorded entry in scope that no current violation matches. Only a proven fix is
    /// <see cref="EntryJudgement.Fixed"/>: the member or type must not have moved into code that is never measured.
    /// </summary>
    public EntryJudgement Judge(BaselineEntry entry, Func<string, bool>? fileExists)
    {
        var sites = Declarations?.Find(entry.Type).Where(site => site.File != entry.File).ToArray() ?? [];
        var simpleMember = entry.Member is null ? null : DeclarationIndex.SimpleMemberName(entry.Member);
        var hiddenMember = simpleMember is not null && sites.Any(site => site.NeverMeasured
            && site.Members.Any(member => DeclarationIndex.SimpleMemberName(member) == simpleMember));
        if (hiddenMember) return EntryJudgement.Unmeasured;
        if (Measured is null || Measured.Contains(entry.File))
        {
            // Type-level values drop when members move into an unmeasured partial part; that is no proof of a fix.
            var hiddenPart = entry.Member is null && sites.Any(site => site.NeverMeasured);
            return Uncertain.Contains(entry.File) || hiddenPart ? EntryJudgement.Unverified : EntryJudgement.Fixed;
        }
        if (fileExists?.Invoke(entry.File) != false) return EntryJudgement.Unmeasured;
        return JudgeDeleted(entry, sites);
    }

    // The recorded file is gone: where its type lives now decides.
    EntryJudgement JudgeDeleted(BaselineEntry entry, DeclarationSite[] sites)
    {
        if (entry.Member is null && sites.Length > 0 && sites.All(site => site.NeverMeasured)) return EntryJudgement.Unmeasured;
        if (sites.Any(site => !site.NeverMeasured && Measured!.Contains(site.File))) return EntryJudgement.Fixed;
        if (sites.Length > 0 || Declarations?.HasLinkedDirectory == true) return EntryJudgement.Unverified;
        // A deleted file is settled by a run over its directory; a file list may lack the rename target.
        return directories.Any(directory => Below(entry.File, directory)) ? EntryJudgement.Fixed : EntryJudgement.Unverified;
    }

    /// <summary>True when the entry's file was judged by this run and, with a type selection, its type was selected.</summary>
    public bool Contains(string file, string type) =>
        !unparsed.Contains(file)
        && (files.Contains(file) || directories.Any(directory => Below(file, directory)))
        && (types is null || types.Contains(type))
        && (!RestrictedFileTypes.TryGetValue(file, out var selected) || selected.Contains(type));
}
