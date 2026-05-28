using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NebulaLauncher.Models;
using NebulaLauncher.ViewModels;

namespace NebulaLauncher.Views;

public partial class ProjectHubView : UserControl
{
    private ProjectHubViewModel? _boundVm;

    public ProjectHubView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => BindViewModel();
    }

    private void BindViewModel()
    {
        // Unsubscribe from the old VM (navigation recreates VMs each time)
        if (_boundVm is not null)
            _boundVm.OpenProjectRequested -= OnOpenProjectRequested;

        _boundVm = DataContext as ProjectHubViewModel;

        if (_boundVm is not null)
            _boundVm.OpenProjectRequested += OnOpenProjectRequested;
    }

    // ── Engine splash + launch ────────────────────────────────

    private async void OnOpenProjectRequested(NebulaProject project,
                                              EngineInstall engine,
                                              string engineExe)
    {
        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        var splash = new SplashScreen(project, engine);
        if (parentWindow is not null) splash.Show(parentWindow);
        else splash.Show();

        try
        {
            splash.SetStatus("Launching engine…");
            Process.Start(new ProcessStartInfo(engineExe, $"\"{project.Path}\"")
            {
                UseShellExecute = true,
            });

            // Hold the splash while the engine window starts up
            splash.SetStatus("Starting up…");
            await Task.Delay(2800);
        }
        catch
        {
            // Error banner already set by VM — just close the splash
        }
        finally
        {
            splash.Close();
        }
    }

    // ── New Project ───────────────────────────────────────────

    private async void OnNewProject(object? sender, RoutedEventArgs e)
    {
        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        if (parentWindow is null) return;

        var dialog = new NewProjectDialog();
        var project = await dialog.ShowDialog<NebulaProject?>(parentWindow);

        if (project is not null && DataContext is ProjectHubViewModel vm)
            vm.AddProject(project);
    }

    // ── Open Folder ───────────────────────────────────────────

    private async void OnOpenFromDisk(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title         = "Open Project Folder",
                AllowMultiple = false,
            });

        if (folders.Count > 0 && DataContext is ProjectHubViewModel vm)
            vm.OpenFromPath(folders[0].Path.LocalPath);
    }
}
