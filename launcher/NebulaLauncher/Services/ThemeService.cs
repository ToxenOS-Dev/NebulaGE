using System.IO;
using System.Linq;
using System.Text.Json;
using NebulaLauncher.Models;

namespace NebulaLauncher.Services;

public static class ThemeService
{
    private static readonly string ThemePath =
        Path.Combine(ProjectRegistry.DataDir, "theme.json");

    private static readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
    };

    /// <summary>Load the active preset from disk (or return Nebula Dark).</summary>
    public static ThemePreset LoadActive()
    {
        try
        {
            if (!File.Exists(ThemePath)) return ThemePreset.NebulaDark();

            var json    = File.ReadAllText(ThemePath);
            var preset  = JsonSerializer.Deserialize<ThemePreset>(json, _opts);
            return preset ?? ThemePreset.NebulaDark();
        }
        catch
        {
            return ThemePreset.NebulaDark();
        }
    }

    /// <summary>Persist the active preset to disk.</summary>
    public static void Save(ThemePreset preset)
    {
        Directory.CreateDirectory(ProjectRegistry.DataDir);
        var json = JsonSerializer.Serialize(preset, _opts);
        File.WriteAllText(ThemePath, json);
    }

    /// <summary>Find a built-in preset by name, or return null.</summary>
    public static ThemePreset? FindBuiltin(string name) =>
        ThemePreset.AllPresets().FirstOrDefault(p => p.Name == name);
}
