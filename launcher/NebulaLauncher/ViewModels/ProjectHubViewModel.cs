using System;
using System.Collections.ObjectModel;
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

    public string  Name             => Project.Name;
    public string  Path             => Project.Path;
    public string  EngineVersion    => $"v{Project.EngineVersion}";
    public bool    HasGitRepo       => Project.HasGitRepo;
    public string? GitBranch        => Project.GitBranch;
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

    [RelayCommand]
    private void Open()   => _onOpen(this);

    [RelayCommand]
    private void Remove() => _onRemove(this);

    private static string FormatDate(DateTime dt)
    {
        var now = DateTime.UtcNow;
        var diff = now - dt;

        return diff.TotalMinutes < 1   ? "Just now"
             : diff.TotalHours   < 1   ? $"{(int)diff.TotalMinutes}m ago"
             : diff.TotalDays    < 1   ? $"{(int)diff.TotalHours}h ago"
             : diff.TotalDays    < 30  ? $"{(int)diff.TotalDays}d ago"
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

    public bool HasProjects => Projects.Count > 0;
    public bool IsEmpty     => Projects.Count == 0;

    public ProjectHubViewModel()
    {
        _registry = ProjectRegistry.Load();
        ReloadProjects();
    }

    // ── Public surface for view code-behind ──────────────────

    /// <summary>Called by the view after NewProjectDialog closes with a project.</summary>
    public void AddProject(NebulaProject project)
    {
        // Avoid duplicates (e.g. user created the same path twice)
        var existing = Projects.FirstOrDefault(p => p.Project.Path == project.Path);
        if (existing is not null)
            Projects.Remove(existing);

        var item = new ProjectItemViewModel(project, HandleOpen, HandleRemove);
        Projects.Insert(0, item);
        NotifyListChanged();
    }

    /// <summary>Called by the view after the folder picker resolves a path.</summary>
    public void OpenFromPath(string path)
    {
        var project = _service.Open(path);
        if (project is null) return; // not a valid nebula project — TODO: show inline error

        AddProject(project);
    }

    // ── Internals ─────────────────────────────────────────────

    private void ReloadProjects()
    {
        var items = _registry.Projects
            .Select(p => new ProjectItemViewModel(p, HandleOpen, HandleRemove));

        Projects = new ObservableCollection<ProjectItemViewModel>(items);
    }

    private void HandleOpen(ProjectItemViewModel item)
    {
        // Update last-opened timestamp in registry
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
