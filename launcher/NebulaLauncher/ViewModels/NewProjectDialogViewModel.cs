using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NebulaLauncher.Services;

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

    // ── Mode toggle ──────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNewMode))]
    [NotifyPropertyChangedFor(nameof(IsCloneMode))]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    [NotifyPropertyChangedFor(nameof(CreateButtonLabel))]
    private bool _cloneModeActive;

    public bool IsNewMode   => !CloneModeActive;
    public bool IsCloneMode =>  CloneModeActive;

    [RelayCommand] private void SetNewMode()   => CloneModeActive = false;
    [RelayCommand] private void SetCloneMode() => CloneModeActive = true;

    // ── Observable state ─────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    private string _projectName = string.Empty;

    [ObservableProperty]
    private string _location = DefaultProjectsPath;

    [ObservableProperty]
    private TemplateOption _selectedTemplate;

    // ── Clone URL ─────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    [NotifyPropertyChangedFor(nameof(HasValidCloneUrl))]
    [NotifyPropertyChangedFor(nameof(CloneRepoSlug))]
    [NotifyPropertyChangedFor(nameof(CloneUrlError))]
    private string _cloneUrl = string.Empty;

    public bool    HasValidCloneUrl => IsValidGitHubUrl(CloneUrl);
    public string? CloneRepoSlug    => GitHubService.GetRepoSlug(NormalizeCloneUrl(CloneUrl));
    public string? CloneUrlError    =>
        string.IsNullOrWhiteSpace(CloneUrl) || HasValidCloneUrl
            ? null
            : "Paste a full GitHub URL, e.g. https://github.com/user/repo";

    partial void OnCloneUrlChanged(string value)
    {
        // Auto-fill project name from the repo slug when URL becomes valid
        var slug = CloneRepoSlug;
        if (slug is not null)
        {
            var repoName = slug.Split('/').LastOrDefault();
            if (!string.IsNullOrEmpty(repoName))
                ProjectName = repoName;
        }
    }

    // ── Derived ──────────────────────────────────────────────
    public bool   CanCreate         => IsCloneMode
                                           ? HasValidCloneUrl && !string.IsNullOrWhiteSpace(ProjectName)
                                           : !string.IsNullOrWhiteSpace(ProjectName);
    public string CreateButtonLabel => IsCloneMode ? "Clone & Open" : "+ Create Project";

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

    // ── URL helpers ───────────────────────────────────────────
    private static bool IsValidGitHubUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        var norm = NormalizeCloneUrl(url);
        return norm is not null;
    }

    public static string? NormalizeCloneUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        url = url.Trim();

        // SSH: git@github.com:user/repo[.git]
        if (url.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
        {
            var path = url["git@github.com:".Length..];
            if (path.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) path = path[..^4];
            return $"https://github.com/{path}";
        }

        // HTTPS: https://github.com/user/repo[.git]
        if (url.Contains("github.com", StringComparison.OrdinalIgnoreCase))
        {
            if (url.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) url = url[..^4];
            // Make sure it has at least user/repo after github.com/
            var afterHost = url[(url.IndexOf("github.com", StringComparison.OrdinalIgnoreCase) + "github.com".Length)..];
            var parts = afterHost.Trim('/').Split('/');
            if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]))
                return url;
        }

        return null;
    }

    // ── Internal ─────────────────────────────────────────────
    private void OnCardSelected(TemplateCardViewModel selected)
    {
        foreach (var card in TemplateCards)
            card.IsSelected = card == selected;

        SelectedTemplate = selected.Option;
    }
}
