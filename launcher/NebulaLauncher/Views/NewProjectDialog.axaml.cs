using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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
            // Surface the error in the VM error banner (if wired) or swallow
            _ = ex;
            Close(null);
        }
    }

    // ── Clone from GitHub ─────────────────────────────────────

    private async Task CloneAndClose(NewProjectDialogViewModel vm)
    {
        var cloneUrl = NewProjectDialogViewModel.NormalizeCloneUrl(vm.CloneUrl) ?? vm.CloneUrl.Trim();
        var dest     = Path.Combine(vm.Location, vm.ProjectName.Trim());

        // Disable UI while cloning
        IsEnabled = false;

        try
        {
            Directory.CreateDirectory(vm.Location);

            using var proc = Process.Start(new ProcessStartInfo(
                "git", $"clone \"{cloneUrl}\" \"{dest}\"")
            {
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            });

            if (proc is not null)
                await proc.WaitForExitAsync();

            if (proc?.ExitCode == 0 && Directory.Exists(dest))
            {
                // Register the cloned project
                var project = new NebulaProject
                {
                    Name      = vm.ProjectName.Trim(),
                    Path      = dest,
                    GitHubUrl = NewProjectDialogViewModel.NormalizeCloneUrl(vm.CloneUrl),
                    Created   = DateTime.UtcNow,
                    LastOpened = DateTime.UtcNow,
                };
                await Dispatcher.UIThread.InvokeAsync(() => Close(project));
                return;
            }
        }
        catch { }

        // Clone failed — re-enable and let user fix the URL
        await Dispatcher.UIThread.InvokeAsync(() => IsEnabled = true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close(null);
}
