using System;
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

    private readonly Action<ProjectItemViewModel>  _onOpen;
    private readonly Action<ProjectItemViewModel>  _onRemove;

    public string  Name              => Project.Name;
    public string  Path              => Project.Path;
    public string  EngineVersion     => $"v{Project.EngineVersion}";
    public string  LastOpenedDisplay => FormatDate(Project.LastOpened);

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

        _hasGitRepo = project.HasGitRepo;
        _gitBranch  = project.GitBranch;
    }

    [RelayCommand] private void Open()   => _onOpen(this);
    [RelayCommand] private void Remove() => _onRemove(this);

    [RelayCommand]
    private void OpenInIde()
    {
        var idePath = SettingsService.Load().PreferredIdePath;
        if (string.IsNullOrEmpty(idePath)) return;
        try { Process.Start(new ProcessStartInfo(idePath, $"\"{Project.Path}\"") { UseShellExecute = true }); }
        catch { /* IDE path invalid — silently ignore */ }
    }

    [RelayCommand]
    private void ShowInFiles()
    {
        try { Process.Start(new ProcessStartInfo("xdg-open", $"\"{Project.Path}\"") { UseShellExecute = true }); }
        catch { }
    }

    public void RefreshGitStatus()
    {
        var (hasRepo, branch) = GitService.GetStatus(Project.Path);
        HasGitRepo         = hasRepo;
        GitBranch          = branch;
        Project.HasGitRepo = hasRepo;
        Project.GitBranch  = branch;
    }

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
    [NotifyPropertyChangedFor(nameof(FilteredProjects))]
    private ObservableCollection<ProjectItemViewModel> _projects = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredProjects))]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredProjects))]
    private bool _isGridView = false;

    public bool HasProjects => Projects.Count > 0;
    public bool IsEmpty     => FilteredProjects.Count == 0;
    public bool HasError    => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<ProjectItemViewModel> FilteredProjects
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return Projects;

            var q = SearchText.Trim();
            return new ObservableCollection<ProjectItemViewModel>(
                Projects.Where(p =>
                    p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    p.Path.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public ProjectHubViewModel()
    {
        _registry = ProjectRegistry.Load();
        ReloadProjects();
    }

    // ── Public API for code-behind ────────────────────────────

    public void AddProject(NebulaProject project)
    {
        ErrorMessage = null;

        var (hasRepo, branch) = GitService.GetStatus(project.Path);
        project.HasGitRepo    = hasRepo;
        project.GitBranch     = branch;

        var existing = Projects.FirstOrDefault(p => p.Project.Path == project.Path);
        if (existing is not null) Projects.Remove(existing);

        Projects.Insert(0, new ProjectItemViewModel(project, HandleOpen, HandleRemove));
        _registry.AddOrUpdate(project);
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

    [RelayCommand] private void DismissError()  => ErrorMessage = null;
    [RelayCommand] private void SetGridView()   => IsGridView = true;
    [RelayCommand] private void SetListView()   => IsGridView = false;

    // ── Internals ─────────────────────────────────────────────

    private void ReloadProjects()
    {
        var items = _registry.Projects
            .Select(p => new ProjectItemViewModel(p, HandleOpen, HandleRemove));

        Projects = new ObservableCollection<ProjectItemViewModel>(items);
        _ = RefreshGitStatusAsync(Projects.ToList());
    }

    private static async Task RefreshGitStatusAsync(
        System.Collections.Generic.List<ProjectItemViewModel> items)
    {
        var statuses = await Task.Run(() =>
            items.Select(i => (i, GitService.GetStatus(i.Project.Path))).ToList());

        foreach (var (item, _) in statuses)
            item.RefreshGitStatus();

        if (statuses.Count > 0)
        {
            var reg = ProjectRegistry.Load();
            foreach (var (item, _) in statuses)
                reg.AddOrUpdate(item.Project);
        }
    }

    private void HandleOpen(ProjectItemViewModel item)
    {
        item.Project.LastOpened = DateTime.UtcNow;
        _registry.AddOrUpdate(item.Project);
        // TODO: open editor window
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
        OnPropertyChanged(nameof(FilteredProjects));
    }
}
