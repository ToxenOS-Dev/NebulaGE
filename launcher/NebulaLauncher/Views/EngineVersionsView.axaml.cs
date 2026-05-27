using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NebulaLauncher.ViewModels;

namespace NebulaLauncher.Views;

public partial class EngineVersionsView : UserControl
{
    public EngineVersionsView()
    {
        InitializeComponent();
    }

    // ── Add Local Build ───────────────────────────────────────
    // Opens a folder picker; passes the selected path to the VM.

    private async void OnAddLocalBuild(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title         = "Select NebulaGE Engine Folder",
                AllowMultiple = false,
            });

        if (folders.Count > 0 && DataContext is EngineVersionsViewModel vm)
            vm.AddFromPath(folders[0].Path.LocalPath);
    }

    // ── Browse Releases ───────────────────────────────────────
    // Opens the NebulaGE GitHub releases page in the default browser.

    private void OnBrowseReleases(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(
                "xdg-open",
                "https://github.com/NebulaGE/NebulaGE/releases")
            {
                UseShellExecute = true
            });
        }
        catch { }
    }
}
