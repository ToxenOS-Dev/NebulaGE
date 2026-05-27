using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NebulaLauncher.Models;
using NebulaLauncher.Services;
using NebulaLauncher.ViewModels;

namespace NebulaLauncher.Views;

public partial class NewProjectDialog : Window
{
    private readonly ProjectService _service = new();

    public NewProjectDialog()
    {
        InitializeComponent();
        DataContext = new NewProjectDialogViewModel();

        // Focus name input on open
        Opened += (_, _) => NameInput.Focus();
    }

    // ── Location browse ───────────────────────────────────────

    private async void OnBrowseLocation(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not NewProjectDialogViewModel vm) return;

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title        = "Select Project Location",
            AllowMultiple = false,
        });

        if (folders.Count > 0)
            vm.Location = folders[0].Path.LocalPath;
    }

    // ── Confirm / cancel ─────────────────────────────────────

    private void OnCreate(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not NewProjectDialogViewModel vm || !vm.CanCreate)
            return;

        try
        {
            var project = _service.Create(
                vm.ProjectName.Trim(),
                vm.Location,
                vm.SelectedTemplate.Id);

            Close(project);
        }
        catch
        {
            // TODO: surface error inline rather than silently failing
            Close(null);
        }
    }

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close(null);
}
