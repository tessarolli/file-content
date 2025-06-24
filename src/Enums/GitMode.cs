namespace file_content;

/// <summary>
/// Specifies the Git mode for file selection.
/// </summary>
public enum GitMode
{
    /// <summary>
    /// No Git integration.
    /// </summary>
    None,

    /// <summary>
    /// Select changed (modified and untracked) files.
    /// </summary>
    Changed,

    /// <summary>
    /// Select staged (added) files.
    /// </summary>
    Staged,

    /// <summary>
    /// Select all changed and staged files.
    /// </summary>
    All,
}