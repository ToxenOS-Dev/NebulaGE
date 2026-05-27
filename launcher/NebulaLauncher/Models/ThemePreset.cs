using System.Text.Json.Serialization;
using Avalonia.Media;

namespace NebulaLauncher.Models;

public class ThemePreset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Custom";

    // ── Backgrounds ──────────────────────────────────────
    [JsonPropertyName("bg")]           public string Bg           { get; set; } = "#0F0F11";
    [JsonPropertyName("surface")]      public string Surface      { get; set; } = "#16161A";
    [JsonPropertyName("surface2")]     public string Surface2     { get; set; } = "#1C1C21";
    [JsonPropertyName("surface3")]     public string Surface3     { get; set; } = "#222228";

    // ── Borders ──────────────────────────────────────────
    [JsonPropertyName("border")]       public string Border       { get; set; } = "#2A2A30";
    [JsonPropertyName("borderSubtle")] public string BorderSubtle { get; set; } = "#1F1F24";

    // ── Text ─────────────────────────────────────────────
    [JsonPropertyName("text")]         public string Text         { get; set; } = "#E2E2E8";
    [JsonPropertyName("textMuted")]    public string TextMuted    { get; set; } = "#8888A0";
    [JsonPropertyName("textSubtle")]   public string TextSubtle   { get; set; } = "#44445A";

    // ── Accent ───────────────────────────────────────────
    [JsonPropertyName("accent")]       public string Accent       { get; set; } = "#8B6FD4";

    // ── Status ───────────────────────────────────────────
    [JsonPropertyName("success")]      public string Success      { get; set; } = "#4CAF7D";
    [JsonPropertyName("warning")]      public string Warning      { get; set; } = "#E8A838";
    [JsonPropertyName("error")]        public string Error        { get; set; } = "#E05C5C";

    // ── Built-in presets ─────────────────────────────────

    public static ThemePreset NebulaDark() => new()
    {
        Name        = "Nebula Dark",
        Bg          = "#0F0F11",
        Surface     = "#16161A",
        Surface2    = "#1C1C21",
        Surface3    = "#222228",
        Border      = "#2A2A30",
        BorderSubtle= "#1F1F24",
        Text        = "#E2E2E8",
        TextMuted   = "#8888A0",
        TextSubtle  = "#44445A",
        Accent      = "#8B6FD4",
        Success     = "#4CAF7D",
        Warning     = "#E8A838",
        Error       = "#E05C5C",
    };

    public static ThemePreset NebulaLight() => new()
    {
        Name        = "Nebula Light",
        Bg          = "#F4F4F8",
        Surface     = "#FFFFFF",
        Surface2    = "#EBEBF0",
        Surface3    = "#E0E0E8",
        Border      = "#D0D0DA",
        BorderSubtle= "#E8E8F0",
        Text        = "#1A1A24",
        TextMuted   = "#5A5A70",
        TextSubtle  = "#9898A8",
        Accent      = "#6B4FC0",
        Success     = "#2E9E5E",
        Warning     = "#C07010",
        Error       = "#C03838",
    };

    public static ThemePreset HighContrast() => new()
    {
        Name        = "High Contrast",
        Bg          = "#000000",
        Surface     = "#0A0A0A",
        Surface2    = "#141414",
        Surface3    = "#1E1E1E",
        Border      = "#555555",
        BorderSubtle= "#333333",
        Text        = "#FFFFFF",
        TextMuted   = "#BBBBBB",
        TextSubtle  = "#888888",
        Accent      = "#AA88FF",
        Success     = "#55FF88",
        Warning     = "#FFCC44",
        Error       = "#FF5555",
    };

    public static ThemePreset MidnightBlue() => new()
    {
        Name        = "Midnight Blue",
        Bg          = "#0A0E1A",
        Surface     = "#111828",
        Surface2    = "#182030",
        Surface3    = "#1E2838",
        Border      = "#2A3448",
        BorderSubtle= "#1A2238",
        Text        = "#D8E4F8",
        TextMuted   = "#7890B8",
        TextSubtle  = "#3A4E6A",
        Accent      = "#4488DD",
        Success     = "#44AA77",
        Warning     = "#DDAA33",
        Error       = "#DD5555",
    };

    public static ThemePreset Dracula() => new()
    {
        Name        = "Dracula",
        Bg          = "#1E1F29",
        Surface     = "#282A36",
        Surface2    = "#343746",
        Surface3    = "#414455",
        Border      = "#44475A",
        BorderSubtle= "#343648",
        Text        = "#F8F8F2",
        TextMuted   = "#6272A4",
        TextSubtle  = "#44475A",
        Accent      = "#BD93F9",
        Success     = "#50FA7B",
        Warning     = "#FFB86C",
        Error       = "#FF5555",
    };

    public static ThemePreset Solarized() => new()
    {
        Name        = "Solarized Dark",
        Bg          = "#002B36",
        Surface     = "#073642",
        Surface2    = "#0B4452",
        Surface3    = "#0F5060",
        Border      = "#1A6070",
        BorderSubtle= "#0A3A48",
        Text        = "#839496",
        TextMuted   = "#586E75",
        TextSubtle  = "#344E55",
        Accent      = "#268BD2",
        Success     = "#859900",
        Warning     = "#B58900",
        Error       = "#DC322F",
    };

    public static ThemePreset[] AllPresets() =>
    [
        NebulaDark(),
        NebulaLight(),
        HighContrast(),
        MidnightBlue(),
        Dracula(),
        Solarized(),
    ];

    // ── Derived color helpers ─────────────────────────────

    /// <summary>Parse a hex string (#RRGGBB or #AARRGGBB) into a Color.</summary>
    public static Color ParseColor(string hex)
    {
        if (Color.TryParse(hex, out var c)) return c;
        return Colors.Magenta; // fallback for bad values
    }

    /// <summary>Re-color with a new alpha (0–255).</summary>
    public static Color WithAlpha(Color c, byte alpha) =>
        new(alpha, c.R, c.G, c.B);

    /// <summary>Estimated luminance (0–1) of an RGB color (simplified).</summary>
    public static double Luminance(Color c) =>
        (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
}
