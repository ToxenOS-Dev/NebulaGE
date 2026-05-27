using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NebulaLauncher.Models;
using NebulaLauncher.Services;

namespace NebulaLauncher.ViewModels;

// ── Per-project item VM ───────────────────────────────────────────────────────

public partial class ProjectItemViewModel : ViewModelBase
{
    public NebulaProject Project { get; }

    private readonly Action<ProjectItemViewModel> _onOpen;
    private readonly Action<ProjectItemViewModel> _onRemove;

    public string  Name              => Project.Name;
    public string  Path              => Project.Path;
    public string  EngineVersion     => $"v{Project.EngineVersion}";
    public bool    HasGitRepo        => Project.HasGitRepo;
    public string? GitBranch         => Project.GitBranch;
    public string  LastOpenedDisplay => FormatDate(Project.LastOpened);

    public ProjectItemViewModel(
        NebulaProject project,
        Action<ProjectItemViewModel> onOpen,
        Action<ProjectItemViewModel> onRemove)
    {
        Project   = project;
        _onOpen   = onOpen;
        _onRemove = onRemove;
    }

    // ── Commands ──────────────────────────────────────────────

    [RelayCommand]
    private void Open() => _onOpen(this);

    [RelayCommand]
    private void Remove() => _onRemove(this);

    [RelayCommand]
    private void OpenInIde()
    {
        var settings = SettingsService.Load();
        if (settings.PreferredIdePath is null) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName        = settings.PreferredIdePath,
                Arguments       = $"\"{Project.Path}\"",
                UseShellExecute = false,
            });
        }
        catch { /* IDE not found or failed to launch — TODO: surface error */ }
    }

    [RelayCommand]
    private void ShowInFiles()
    {
        try
        {
            // xdg-open opens the folder in whatever file manager the DE uses
            Process.Start(new ProcessStartInfo
            {
                FileName        = "xdg-open",
                Arguments       = $"\"{Project.Path}\"",
                UseShellExecute = false,
            });
        }
        catch { /* xdg-open unavailable */ }
    }

    // ── Helpers ───────────────────────────────────────────────

    private static string FormatDate(DateTime dt)
    {
        var diff = DateTime.UtcNow - dt;

        return diff.TotalMinutes < 1  ? "Just now"
             : diff.TotalHours   < 1  ? $"{(int)diff.TotalMinutes}m ago"
             : diff.TotalDays    < 1  ? $"{(int)diff.TotalHours}h ago"
             : diff.TotalDays    < 30 ? $"{(int)diff.TotalDays}d ago"
             : dt.ToString("MMM d, yyyy");
    }
}

// ── Hub VM ────────────────────────────────────────────────────────────────────

public partial class ProjectHubViewModel : ViewModelBase
{
    private readonly ProjectService _service = new();
    private ProjectRegistry         _registry;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProjects))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private ObservableCollection<ProjectItemViewModel> _projects = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasProjects => Projects.Count > 0;
    public bool IsEmpty     => Projects.Count == 0;
    public bool HasError    => !string.IsNullOrEmpty(ErrorMessage);

    public ProjectHubViewModel()
    {
        _registry = ProjectRegistry.Load();
        ReloadProjects();
    }

    // ── Public surface for view code-behind ──────────────────

    /// <summary>Called by the view after NewProjectDialog closes with a project.</summary>
    public void AddProject(NebulaProject project)
    {
        ErrorMessage = null;

        var existing = Projects.FirstOrDefault(p => p.Project.Path == project.Path);
        if (existing is not null)
            Projects.Remove(existing);

        Projects.Insert(0, new ProjectItemViewModel(project, HandleOpen, HandleRemove));
        NotifyListChanged();
    }

    /// <summary>Called by the view after the folder picker resolves a path.</summary>
    public void OpenFromPath(string path)
    {
        var project = _service.Open(path);

        if (project is null)
        {
            ErrorMessage = $"No NebulaGE project found in: {path}";
            return;
        }

        AddProject(project);
    }

    // ── Commands ──────────────────────────────────────────────

    [RelayCommand]
    private void DismissError() => ErrorMessage = null;

    // ── Internals ─────────────────────────────────────────────

    private void ReloadProjects()
    {
        Projects = new ObservableCollection<ProjectItemViewModel>(
            _registry.Projects.Select(p => new ProjectItemViewModel(p, HandleOpen, HandleRemove)));
    }

    private void HandleOpen(ProjectItemViewModel item)
    {
        item.Project.LastOpened = DateTime.UtcNow;
        _registry.AddOrUpdate(item.Project);
        // TODO: launch editor for item.Project
    }

    private void HandleRemove(ProjectItemViewModel item)
    {
        _registry.Remove(item.Project);
        Projects.Remove(item);
        NotifyListChanged();
    }

    private void NotifyListChanged()
    {
        OnPropertyChanged(nameof(HasProjects));
        OnPropertyChanged(nameof(IsEmpty));
    }

    partial void OnSearchTextChanged(string value)
    {
        // TODO: filter projects list against value
    }
}
