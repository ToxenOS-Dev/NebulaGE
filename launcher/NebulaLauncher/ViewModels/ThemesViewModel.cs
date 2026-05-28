using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NebulaLauncher.Models;
using NebulaLauncher.Services;

// Alias so we can call Avalonia's FontFamily without ambiguity
using AvaloniaFontFamily = Avalonia.Media.FontFamily;

namespace NebulaLauncher.ViewModels;

// ── Single color row ──────────────────────────────────────────────────────────

public partial class ColorEntryViewModel : ViewModelBase
{
    public string Label        { get; }
    public string PropertyName { get; }

    private readonly Action<ColorEntryViewModel> _onChange;
    private bool _syncing;

    [ObservableProperty] private Color  _color;
    [ObservableProperty] private string _hexInput = "#000000";

    // Live preview brush so the swatch reflects the current color
    [ObservableProperty] private SolidColorBrush _previewBrush = new(Colors.Black);

    public ColorEntryViewModel(string label, string propertyName, Color initial,
                               Action<ColorEntryViewModel> onChange)
    {
        Label        = label;
        PropertyName = propertyName;
        _onChange    = onChange;
        SetColorSilent(initial);
    }

    partial void OnColorChanged(Color value)
    {
        if (_syncing) return;
        _syncing = true;
        HexInput     = ColorToHex(value);
        PreviewBrush = new SolidColorBrush(value);
        _syncing = false;
        _onChange(this);
    }

    partial void OnHexInputChanged(string value)
    {
        if (_syncing) return;
        var normalized = NormalizeHex(value);
        if (Color.TryParse(normalized, out var c))
        {
            _syncing = true;
            Color        = c;
            PreviewBrush = new SolidColorBrush(c);
            _syncing = false;
            _onChange(this);
        }
    }

    /// <summary>Update color from preset without firing _onChange.</summary>
    public void SetColorSilent(Color c)
    {
        _syncing     = true;
        Color        = c;
        HexInput     = ColorToHex(c);
        PreviewBrush = new SolidColorBrush(c);
        _syncing     = false;
    }

    private static string ColorToHex(Color c) =>
        $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private static string NormalizeHex(string s)
    {
        s = s.Trim();
        if (!s.StartsWith('#')) s = "#" + s;
        return s;
    }
}

// ── Section (group of color rows) ─────────────────────────────────────────────

public class ColorSectionViewModel
{
    public string Title  { get; }
    public ObservableCollection<ColorEntryViewModel> Entries { get; } = [];

    public ColorSectionViewModel(string title) => Title = title;
}

// ── Preset option ─────────────────────────────────────────────────────────────

public partial class PresetOptionViewModel : ViewModelBase
{
    public ThemePreset Preset { get; }

    [ObservableProperty] private bool _isSelected;

    private readonly Action<PresetOptionViewModel> _onSelect;

    public string          Name        => Preset.Name;

    /// <summary>Accent swatch shown on the preset chip so users can see the color at a glance.</summary>
    public SolidColorBrush AccentBrush => new(ThemePreset.ParseColor(Preset.Accent));

    public PresetOptionViewModel(ThemePreset preset, bool isSelected,
                                 Action<PresetOptionViewModel> onSelect)
    {
        Preset      = preset;
        _isSelected = isSelected;
        _onSelect   = onSelect;
    }

    [RelayCommand]
    private void Select() => _onSelect(this);
}

// ── Themes page VM ────────────────────────────────────────────────────────────

public partial class ThemesViewModel : ViewModelBase
{
    private ThemePreset _activePreset;

    public ObservableCollection<PresetOptionViewModel> Presets { get; } = [];
    public ObservableCollection<ColorSectionViewModel> Sections { get; } = [];

    /// <summary>
    /// Exposes the singleton so ThemesView can bind the visual-effects toggles
    /// without any extra plumbing.
    /// </summary>
    public AppearanceViewModel Appearance => AppearanceViewModel.Current;

    // ── Font picker ──────────────────────────────────────

    public ObservableCollection<string> UIFonts   { get; } = [];
    public ObservableCollection<string> MonoFonts { get; } = [];

    [ObservableProperty] private string? _selectedUIFont;
    [ObservableProperty] private string? _selectedMonoFont;

    partial void OnSelectedUIFontChanged(string? value)
    {
        if (value is null) return;
        ApplyFont("NebulaUIFontFamily", value);
        SettingsService.Update(s => s.UiFontFamily = value);
    }

    partial void OnSelectedMonoFontChanged(string? value)
    {
        if (value is null) return;
        ApplyFont("NebulaMonoFontFamily", value);
        SettingsService.Update(s => s.MonoFontFamily = value);
    }

    private static void ApplyFont(string resourceKey, string familyName)
    {
        if (Avalonia.Application.Current is { } app)
            app.Resources[resourceKey] = AvaloniaFontFamily.Parse(familyName);
    }

    public ThemesViewModel()
    {
        _activePreset = ThemeService.LoadActive();
        BuildPresets();
        BuildSections();
        LoadFontsAsync();
    }

    private void LoadFontsAsync()
    {
        var settings = SettingsService.Load();

        // Load font lists on a background thread — fc-list can take ~200 ms
        System.Threading.Tasks.Task.Run(() =>
        {
            var uiFonts   = FontScanService.GetUIFonts();
            var monoFonts = FontScanService.GetMonoFonts();

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UIFonts.Clear();
                foreach (var f in uiFonts) UIFonts.Add(f);

                MonoFonts.Clear();
                foreach (var f in monoFonts) MonoFonts.Add(f);

                // Restore saved selections (without triggering the Apply side-effects)
                // Assign backing fields directly to restore saved values without
                // triggering OnSelectedXxxFontChanged (which would call Apply+Save).
#pragma warning disable MVVMTK0034
                _selectedUIFont   = settings.UiFontFamily   ?? BestMatch(UIFonts,   "Inter", "Cantarell");
                _selectedMonoFont = settings.MonoFontFamily ?? BestMatch(MonoFonts, "Cascadia Code", "Liberation Mono");
#pragma warning restore MVVMTK0034

                OnPropertyChanged(nameof(SelectedUIFont));
                OnPropertyChanged(nameof(SelectedMonoFont));
            });
        });
    }

    /// Returns the first name from <paramref name="preferred"/> that exists in
    /// <paramref name="list"/>, falling back to the first item in the list.
    private static string? BestMatch(ObservableCollection<string> list, params string[] preferred)
    {
        foreach (var name in preferred)
            if (list.Contains(name, StringComparer.OrdinalIgnoreCase)) return name;
        return list.FirstOrDefault();
    }

    // ── Preset strip ─────────────────────────────────────

    private void BuildPresets()
    {
        Presets.Clear();
        foreach (var preset in ThemePreset.AllPresets())
        {
            Presets.Add(new PresetOptionViewModel(
                preset,
                isSelected: preset.Name == _activePreset.Name,
                onSelect:   OnPresetSelected));
        }
        // If active is custom (doesn't match any built-in), still show it
        if (!Presets.Any(p => p.IsSelected))
        {
            Presets[0].IsSelected = true;
        }
    }

    private void OnPresetSelected(PresetOptionViewModel selected)
    {
        foreach (var p in Presets) p.IsSelected = p == selected;

        _activePreset = selected.Preset;
        // Rebuild sections so hex inputs and swatches update
        RefreshSectionColors();
        ThemeManager.Apply(_activePreset);
        // Recompute NebulaToolbarBrush — it depends on surface color + glass toggle
        AppearanceViewModel.Current.Apply();
        ThemeService.Save(_activePreset);
    }

    // ── Color sections ───────────────────────────────────

    private void BuildSections()
    {
        Sections.Clear();

        Sections.Add(MakeSection("Backgrounds", new[]
        {
            ("Background",  nameof(ThemePreset.Bg)),
            ("Surface",     nameof(ThemePreset.Surface)),
            ("Surface 2",   nameof(ThemePreset.Surface2)),
            ("Surface 3",   nameof(ThemePreset.Surface3)),
        }));

        Sections.Add(MakeSection("Borders", new[]
        {
            ("Border",        nameof(ThemePreset.Border)),
            ("Border Subtle", nameof(ThemePreset.BorderSubtle)),
        }));

        Sections.Add(MakeSection("Text", new[]
        {
            ("Primary",  nameof(ThemePreset.Text)),
            ("Muted",    nameof(ThemePreset.TextMuted)),
            ("Subtle",   nameof(ThemePreset.TextSubtle)),
        }));

        Sections.Add(MakeSection("Accent", new[]
        {
            ("Accent — buttons & highlights", nameof(ThemePreset.Accent)),
        }));

        Sections.Add(MakeSection("Status", new[]
        {
            ("Success", nameof(ThemePreset.Success)),
            ("Warning", nameof(ThemePreset.Warning)),
            ("Error",   nameof(ThemePreset.Error)),
        }));
    }

    private ColorSectionViewModel MakeSection(string title,
        (string Label, string PropName)[] entries)
    {
        var section = new ColorSectionViewModel(title);
        foreach (var (label, propName) in entries)
        {
            var initial = GetPresetColor(propName);
            section.Entries.Add(new ColorEntryViewModel(label, propName, initial,
                OnColorEntryChanged));
        }
        return section;
    }

    private void OnColorEntryChanged(ColorEntryViewModel entry)
    {
        // Write the new color back into the active preset via reflection
        SetPresetColor(entry.PropertyName, entry.Color);
        // Deselect any built-in preset since we've diverged
        foreach (var p in Presets) p.IsSelected = false;
        // Apply live — recompute toolbar brush too (depends on surface color)
        ThemeManager.Apply(_activePreset);
        AppearanceViewModel.Current.Apply();
        ThemeService.Save(_activePreset);
    }

    private void RefreshSectionColors()
    {
        foreach (var section in Sections)
            foreach (var entry in section.Entries)
                entry.SetColorSilent(GetPresetColor(entry.PropertyName));
    }

    // ── Reflection helpers ───────────────────────────────

    private Color GetPresetColor(string propName)
    {
        var hex = typeof(ThemePreset)
            .GetProperty(propName, BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(_activePreset) as string ?? "#FF00FF";
        return ThemePreset.ParseColor(hex);
    }

    private void SetPresetColor(string propName, Color c)
    {
        var hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        typeof(ThemePreset)
            .GetProperty(propName, BindingFlags.Public | BindingFlags.Instance)
            ?.SetValue(_activePreset, hex);
    }
}
