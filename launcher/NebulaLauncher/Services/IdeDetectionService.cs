using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace NebulaLauncher.Services;

public record IdeInfo(
    string Name,
    string ExecutablePath,
    int    Priority
);

/// <summary>
/// Scans the Linux system for installed IDEs via PATH and known locations.
/// Priority order: VS Code → VSCodium → Rider → CLion → Neovim → Emacs → Qt Creator.
/// </summary>
public class IdeDetectionService
{
    private static readonly (string Name, string[] Candidates, int Priority)[] KnownIdes =
    [
        ("Visual Studio Code", ["code"],                                              100),
        ("VSCodium",           ["codium"],                                             90),
        ("JetBrains Rider",    ["rider", "/opt/rider/bin/rider.sh"],                   80),
        ("CLion",              ["clion", "/opt/clion/bin/clion.sh"],                   70),
        ("Neovim",             ["nvim"],                                                60),
        ("Emacs",              ["emacs"],                                               50),
        ("Qt Creator",         ["qtcreator"],                                           40),
    ];

    private static readonly string[] ExtraSearchPaths =
    [
        "/usr/bin",
        "/usr/local/bin",
        "/opt/local/bin",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin"),
    ];

    // ── Public API ────────────────────────────────────────────

    public List<IdeInfo> Detect()
    {
        var found = new List<IdeInfo>();

        foreach (var (name, candidates, priority) in KnownIdes)
        {
            foreach (var candidate in candidates)
            {
                var resolved = Resolve(candidate);
                if (resolved is not null)
                {
                    found.Add(new IdeInfo(name, resolved, priority));
                    break;
                }
            }
        }

        return [.. found.OrderByDescending(ide => ide.Priority)];
    }

    /// <summary>Returns the highest-priority installed IDE, or null if none found.</summary>
    public IdeInfo? GetPreferred() => Detect().FirstOrDefault();

    // ── Resolution ────────────────────────────────────────────

    private static string? Resolve(string candidate)
    {
        // Absolute path given — check directly
        if (Path.IsPathRooted(candidate))
            return File.Exists(candidate) ? candidate : null;

        // Search PATH + extra known locations
        var searchDirs = GetPathDirs().Concat(ExtraSearchPaths).Distinct();

        return searchDirs
            .Select(dir => Path.Combine(dir, candidate))
            .FirstOrDefault(File.Exists);
    }

    private static IEnumerable<string> GetPathDirs() =>
        (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(':', StringSplitOptions.RemoveEmptyEntries);
}
