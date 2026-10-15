namespace Codemetr.CSharp;

/// <summary>Why a recorded baseline entry has no current violation.</summary>
public enum EntryJudgement
{
    /// <summary>The file was measured without inactive code (or was deleted in a directory run): the debt is gone.</summary>
    Fixed,
    /// <summary>Not provable in this run (inactive #if code, or a deleted file in a file-list run): the entry stays.</summary>
    Unverified,
    /// <summary>The file exists but is no longer measured (excluded, generated header, skipped): this fails the check.</summary>
    Unmeasured,
}
