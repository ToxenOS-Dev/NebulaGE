using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NebulaLauncher.Services;

namespace NebulaLauncher.ViewModels;

public enum NavItem
{
    Projects,
    Engines,
    Settings
}

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProjectsSelected))]
    [NotifyPropertyChangedFor(nameof(IsEnginesSelected))]
    [NotifyPropertyChangedFor(nameof(IsSettingsSelected))]
    private NavItem _selectedNav = NavItem.Projects;

    [ObservableProperty]
    private ViewModelBase _currentPage;

    public bool IsProjectsSelected => SelectedNav == NavItem.Projects;
    public bool IsEnginesSelected  => SelectedNav == NavItem.Engines;
    public bool IsSettingsSelected => SelectedNav == NavItem.Settings;

    // ── GitHub connection ─────────────────────────────────────
    /// <summary>Singleton shared with the Settings page so login/logout updates the sidebar live.</summary>
    public GitHubSessionViewModel GitHub => GitHubSessionViewModel.Current;

    public MainWindowViewModel()
    {
        _currentPage = new ProjectHubViewModel();
    }

    [RelayCommand]
    private void Navigate(NavItem item)
    {
        SelectedNav = item;
        CurrentPage = item switch
        {
            NavItem.Projects => new ProjectHubViewModel(),
            NavItem.Engines  => new EngineVersionsViewModel(),
            NavItem.Settings => new SettingsViewModel(),
            _                => CurrentPage
        };
    }
}
