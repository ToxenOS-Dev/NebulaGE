using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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
            Title         = "Select Project Location",
            AllowMultiple = false,
        });

        if (folders.Count > 0)
            vm.Location = folders[0].Path.LocalPath;
    }

    // ── Confirm / cancel ─────────────────────────────────────

    private async void OnCreate(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not NewProjectDialogViewModel vm || !vm.CanCreate)
            return;

        if (vm.IsCloneMode)
            await CloneAndClose(vm);
        else
            CreateAndClose(vm);
    }

    // ── New project ───────────────────────────────────────────

    private void CreateAndClose(NewProjectDialogViewModel vm)
    {
        vm.ErrorMessage = null;
        try
        {
            var project = _service.Create(
                vm.ProjectName.Trim(),
                vm.Location,
                vm.SelectedTemplate.Id);

            Close(project);
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = ex.Message;
        }
    }

    // ── Clone from GitHub ─────────────────────────────────────
    // Hands the clone off to the global DownloadManagerViewModel tray and
    // closes the dialog immediately — no waiting, no blocking.

    private Task CloneAndClose(NewProjectDialogViewModel vm)
    {
        var cloneUrl = NewProjectDialogViewModel.NormalizeCloneUrl(vm.CloneUrl) ?? vm.CloneUrl.Trim();
        var name     = vm.ProjectName.Trim();
        var dest     = Path.Combine(vm.Location, name);

        DownloadManagerViewModel.Current.StartClone(name, cloneUrl, dest, vm.Location);

        Close(null);   // dialog closes; tray handles the rest
        return Task.CompletedTask;
    }

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close(null);
}
