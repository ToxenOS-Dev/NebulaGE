using System;
using System.IO;

namespace NebulaLauncher.Services;

/// <summary>
/// Reads Git repository state directly from the .git/ directory.
/// No process spawn — fast enough to call on every project load.
/// </summary>
public static class GitService
{
    /// <summary>Returns true if <paramref name="path"/> contains a .git/ directory.</summary>
    public static bool HasRepo(string path) =>
        Directory.Exists(Path.Combine(path, ".git"));

    /// <summary>
    /// Reads the current branch name from .git/HEAD.
    /// Returns a short hash for detached HEAD, null if unreadable.
    /// </summary>
    public static string? GetBranch(string path)
    {
        var headFile = Path.Combine(path, ".git", "HEAD");
        if (!File.Exists(headFile)) return null;

        try
        {
            var content = File.ReadAllText(headFile).Trim();

            // Normal branch: "ref: refs/heads/main"
            const string prefix = "ref: refs/heads/";
            if (content.StartsWith(prefix, StringComparison.Ordinal))
                return content[prefix.Length..];

            // Detached HEAD: 40-char commit hash — show first 7
            return content.Length >= 7 ? content[..7] : content;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Returns (hasRepo, branchName) in one call.</summary>
    public static (bool HasRepo, string? Branch) GetStatus(string path)
    {
        if (!HasRepo(path)) return (false, null);
        return (true, GetBranch(path));
    }
}
