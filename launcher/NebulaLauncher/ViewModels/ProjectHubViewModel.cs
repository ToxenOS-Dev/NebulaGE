using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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

    // Displayed in the row
    public string Name              => Project.Name;
    public string Path              => Project.Path;
    public string EngineVersion     => $"v{Project.EngineVersion}";
    public string LastOpenedDisplay => FormatDate(Project.LastOpened);

    // Observable so async git refresh updates the badge without reload
    [ObservableProperty] private bool    _hasGitRepo;
    [ObservableProperty] private string? _gitBranch;

    public ProjectItemViewModel(
        NebulaProject project,
        Action<ProjectItemViewModel> onOpen,
        Action<ProjectItemViewModel> onRemove)
    {
        Project   = project;
        _onOpen   = onOpen;
        _onRemove = onRemove;

        // Seed from cached registry value — async refresh overwrites later
        _hasGitRepo = project.HasGitRepo;
        _gitBranch  = project.GitBranch;
    }

    // ── Commands ──────────────────────────────────────────────

    [RelayCommand] private void Open()   => _onOpen(this);
    [RelayCommand] private void Remove() => _onRemove(this);

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
        catch { }
    }

    [RelayCommand]
    private void ShowInFiles()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName        = "xdg-open",
                Arguments       = $"\"{Project.Path}\"",
                UseShellExecute = false,
            });
        }
        catch { }
    }

    // ── Git refresh (called from hub on background thread) ────

    /// <summary>Reads git status from disk and updates observable properties.</summary>
    public void RefreshGitStatus()
    {
        var (hasRepo, branch) = GitService.GetStatus(Project.Path);
        HasGitRepo            = hasRepo;
        GitBranch             = branch;
        Project.HasGitRepo    = hasRepo;
        Project.GitBranch     = branch;
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

    public void AddProject(NebulaProject project)
    {
        ErrorMessage = null;

        // Detect git status immediately (fast — just reads a file)
        var (hasRepo, branch) = GitService.GetStatus(project.Path);
        project.HasGitRepo    = hasRepo;
        project.GitBranch     = branch;

        var existing = Projects.FirstOrDefault(p => p.Project.Path == project.Path);
        if (existing is not null) Projects.Remove(existing);

        Projects.Insert(0, new ProjectItemViewModel(project, HandleOpen, HandleRemove));
        NotifyListChanged();
    }

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

    [RelayCommand] private void DismissError() => ErrorMessage = null;

    // ── Internals ─────────────────────────────────────────────

    private void ReloadProjects()
    {
        Projects = new ObservableCollection<ProjectItemViewModel>(
            _registry.Projects.Select(p => new ProjectItemViewModel(p, HandleOpen, HandleRemove)));

        // Refresh git status in background — badges update when done
        _ = RefreshGitStatusAsync(Projects.ToList());
    }

    private static async Task RefreshGitStatusAsync(List<ProjectItemViewModel> items)
    {
        // Read all .git/HEAD files off the UI thread
        var statuses = await Task.Run(() =>
            items.Select(item => (item, GitService.GetStatus(item.Project.Path))).ToList());

        // Apply results back on the UI thread
        foreach (var (item, (hasRepo, branch)) in statuses)
            item.RefreshGitStatus();

        // Persist refreshed state to registry
        if (statuses.Count > 0)
        {
            var registry = ProjectRegistry.Load();
            foreach (var (item, _) in statuses)
                registry.AddOrUpdate(item.Project);
        }
    }

    private void HandleOpen(ProjectItemViewModel item)
    {
        item.Project.LastOpened = DateTime.UtcNow;
        _registry.AddOrUpdate(item.Project);
        // TODO: launch editor
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
        // TODO: filter list
    }
}
