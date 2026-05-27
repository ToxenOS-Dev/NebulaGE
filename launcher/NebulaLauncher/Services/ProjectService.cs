using System;
using System.IO;
using System.Text.Json;
using NebulaLauncher.Models;

namespace NebulaLauncher.Services;

/// <summary>
/// Handles project creation, opening, and on-disk layout.
/// </summary>
public class ProjectService
{
    private const string ProjectFileName = "project.nebula";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // ── Create ────────────────────────────────────────────────

    /// <summary>
    /// Creates a new project directory structure, writes project.nebula,
    /// generates .gitignore, and registers the project.
    /// </summary>
    public NebulaProject Create(string name, string parentDirectory, string template = "empty")
    {
        var projectPath = Path.Combine(parentDirectory, name);

        if (Directory.Exists(projectPath))
            throw new InvalidOperationException($"Directory already exists: {projectPath}");

        // Standard project layout
        Directory.CreateDirectory(projectPath);
        Directory.CreateDirectory(Path.Combine(projectPath, "assets"));
        Directory.CreateDirectory(Path.Combine(projectPath, "scenes"));
        Directory.CreateDirectory(Path.Combine(projectPath, "scripts"));
        Directory.CreateDirectory(Path.Combine(projectPath, "shaders"));
        Directory.CreateDirectory(Path.Combine(projectPath, "build"));

        var project = new NebulaProject
        {
            Name       = name,
            Path       = projectPath,
            Template   = template,
            Created    = DateTime.UtcNow,
            LastOpened = DateTime.UtcNow,
        };

        WriteProjectFile(project);
        WriteGitIgnore(projectPath, name);

        ProjectRegistry.Load().AddOrUpdate(project);
        return project;
    }

    // ── Open ──────────────────────────────────────────────────

    /// <summary>
    /// Registers and opens an existing project by path.
    /// Returns null if the directory doesn't contain a valid project.nebula.
    /// </summary>
    public NebulaProject? Open(string path)
    {
        var projectFile = Path.Combine(path, ProjectFileName);

        if (!File.Exists(projectFile))
            return null;

        NebulaProject project;
        try
        {
            var json = File.ReadAllText(projectFile);
            project = JsonSerializer.Deserialize<NebulaProject>(json, JsonOptions)
                      ?? new NebulaProject { Path = path };
        }
        catch
        {
            project = new NebulaProject { Path = path };
        }

        // Ensure path is always canonical
        project.Path       = path;
        project.LastOpened = DateTime.UtcNow;

        ProjectRegistry.Load().AddOrUpdate(project);
        return project;
    }

    // ── Helpers ───────────────────────────────────────────────

    private static void WriteProjectFile(NebulaProject project)
    {
        var json = JsonSerializer.Serialize(project, JsonOptions);
        File.WriteAllText(Path.Combine(project.Path, ProjectFileName), json);
    }

    private static void WriteGitIgnore(string projectPath, string projectName)
    {
        var content = $"""
            # NebulaGE — {projectName}

            # Build output
            build/
            .nebula-cache/

            # Compiled shaders
            shaders/*.spv

            # OS
            .DS_Store
            Thumbs.db
            *~

            # Editor
            .idea/
            .vscode/settings.json
            *.swp
            """;

        File.WriteAllText(Path.Combine(projectPath, ".gitignore"), content);
    }
}
