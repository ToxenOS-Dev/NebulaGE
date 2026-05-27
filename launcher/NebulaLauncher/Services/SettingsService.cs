using System;
using System.IO;
using System.Text.Json;
using NebulaLauncher.Models;

namespace NebulaLauncher.Services;

/// <summary>
/// Loads and saves launcher settings to ~/.local/share/NebulaGE/settings.json
/// </summary>
public static class SettingsService
{
    private static readonly string SettingsPath =
        Path.Combine(ProjectRegistry.DataDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static LauncherSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return new LauncherSettings();

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<LauncherSettings>(json, JsonOptions)
                   ?? new LauncherSettings();
        }
        catch
        {
            return new LauncherSettings();
        }
    }

    public static void Save(LauncherSettings settings)
    {
        Directory.CreateDirectory(ProjectRegistry.DataDir);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    public static void Update(Action<LauncherSettings> mutate)
    {
        var settings = Load();
        mutate(settings);
        Save(settings);
    }
}
