using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace NebulaLauncher.Models;

/// <summary>
/// Manages the persisted list of known NebulaGE projects.
/// Stored at ~/.local/share/NebulaGE/registry.json (XDG-compliant).
/// </summary>
public class ProjectRegistry
{
    // XDG_DATA_HOME / NebulaGE
    public static readonly string DataDir =
        Path.Combine(
            Environment.GetEnvironmentVariable("XDG_DATA_HOME")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share"),
            "NebulaGE");

    private static readonly string RegistryPath = Path.Combine(DataDir, "registry.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly List<NebulaProject> _projects = [];

    public IReadOnlyList<NebulaProject> Projects =>
        [.. _projects.OrderByDescending(p => p.LastOpened)];

    // ── Load ──────────────────────────────────────────────────

    public static ProjectRegistry Load()
    {
        var registry = new ProjectRegistry();

        if (!File.Exists(RegistryPath))
            return registry;

        try
        {
            var json = File.ReadAllText(RegistryPath);
            var projects = JsonSerializer.Deserialize<List<NebulaProject>>(json, JsonOptions);
            if (projects is not null)
                registry._projects.AddRange(projects);
        }
        catch
        {
            // Registry missing or corrupt — start fresh, don't crash launcher
        }

        return registry;
    }

    // ── Mutations ─────────────────────────────────────────────

    /// <summary>Add or update a project by path, then persist.</summary>
    public void AddOrUpdate(NebulaProject project)
    {
        var existing = _projects.FirstOrDefault(p => p.Path == project.Path);
        if (existing is not null)
            _projects.Remove(existing);

        _projects.Add(project);
        Save();
    }

    /// <summary>Remove a project from the registry (does NOT delete files).</summary>
    public void Remove(NebulaProject project)
    {
        _projects.Remove(project);
        Save();
    }

    // ── Queries ───────────────────────────────────────────────

    public NebulaProject? FindByPath(string path) =>
        _projects.FirstOrDefault(p => p.Path == path);

    // ── Persist ───────────────────────────────────────────────

    public void Save()
    {
        Directory.CreateDirectory(DataDir);
        var json = JsonSerializer.Serialize(_projects, JsonOptions);
        File.WriteAllText(RegistryPath, json);
    }
}
