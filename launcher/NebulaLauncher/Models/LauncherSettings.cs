using System.Text.Json.Serialization;

namespace NebulaLauncher.Models;

public class LauncherSettings
{
    /// <summary>False after first run completes IDE detection.</summary>
    [JsonPropertyName("firstRun")]
    public bool FirstRun { get; set; } = true;

    /// <summary>Full path to the preferred IDE executable.</summary>
    [JsonPropertyName("preferredIdePath")]
    public string? PreferredIdePath { get; set; }

    /// <summary>Human-readable name of the preferred IDE.</summary>
    [JsonPropertyName("preferredIdeName")]
    public string? PreferredIdeName { get; set; }

    /// <summary>Default parent directory for new projects.</summary>
    [JsonPropertyName("defaultProjectLocation")]
    public string? DefaultProjectLocation { get; set; }
}
