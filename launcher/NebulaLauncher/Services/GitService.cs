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

    /// <summary>
    /// Reads the origin remote URL from .git/config and normalises it to an
    /// https://github.com/… URL.  Returns null if no origin or not GitHub.
    /// No process spawn — reads the config file directly.
    /// </summary>
    public static string? GetGitHubUrl(string path)
    {
        var configFile = Path.Combine(path, ".git", "config");
        if (!File.Exists(configFile)) return null;

        try
        {
            bool inOrigin = false;
            foreach (var line in File.ReadAllLines(configFile))
            {
                var t = line.Trim();
                if (t == "[remote \"origin\"]") { inOrigin = true; continue; }
                if (t.StartsWith('['))           { inOrigin = false; continue; }

                if (inOrigin && t.StartsWith("url = "))
                    return NormalizeGitHubUrl(t["url = ".Length..].Trim());
            }
        }
        catch { }

        return null;
    }

    // Converts both SSH and HTTPS git remote URLs to an https://github.com/… link.
    private static string? NormalizeGitHubUrl(string url)
    {
        // SSH:   git@github.com:user/repo.git  →  https://github.com/user/repo
        if (url.StartsWith("git@github.com:", StringComparison.Ordinal))
        {
            var rest = url["git@github.com:".Length..];
            if (rest.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                rest = rest[..^4];
            return $"https://github.com/{rest}";
        }

        // HTTPS: https://github.com/user/repo[.git]
        if (url.Contains("github.com", StringComparison.OrdinalIgnoreCase))
            return url.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? url[..^4] : url;

        return null; // not a GitHub remote
    }
}
