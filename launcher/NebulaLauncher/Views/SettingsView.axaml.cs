using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NebulaLauncher.ViewModels;

namespace NebulaLauncher.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void OnBrowseLocation(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title         = "Default Project Location",
                AllowMultiple = false,
            });

        if (folders.Count > 0 && DataContext is SettingsViewModel vm)
            vm.SetDefaultLocation(folders[0].Path.LocalPath);
    }
}
