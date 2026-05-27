using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NebulaLauncher.ViewModels;

// ── Template option data ─────────────────────────────────────────────────────

public record TemplateOption(string Id, string Label, string Description, bool IsAvailable = true);

// ── Per-card VM (holds selection state) ──────────────────────────────────────

public partial class TemplateCardViewModel : ViewModelBase
{
    public TemplateOption Option { get; }

    [ObservableProperty]
    private bool _isSelected;

    public string Label       => Option.Label;
    public string Description => Option.Description;
    public bool   IsAvailable => Option.IsAvailable;

    private readonly Action<TemplateCardViewModel> _onSelect;

    public TemplateCardViewModel(TemplateOption option, bool isSelected, Action<TemplateCardViewModel> onSelect)
    {
        Option      = option;
        _isSelected = isSelected;
        _onSelect   = onSelect;
    }

    [RelayCommand]
    private void Select() => _onSelect(this);
}

// ── Dialog VM ────────────────────────────────────────────────────────────────

public partial class NewProjectDialogViewModel : ViewModelBase
{
    // ── Templates ────────────────────────────────────────────
    private static readonly List<TemplateOption> _templates =
    [
        new("empty",   "Empty",   "Bare project with no content", IsAvailable: true),
        new("2d-game", "2D Game", "Coming soon",                  IsAvailable: false),
        new("3d-game", "3D Game", "Coming soon",                  IsAvailable: false),
    ];

    public IReadOnlyList<TemplateCardViewModel> TemplateCards { get; }

    // ── Observable state ─────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    [NotifyPropertyChangedFor(nameof(FullPath))]
    private string _projectName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullPath))]
    private string _location = DefaultProjectsPath;

    [ObservableProperty]
    private TemplateOption _selectedTemplate;

    // ── Derived ──────────────────────────────────────────────
    public bool   CanCreate => !string.IsNullOrWhiteSpace(ProjectName);
    public string FullPath  => string.IsNullOrWhiteSpace(ProjectName)
        ? Location
        : Path.Combine(Location, ProjectName.Trim());

    // ── Constructor ──────────────────────────────────────────
    public NewProjectDialogViewModel()
    {
        TemplateCards    = _templates.Select((t, i) => new TemplateCardViewModel(t, i == 0, OnCardSelected)).ToList();
        _selectedTemplate = _templates[0];
    }

    // ── Default path ─────────────────────────────────────────
    public static string DefaultProjectsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Projects");

    // ── Internal ─────────────────────────────────────────────
    private void OnCardSelected(TemplateCardViewModel selected)
    {
        foreach (var card in TemplateCards)
            card.IsSelected = card == selected;

        SelectedTemplate = selected.Option;
    }
}
