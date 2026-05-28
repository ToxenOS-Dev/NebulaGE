using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NebulaLauncher.Services;

/// <summary>
/// Scans system fonts via fc-list (Linux/FreeDesktop) and returns sorted family names
/// suitable for display in a font picker UI.
/// </summary>
public static class FontScanService
{
    // Substrings that mark a font as an icon/symbol font that shouldn't appear in the UI picker
    private static readonly string[] _iconKeywords =
    [
        "icon", "symbol", "emoji", "nerd", "awesome", "material",
        "octicons", "feather", "boxicons", "phosphor", "lucide",
        "codicon", "powerline", "weather", "webdings", "wingdings",
        "braille", "d050000l",
    ];

    // ── Public API ────────────────────────────────────────────

    /// <summary>Returns all usable UI font families, deduplicated and sorted.</summary>
    public static List<string> GetUIFonts() => Scan(monoOnly: false);

    /// <summary>Returns monospace families only (for the mono font picker).</summary>
    public static List<string> GetMonoFonts() => Scan(monoOnly: true);

    // ── Internals ─────────────────────────────────────────────

    private static List<string> Scan(bool monoOnly)
    {
        var fcArgs = monoOnly
            ? ":spacing=mono: family"  // fc-list filter for monospace only
            : ": family";

        var raw = RunFcList(fcArgs);
        if (raw.Count > 0)
            return raw;

        // Fallback — no fc-list available
        return monoOnly
            ? ["Liberation Mono", "DejaVu Sans Mono", "Courier New"]
            : ["Cantarell", "Liberation Sans", "DejaVu Sans", "Arial"];
    }

    private static List<string> RunFcList(string args)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo("fc-list", args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            });

            if (proc is null) return [];

            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(6000);

            return output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                // Each line may look like: "DejaVu Sans Mono,DejaVu Sans Mono Bold:style=Bold"
                // Take only the first family name before ',' or ':'
                .Select(line =>
                {
                    var name = line.Split(':', StringSplitOptions.RemoveEmptyEntries)[0]
                                   .Split(',',  StringSplitOptions.RemoveEmptyEntries)[0]
                                   .Trim();
                    return name;
                })
                .Where(name =>
                    name.Length > 0 &&
                    !IsIconFont(name) &&
                    // Must start with a printable ASCII letter or digit
                    (char.IsLetter(name[0]) || char.IsDigit(name[0])))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static bool IsIconFont(string name)
    {
        var lower = name.ToLowerInvariant();
        return _iconKeywords.Any(kw => lower.Contains(kw));
    }
}
