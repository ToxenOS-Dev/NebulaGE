using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using NebulaLauncher.Models;

namespace NebulaLauncher.Services;

/// <summary>
/// Writes theme colors into Application.Current.Resources so every
/// {DynamicResource} binding in the UI reflects the active preset instantly.
/// </summary>
public static class ThemeManager
{
    private static ThemePreset _current = ThemePreset.NebulaDark();

    public static ThemePreset Current => _current;

    public static void Apply(ThemePreset preset)
    {
        _current = preset;
        var res = Application.Current!.Resources;

        // ── Base colors ──────────────────────────────────────────
        var bg        = ThemePreset.ParseColor(preset.Bg);
        var surface   = ThemePreset.ParseColor(preset.Surface);
        var surface2  = ThemePreset.ParseColor(preset.Surface2);
        var surface3  = ThemePreset.ParseColor(preset.Surface3);
        var border    = ThemePreset.ParseColor(preset.Border);
        var borderSub = ThemePreset.ParseColor(preset.BorderSubtle);
        var text      = ThemePreset.ParseColor(preset.Text);
        var textMuted = ThemePreset.ParseColor(preset.TextMuted);
        var textSub   = ThemePreset.ParseColor(preset.TextSubtle);
        var accent    = ThemePreset.ParseColor(preset.Accent);
        var success   = ThemePreset.ParseColor(preset.Success);
        var warning   = ThemePreset.ParseColor(preset.Warning);
        var error     = ThemePreset.ParseColor(preset.Error);

        // Derived accent variants
        var accentHover    = Lighten(accent, 0.12f);
        var accentPressed  = Darken(accent, 0.10f);
        var accentSurface  = ThemePreset.WithAlpha(accent, 24);   // ~9% alpha
        var accentSurface2 = ThemePreset.WithAlpha(accent, 37);   // ~15% alpha

        // Derived error variants
        var errorSurface = ThemePreset.WithAlpha(error, 22);
        var errorBorder  = ThemePreset.WithAlpha(error, 64);
        var errorText    = Lighten(error, 0.25f);

        // Glass surfaces — semi-transparent for layered depth
        var glassSurface  = ThemePreset.WithAlpha(surface, 217);  // surface @ 85% — toolbars, tab bars
        var glassBg       = ThemePreset.WithAlpha(bg, 204);        // bg @ 80%  — deep panel backgrounds
        var glassCard     = ThemePreset.WithAlpha(surface2, 204);  // surface2 @ 80% — floating cards

        // Hover/press overlays — invert for light themes
        bool isLight = ThemePreset.Luminance(bg) > 0.5;
        var hoverOverlay = isLight
            ? Color.FromArgb(13, 0, 0, 0)      // black 5%
            : Color.FromArgb(13, 255, 255, 255); // white 5%
        var pressOverlay = isLight
            ? Color.FromArgb(7, 0, 0, 0)
            : Color.FromArgb(7, 255, 255, 255);

        // ── Write colors ─────────────────────────────────────────
        Set(res, "NebulaBg",           bg);
        Set(res, "NebulaSurface",      surface);
        Set(res, "NebulaSurface2",     surface2);
        Set(res, "NebulaSurface3",     surface3);
        Set(res, "NebulaBorder",       border);
        Set(res, "NebulaBorderSubtle", borderSub);
        Set(res, "NebulaText",         text);
        Set(res, "NebulaTextMuted",    textMuted);
        Set(res, "NebulaTextSubtle",   textSub);
        Set(res, "NebulaAccent",       accent);
        Set(res, "NebulaAccentHover",  accentHover);
        Set(res, "NebulaAccentPressed",accentPressed);
        Set(res, "NebulaSuccess",      success);
        Set(res, "NebulaWarning",      warning);
        Set(res, "NebulaError",        error);

        // Git status colors track semantic colors
        Set(res, "NebulaGitModified",  warning);
        Set(res, "NebulaGitNew",       success);
        Set(res, "NebulaGitDeleted",   error);
        Set(res, "NebulaGitConflict",  error);

        // ── Write brushes ────────────────────────────────────────
        SetBrush(res, "NebulaBgBrush",            bg);
        SetBrush(res, "NebulaSurfaceBrush",        surface);
        SetBrush(res, "NebulaSurface2Brush",       surface2);
        SetBrush(res, "NebulaSurface3Brush",       surface3);
        SetBrush(res, "NebulaBorderBrush",         border);
        SetBrush(res, "NebulaBorderSubtleBrush",   borderSub);
        SetBrush(res, "NebulaTextBrush",           text);
        SetBrush(res, "NebulaTextMutedBrush",      textMuted);
        SetBrush(res, "NebulaTextSubtleBrush",     textSub);
        SetBrush(res, "NebulaAccentBrush",         accent);
        SetBrush(res, "NebulaAccentHoverBrush",    accentHover);
        SetBrush(res, "NebulaAccentPressedBrush",  accentPressed);
        SetBrush(res, "NebulaSuccessBrush",        success);
        SetBrush(res, "NebulaWarningBrush",        warning);
        SetBrush(res, "NebulaErrorBrush",          error);

        // Derived surface brushes
        SetBrush(res, "NebulaAccentSurfaceBrush",  accentSurface);
        SetBrush(res, "NebulaAccentSurface2Brush", accentSurface2);
        SetBrush(res, "NebulaErrorSurfaceBrush",   errorSurface);
        SetBrush(res, "NebulaErrorBorderBrush",    errorBorder);
        SetBrush(res, "NebulaErrorTextBrush",      errorText);
        SetBrush(res, "NebulaHoverOverlayBrush",    hoverOverlay);
        SetBrush(res, "NebulaPressOverlayBrush",   pressOverlay);

        // Glass / depth surfaces
        SetBrush(res, "NebulaGlassSurfaceBrush",   glassSurface);
        SetBrush(res, "NebulaGlassBgBrush",        glassBg);
        SetBrush(res, "NebulaGlassCardBrush",      glassCard);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static void Set(IResourceDictionary res, string key, Color value) =>
        res[key] = value;

    private static void SetBrush(IResourceDictionary res, string key, Color value) =>
        res[key] = new SolidColorBrush(value);

    private static Color Lighten(Color c, float amount)
    {
        float r = Math.Min(1f, c.R / 255f + amount);
        float g = Math.Min(1f, c.G / 255f + amount);
        float b = Math.Min(1f, c.B / 255f + amount);
        return new Color(c.A, (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

    private static Color Darken(Color c, float amount)
    {
        float r = Math.Max(0f, c.R / 255f - amount);
        float g = Math.Max(0f, c.G / 255f - amount);
        float b = Math.Max(0f, c.B / 255f - amount);
        return new Color(c.A, (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }
}
