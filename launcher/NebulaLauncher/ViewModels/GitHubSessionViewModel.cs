using System;
using System.Diagnostics;
using System.Threading;
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
    [NotifyPropertyChangedFor(nameof(IsNotConnectedIdle))]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    [NotifyCanExecuteChangedFor(nameof(LogoutCommand))]
    private string? _user;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    [NotifyCanExecuteChangedFor(nameof(LogoutCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LoginButtonLabel))]
    [NotifyPropertyChangedFor(nameof(IsNotConnectedIdle))]
    [NotifyPropertyChangedFor(nameof(AwaitingWithoutCode))]
    [NotifyCanExecuteChangedFor(nameof(CancelLoginCommand))]
    private bool _awaitingBrowserAuth;

    /// <summary>The 8-char device code, e.g. "ABCD-1234". Empty string when not in auth flow.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDeviceCode))]
    [NotifyPropertyChangedFor(nameof(AwaitingWithoutCode))]
    private string _deviceCode = string.Empty;

    /// <summary>Non-null if the last auth attempt failed to produce a token.</summary>
    [ObservableProperty] private string? _authError;

    public bool   HasUser             => User is not null;
    public bool   NoUser              => User is null;
    public bool   ShowDeviceCode      => AwaitingBrowserAuth && DeviceCode.Length > 0;
    public bool   IsNotConnectedIdle  => NoUser && !AwaitingBrowserAuth;
    public bool   AwaitingWithoutCode => AwaitingBrowserAuth && DeviceCode.Length == 0;
    public string LoginButtonLabel    => AwaitingBrowserAuth ? "Connecting…" : "Connect GitHub";

    private GitHubSessionViewModel()
    {
        _user = GitHubService.GetAuthenticatedUser();
    }

    // ── Commands ──────────────────────────────────────────────

    private CancellationTokenSource? _loginCts;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task Login()
    {
        IsBusy              = true;
        AwaitingBrowserAuth = true;
        DeviceCode          = string.Empty;
        AuthError           = null;

        _loginCts = new CancellationTokenSource();
        try
        {
            await RunLoginFlowAsync(_loginCts.Token);
        }
        catch (Exception ex)
        {
            AuthError = ex.Message;
        }
        finally
        {
            _loginCts?.Dispose();
            _loginCts           = null;
            DeviceCode          = string.Empty;
            AwaitingBrowserAuth = false;
            User              ??= GitHubService.GetAuthenticatedUser();
            IsBusy              = false;
        }
    }

    private bool CanLogin() => !IsBusy && NoUser;

    [RelayCommand(CanExecute = nameof(CanCancelLogin))]
    private void CancelLogin()
    {
        _loginCts?.Cancel();
    }

    private bool CanCancelLogin() => AwaitingBrowserAuth;

    private async Task RunLoginFlowAsync(CancellationToken ct)
    {
        // ── 1. Ask GitHub for a device + user code ────────────
        var info = await GitHubDeviceFlowService.RequestDeviceCodeAsync();
        if (info is null)
        {
            AuthError = "Could not reach GitHub. Check your internet connection.";
            return;
        }

        // ── 2. Show code in UI + open the browser ─────────────
        DeviceCode = info.UserCode;
        OpenBrowser(info.VerificationUri);

        // ── 3. Poll until authorized, cancelled, or code expires ─
        using var expiryCts = new CancellationTokenSource(TimeSpan.FromSeconds(info.ExpiresIn));
        using var linked    = CancellationTokenSource.CreateLinkedTokenSource(ct, expiryCts.Token);

        var token = await GitHubDeviceFlowService.PollForTokenAsync(
            info.DeviceCode, info.Interval, linked.Token);

        if (token is null)
        {
            // Cancelled by user — no error message needed; timed out → show message
            if (!ct.IsCancellationRequested)
                AuthError = "Authorization timed out or was denied. Try again.";
            return;
        }

        // ── 4. Resolve username + save so `gh` CLI can use it ─
        var login = await GitHubDeviceFlowService.GetUserLoginAsync(token);
        if (login is null)
        {
            AuthError = "Authorized but could not read GitHub username.";
            return;
        }

        GitHubDeviceFlowService.SaveToGhConfig(token, login);
        User = login;
    }

    [RelayCommand(CanExecute = nameof(CanLogout))]
    private void Logout()
    {
        IsBusy    = true;
        AuthError = null;
        try
        {
            GitHubDeviceFlowService.ClearGhConfig();
        }
        catch { }
        finally
        {
            User   = GitHubService.GetAuthenticatedUser(); // should be null now
            IsBusy = false;
        }
    }

    private bool CanLogout() => !IsBusy && HasUser;

    [RelayCommand]
    private void Refresh()
    {
        AwaitingBrowserAuth = false;
        DeviceCode          = string.Empty;
        AuthError           = null;
        User                = GitHubService.GetAuthenticatedUser();
    }

    // ── Commands ─ (auth helpers) ─────────────────────────────

    /// <summary>Re-opens the GitHub device auth page in the browser.</summary>
    [RelayCommand]
    private void ReopenBrowser() => OpenBrowser("https://github.com/login/device");

    /// <summary>Copies the current device code to the clipboard.</summary>
    [RelayCommand]
    private async Task CopyCode()
    {
        if (string.IsNullOrEmpty(DeviceCode)) return;
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime
                    is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var clipboard = Avalonia.Controls.TopLevel
                    .GetTopLevel(desktop.MainWindow)?.Clipboard;
                if (clipboard is not null)
                    await clipboard.SetTextAsync(DeviceCode);
            }
        }
        catch { }
    }

    // ── Helpers ───────────────────────────────────────────────

    private static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo("xdg-open", url)
            {
                UseShellExecute = true,
            });
        }
        catch { }
    }
}
