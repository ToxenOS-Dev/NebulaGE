using System;
using System.IO;

namespace NebulaLauncher.Services;

/// <summary>
/// Detects whether the GitHub CLI (gh) is authenticated and returns the
/// connected username.  Reads ~/.config/gh/hosts.yml directly — no process
/// spawn, so it's safe to call on the UI thread.
/// </summary>
public static class GitHubService
{
    /// <summary>
    /// Returns the authenticated GitHub username, or null if not logged in.
    /// </summary>
    public static string? GetAuthenticatedUser()
    {
        var hostsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "gh", "hosts.yml");

        if (!File.Exists(hostsFile)) return null;

        try
        {
            // Minimal line-by-line YAML parse — no external dependency.
            // hosts.yml looks like:
            //   github.com:
            //       oauth_token: ghp_…
            //       user: kasparblythje
            //       git_protocol: https
            bool inGitHub = false;
            foreach (var line in File.ReadAllLines(hostsFile))
            {
                // Top-level keys (no leading whitespace)
                if (!line.StartsWith(' ') && !line.StartsWith('\t'))
                {
                    inGitHub = line.TrimEnd().Equals("github.com:",
                                   StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!inGitHub) continue;

                var trimmed = line.Trim();
                if (trimmed.StartsWith("user:", StringComparison.Ordinal))
                    return trimmed["user:".Length..].Trim();
            }
        }
        catch { }

        return null;
    }

    /// <summary>
    /// Derives the short "user/repo" slug from a full GitHub URL.
    /// e.g. "https://github.com/toxen-dev/NebulaGE" → "toxen-dev/NebulaGE"
    /// </summary>
    public static string? GetRepoSlug(string? gitHubUrl)
    {
        if (string.IsNullOrEmpty(gitHubUrl)) return null;

        const string prefix = "https://github.com/";
        if (gitHubUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return gitHubUrl[prefix.Length..];

        return null;
    }
}
