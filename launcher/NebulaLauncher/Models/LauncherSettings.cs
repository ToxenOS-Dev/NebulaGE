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

    // ── Visual effects (all OFF by default) ──────────────────────
    /// <summary>Ambient glow blobs in the window background.</summary>
    [JsonPropertyName("ambientGlowEnabled")]
    public bool AmbientGlowEnabled { get; set; } = false;

    /// <summary>Acrylic/blur behind the sidebar.</summary>
    [JsonPropertyName("sidebarBlurEnabled")]
    public bool SidebarBlurEnabled { get; set; } = false;

    /// <summary>Glass/frosted surfaces on toolbar and cards.</summary>
    [JsonPropertyName("glassSurfacesEnabled")]
    public bool GlassSurfacesEnabled { get; set; } = false;

    // ── Project hub view mode ─────────────────────────────────────────────────
    /// <summary>True = grid card view; false = compact list view.</summary>
    [JsonPropertyName("projectGridView")]
    public bool ProjectGridView { get; set; } = false;
}
