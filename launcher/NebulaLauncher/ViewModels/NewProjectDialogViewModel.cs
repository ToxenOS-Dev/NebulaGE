using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NebulaLauncher.ViewModels;

// ── Template option data ─────────────────────────────────────────────────────

/// <param name="CardBg">Solid base colour for the card image area (hex string).</param>
/// <param name="BlobColor">Accent blob colour for the decorative glow (hex string).</param>
public record TemplateOption(
    string Id,
    string Label,
    string Description,
    string CardBg    = "#111115",
    string BlobColor = "#8B6FD4",
    bool   IsAvailable = true);

// ── Per-card VM (holds selection state + resolved brushes) ───────────────────

public partial class TemplateCardViewModel : ViewModelBase
{
    public TemplateOption Option { get; }

    [ObservableProperty] private bool _isSelected;

    public string Label        => Option.Label;
    public string Description  => Option.Description;
    public bool   IsAvailable  => Option.IsAvailable;
    public bool   IsComingSoon => !Option.IsAvailable;

    // Pre-built brushes used by the image card in XAML
    public SolidColorBrush CardBgBrush { get; }
    public SolidColorBrush BlobBrush   { get; }

    private readonly Action<TemplateCardViewModel> _onSelect;

    public TemplateCardViewModel(TemplateOption option, bool isSelected,
                                 Action<TemplateCardViewModel> onSelect)
    {
        Option      = option;
        _isSelected = isSelected;
        _onSelect   = onSelect;

        CardBgBrush = new SolidColorBrush(Color.Parse(option.CardBg));
        BlobBrush   = new SolidColorBrush(Color.Parse(option.BlobColor));
    }

    [RelayCommand] private void Select() => _onSelect(this);
}

// ── Dialog VM ────────────────────────────────────────────────────────────────

public partial class NewProjectDialogViewModel : ViewModelBase
{
    // ── Templates ────────────────────────────────────────────
    private static readonly List<TemplateOption> _templates =
    [
        new("empty",   "Empty",   "Bare project with no content",
            CardBg: "#0E0E12", BlobColor: "#8B6FD4", IsAvailable: true),
        new("2d-game", "2D Game", "Side-scrollers, platformers",
            CardBg: "#080F1C", BlobColor: "#3A7ED4", IsAvailable: false),
        new("3d-game", "3D Game", "First-person, open-world",
            CardBg: "#0E0818", BlobColor: "#A04AE8", IsAvailable: false),
    ];

    public IReadOnlyList<TemplateCardViewModel> TemplateCards { get; }

    // ── Observable state ─────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    private string _projectName = string.Empty;

    [ObservableProperty]
    private string _location = DefaultProjectsPath;

    [ObservableProperty]
    private TemplateOption _selectedTemplate;

    // ── Derived ──────────────────────────────────────────────
    public bool CanCreate => !string.IsNullOrWhiteSpace(ProjectName);

    // ── Constructor ──────────────────────────────────────────
    public NewProjectDialogViewModel()
    {
        TemplateCards     = _templates.Select((t, i) =>
            new TemplateCardViewModel(t, i == 0, OnCardSelected)).ToList();
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
