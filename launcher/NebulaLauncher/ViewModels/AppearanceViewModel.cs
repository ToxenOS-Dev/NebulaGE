using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using NebulaLauncher.Models;
using NebulaLauncher.Services;

namespace NebulaLauncher.ViewModels;

/// <summary>
/// Singleton that owns the visual-effects toggles and writes them to
/// Application.Current.Resources so every {DynamicResource} binding
/// in XAML reacts without any VM-to-VM wiring.
/// </summary>
public partial class AppearanceViewModel : ViewModelBase
{
    public static AppearanceViewModel Current { get; } = new();

    [ObservableProperty] private bool _ambientGlowEnabled;
    [ObservableProperty] private bool _sidebarBlurEnabled;
    [ObservableProperty] private bool _glassSurfacesEnabled;

    private AppearanceViewModel()
    {
        var s = SettingsService.Load();
        _ambientGlowEnabled  = s.AmbientGlowEnabled;
        _sidebarBlurEnabled  = s.SidebarBlurEnabled;
        _glassSurfacesEnabled = s.GlassSurfacesEnabled;
    }

    /// <summary>Write all flags to Application.Current.Resources.</summary>
    public void Apply()
    {
        var res = Application.Current!.Resources;

        // Booleans for IsVisible bindings
        res["NebulaGlowBlobsEnabled"]  = AmbientGlowEnabled;
        res["NebulaSidebarBlurEnabled"] = SidebarBlurEnabled;
        res["NebulaGlassEnabled"]       = GlassSurfacesEnabled;

        // Toolbar / panel background: glass if enabled, else solid surface
        var surfaceColor = ThemePreset.ParseColor(ThemeManager.Current.Surface);
        var toolbarColor = GlassSurfacesEnabled
            ? ThemePreset.WithAlpha(surfaceColor, 217)  // 85%
            : surfaceColor;
        res["NebulaToolbarBrush"] = new SolidColorBrush(toolbarColor);
    }

    partial void OnAmbientGlowEnabledChanged(bool value)
    {
        SettingsService.Update(s => s.AmbientGlowEnabled = value);
        Apply();
    }
    partial void OnSidebarBlurEnabledChanged(bool value)
    {
        SettingsService.Update(s => s.SidebarBlurEnabled = value);
        Apply();
    }
    partial void OnGlassSurfacesEnabledChanged(bool value)
    {
        SettingsService.Update(s => s.GlassSurfacesEnabled = value);
        Apply();
    }
}
