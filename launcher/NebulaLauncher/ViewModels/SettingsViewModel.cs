using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NebulaLauncher.Models;
using NebulaLauncher.Services;

namespace NebulaLauncher.ViewModels;

// ── Per-IDE option VM ─────────────────────────────────────────────────────────

public partial class IdeOptionViewModel : ViewModelBase
{
    public IdeInfo Ide { get; }

    [ObservableProperty] private bool _isSelected;

    public string Name    => Ide.Name;
    public string ExePath => Ide.ExecutablePath;

    private readonly Action<IdeOptionViewModel> _onSelect;

    public IdeOptionViewModel(IdeInfo ide, bool isSelected, Action<IdeOptionViewModel> onSelect)
    {
        Ide         = ide;
        _isSelected = isSelected;
        _onSelect   = onSelect;
    }

    [RelayCommand]
    private void Select() => _onSelect(this);
}

// ── Settings page VM ─────────────────────────────────────────────────────────

public enum SettingsSection { General, Themes }

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IdeDetectionService _ideService = new();

    public ObservableCollection<IdeOptionViewModel> DetectedIdes { get; }

    [ObservableProperty]
    private string _defaultProjectLocation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGeneralActive))]
    [NotifyPropertyChangedFor(nameof(IsThemesActive))]
    private SettingsSection _activeSection = SettingsSection.General;

    public bool IsGeneralActive => ActiveSection == SettingsSection.General;
    public bool IsThemesActive  => ActiveSection == SettingsSection.Themes;

    public bool NoIdesFound => DetectedIdes.Count == 0;

    public ThemesViewModel Themes { get; } = new();

    public SettingsViewModel()
    {
        var settings = SettingsService.Load();

        _defaultProjectLocation = settings.DefaultProjectLocation
            ?? NewProjectDialogViewModel.DefaultProjectsPath;

        var detected     = _ideService.Detect();
        var preferredPath = settings.PreferredIdePath;

        DetectedIdes = new ObservableCollection<IdeOptionViewModel>(
            detected.Select(ide =>
                new IdeOptionViewModel(
                    ide,
                    isSelected: ide.ExecutablePath == preferredPath,
                    onSelect:   OnIdeSelected)));

        // If nothing matched, select the first detected IDE
        if (DetectedIdes.Any() && !DetectedIdes.Any(i => i.IsSelected))
        {
            DetectedIdes[0].IsSelected = true;
            PersistIde(DetectedIdes[0]);
        }
    }

    // ── Commands ──────────────────────────────────────────

    [RelayCommand]
    private void Navigate(SettingsSection section) => ActiveSection = section;

    // Called from SettingsView code-behind after folder picker resolves
    public void SetDefaultLocation(string path)
    {
        DefaultProjectLocation = path;
    }

    // ── Internals ─────────────────────────────────────────

    private void OnIdeSelected(IdeOptionViewModel selected)
    {
        foreach (var ide in DetectedIdes)
            ide.IsSelected = ide == selected;

        PersistIde(selected);
    }

    private static void PersistIde(IdeOptionViewModel ide) =>
        SettingsService.Update(s =>
        {
            s.PreferredIdePath = ide.Ide.ExecutablePath;
            s.PreferredIdeName = ide.Ide.Name;
        });

    partial void OnDefaultProjectLocationChanged(string value) =>
        SettingsService.Update(s => s.DefaultProjectLocation = value);
}
