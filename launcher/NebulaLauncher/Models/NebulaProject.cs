using System;
using System.Text.Json.Serialization;

namespace NebulaLauncher.Models;

/// <summary>
/// Represents a NebulaGE project entry. This is both stored in
/// the launcher registry and written as the project.nebula file
/// inside the project folder.
/// </summary>
public class NebulaProject
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("engineVersion")]
    public string EngineVersion { get; set; } = "0.1.0";

    [JsonPropertyName("template")]
    public string Template { get; set; } = "empty";

    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("lastOpened")]
    public DateTime LastOpened { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("hasGitRepo")]
    public bool HasGitRepo { get; set; }

    [JsonPropertyName("gitBranch")]
    public string? GitBranch { get; set; }
}
