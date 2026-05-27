using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NebulaLauncher.Services;

namespace NebulaLauncher.ViewModels;

/// <summary>
/// Singleton that owns the live GitHub authentication state.
/// Both the sidebar indicator (MainWindowViewModel) and the Settings page
/// bind to this so they stay in sync without any extra plumbing.
/// </summary>
public partial class GitHubSessionViewModel : ViewModelBase
{
    public static GitHubSessionViewModel Current { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUser))]
    [NotifyPropertyChangedFor(nameof(NoUser))]
    private string? _user;

    /// <summary>True while a gh CLI command is running.</summary>
    [ObservableProperty] private bool _isBusy;

    /// <summary>True while the browser OAuth flow is in progress.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LoginButtonLabel))]
    private bool _awaitingBrowserAuth;

    public bool   HasUser         => User is not null;
    public bool   NoUser          => User is null;
    public string LoginButtonLabel => AwaitingBrowserAuth ? "Connecting…" : "Connect GitHub";

    private GitHubSessionViewModel()
    {
        _user = GitHubService.GetAuthenticatedUser();
    }

    // ── Commands ──────────────────────────────────────────────

    /// <summary>
    /// Runs <c>gh auth login --web</c>, which auto-opens the browser OAuth flow.
    /// Awaits the process — gh exits once authentication completes — then
    /// refreshes the user automatically. No manual "Refresh" click needed.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task Login()
    {
        IsBusy = true;
        AwaitingBrowserAuth = true;
        try
        {
            using var proc = Process.Start(new ProcessStartInfo(
                "gh", "auth login --web --git-protocol https")
            {
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            });

            if (proc is not null)
                await proc.WaitForExitAsync();
        }
        catch
        {
            // gh not installed or launch failed — fall through to Refresh
        }

        AwaitingBrowserAuth = false;
        User   = GitHubService.GetAuthenticatedUser();
        IsBusy = false;
    }

    private bool CanLogin() => !IsBusy && NoUser;

    /// <summary>Signs out from GitHub and clears the stored token.</summary>
    [RelayCommand(CanExecute = nameof(CanLogout))]
    private async Task Logout()
    {
        IsBusy = true;
        AwaitingBrowserAuth = false;
        try
        {
            // pipe 'y' to confirm the prompt
            using var proc = Process.Start(new ProcessStartInfo(
                "bash", "-c \"printf 'y\\n' | gh auth logout --hostname github.com\"")
            {
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            });
            if (proc is not null)
                await proc.WaitForExitAsync();
        }
        catch { }

        User   = GitHubService.GetAuthenticatedUser();
        IsBusy = false;
    }

    private bool CanLogout() => !IsBusy && HasUser;

    /// <summary>Re-reads the gh config file and updates User.</summary>
    [RelayCommand]
    private void Refresh()
    {
        AwaitingBrowserAuth = false;
        User = GitHubService.GetAuthenticatedUser();
    }
}
