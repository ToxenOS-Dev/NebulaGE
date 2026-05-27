using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NebulaLauncher.Models;

namespace NebulaLauncher.Services;

/// <summary>Persists installed engine builds to ~/.local/share/NebulaGE/engines.json.</summary>
public class EngineRegistry
{
    private static readonly string FilePath =
        Path.Combine(ProjectRegistry.DataDir, "engines.json");

    private static readonly JsonSerializerOptions _json =
        new() { WriteIndented = true };

    [JsonPropertyName("engines")]
    public List<EngineInstall> Engines { get; set; } = [];

    // ── Load / save ──────────────────────────────────────────

    public static EngineRegistry Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new EngineRegistry();
            return JsonSerializer.Deserialize<EngineRegistry>(
                       File.ReadAllText(FilePath)) ?? new EngineRegistry();
        }
        catch { return new EngineRegistry(); }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, _json));
        }
        catch { }
    }

    // ── Mutations (each saves immediately) ───────────────────

    public void AddOrUpdate(EngineInstall install)
    {
        Engines.RemoveAll(e => e.Path == install.Path);
        Engines.Insert(0, install);
        Save();
    }

    public void Remove(EngineInstall install)
    {
        Engines.RemoveAll(e => e.Path == install.Path);

        // Reassign default to the first remaining entry if we removed it
        if (install.IsDefault && Engines.Count > 0)
            Engines[0].IsDefault = true;

        Save();
    }

    public void SetDefault(EngineInstall install)
    {
        foreach (var e in Engines) e.IsDefault = false;
        var target = Engines.FirstOrDefault(e => e.Path == install.Path);
        if (target is not null) target.IsDefault = true;
        Save();
    }
}
