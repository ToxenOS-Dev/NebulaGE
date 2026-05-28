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

    private async Task CloneAndClose(NewProjectDialogViewModel vm)
    {
        var cloneUrl = NewProjectDialogViewModel.NormalizeCloneUrl(vm.CloneUrl) ?? vm.CloneUrl.Trim();
        var dest     = Path.Combine(vm.Location, vm.ProjectName.Trim());

        vm.ErrorMessage = null;
        vm.IsBusy       = true;

        string? stderr = null;
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
            {
                stderr = await proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync();
            }

            if (proc?.ExitCode == 0 && Directory.Exists(dest))
            {
                var project = new NebulaProject
                {
                    Name       = vm.ProjectName.Trim(),
                    Path       = dest,
                    GitHubUrl  = NewProjectDialogViewModel.NormalizeCloneUrl(vm.CloneUrl),
                    Created    = DateTime.UtcNow,
                    LastOpened = DateTime.UtcNow,
                };
                await Dispatcher.UIThread.InvokeAsync(() => Close(project));
                return;
            }

            // Clone failed — surface the git stderr
            var hint = string.IsNullOrWhiteSpace(stderr)
                ? "Check the URL and your internet connection."
                : stderr.Trim();
            vm.ErrorMessage = $"Clone failed: {hint}";
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = $"Clone error: {ex.Message}";
        }
        finally
        {
            vm.IsBusy = false;
        }
    }

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close(null);
}
