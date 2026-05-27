using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NebulaLauncher.Models;
using NebulaLauncher.ViewModels;

namespace NebulaLauncher.Views;

public partial class ProjectHubView : UserControl
{
    public ProjectHubView()
    {
        InitializeComponent();
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
