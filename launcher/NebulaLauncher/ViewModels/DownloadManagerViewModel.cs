using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NebulaLauncher.Models;

namespace NebulaLauncher.ViewModels;

// ── Single clone task ─────────────────────────────────────────────────────────

public partial class DownloadItemViewModel : ViewModelBase
{
    // Matches: "Receiving objects:  54% (54/100), 2.34 MiB | 1.23 MiB/s"
    private static readonly Regex _receiveRx = new(
        @"Receiving objects:\s+(\d+)%.*?(\d[\d.]*\s*(?:KiB|MiB|GiB)/s)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Matches: "Resolving deltas:  80%"
    private static readonly Regex _resolveRx = new(
        @"Resolving deltas:\s+(\d+)%",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly CancellationTokenSource _cts = new();
    private readonly Action<DownloadItemViewModel> _onRemove;

    // ── Public info ──────────────────────────────────────────
    public string Name { get; }   // project name, e.g. "my-game"
    public string Slug { get; }   // display URL, e.g. "github.com/user/my-game"

    // ── Observable state ─────────────────────────────────────
    [ObservableProperty] private double  _progress;      // 0.0 – 1.0
    [ObservableProperty] private string  _statusText = "Starting…";
    [ObservableProperty] private bool    _isActive   = true;
    [ObservableProperty] private bool    _isComplete;
    [ObservableProperty] private bool    _isFailed;

    public bool IsFinished => IsComplete || IsFailed;

    public DownloadItemViewModel(string name, string slug,
                                 Action<DownloadItemViewModel> onRemove)
    {
        Name     = name;
        Slug     = slug;
        _onRemove = onRemove;
    }

    // ── Commands ─────────────────────────────────────────────

    [RelayCommand]
    private void Cancel()
    {
        _cts.Cancel();
        StatusText = "Cancelled";
        IsActive   = false;
        IsFailed   = true;
        AutoDismiss(delay: 3000);
    }

    [RelayCommand]
    private void Dismiss() => _onRemove(this);

    // ── Clone runner ─────────────────────────────────────────

    /// <summary>
    /// Runs git clone, updates progress from stderr, fires <paramref name="onComplete"/>
    /// with the new <see cref="NebulaProject"/> on success or null on failure/cancel.
    /// </summary>
    public async Task RunCloneAsync(
        string cloneUrl, string dest, string location,
        Action<NebulaProject?> onComplete)
    {
        try
        {
            Directory.CreateDirectory(location);

            using var proc = Process.Start(new ProcessStartInfo(
                "git", $"clone --progress \"{cloneUrl}\" \"{dest}\"")
            {
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            }) ?? throw new InvalidOperationException("Could not start git.");

            // Parse progress from stderr on a background task
            var parseTask = ParseProgressAsync(proc.StandardError.BaseStream, _cts.Token);

            // Wait for git to exit (or cancel)
            await proc.WaitForExitAsync(_cts.Token).ConfigureAwait(false);
            await parseTask.ConfigureAwait(false);

            if (_cts.Token.IsCancellationRequested || proc.ExitCode != 0)
            {
                if (!_cts.Token.IsCancellationRequested)
                {
                    StatusText = "Clone failed";
                    IsFailed   = true;
                    IsActive   = false;
                    AutoDismiss(delay: 6000);
                }
                await Dispatcher.UIThread.InvokeAsync(() => onComplete(null));
                return;
            }

            // Success
            var project = new NebulaProject
            {
                Name       = Name,
                Path       = dest,
                GitHubUrl  = cloneUrl,
                Created    = DateTime.UtcNow,
                LastOpened = DateTime.UtcNow,
            };

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Progress   = 1.0;
                StatusText = "Done";
                IsActive   = false;
                IsComplete = true;
                onComplete(project);
            });

            AutoDismiss(delay: 4000);
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(() => onComplete(null));
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText = $"Error: {ex.Message}";
                IsActive   = false;
                IsFailed   = true;
                onComplete(null);
            });
            AutoDismiss(delay: 8000);
        }
    }

    // ── Git progress parser ──────────────────────────────────

    private async Task ParseProgressAsync(Stream stderr, CancellationToken ct)
    {
        var sb  = new StringBuilder();
        var buf = new byte[512];

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var count = await stderr.ReadAsync(buf, ct).ConfigureAwait(false);
                if (count == 0) break;

                for (var i = 0; i < count; i++)
                {
                    var ch = (char)buf[i];
                    if (ch == '\r' || ch == '\n')
                    {
                        var line = sb.ToString();
                        if (line.Length > 0)
                            UpdateFromLine(line);
                        sb.Clear();
                    }
                    else sb.Append(ch);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch { /* stream closed */ }
    }

    private void UpdateFromLine(string line)
    {
        var receiveMatch = _receiveRx.Match(line);
        if (receiveMatch.Success)
        {
            var pct   = int.Parse(receiveMatch.Groups[1].Value);
            var speed = receiveMatch.Groups[2].Value;
            // Map receiving (0-100%) → 0.0-0.90 of total progress
            var prog  = pct / 100.0 * 0.90;
            Dispatcher.UIThread.Post(() =>
            {
                Progress   = prog;
                StatusText = $"{pct}%  •  {speed}";
            });
            return;
        }

        var resolveMatch = _resolveRx.Match(line);
        if (resolveMatch.Success)
        {
            var pct  = int.Parse(resolveMatch.Groups[1].Value);
            var prog = 0.90 + pct / 100.0 * 0.10;
            Dispatcher.UIThread.Post(() =>
            {
                Progress   = prog;
                StatusText = $"Resolving…  {pct}%";
            });
            return;
        }

        // Show other git status lines (Counting, Compressing…)
        var clean = line.TrimStart("remote: ".ToCharArray()).Trim();
        if (clean.Length > 0 && !clean.StartsWith("Cloning"))
            Dispatcher.UIThread.Post(() => StatusText = clean);
    }

    private void AutoDismiss(int delay) =>
        Task.Delay(delay).ContinueWith(_ =>
            Dispatcher.UIThread.Post(() => _onRemove(this)));
}

// ── Tray singleton ────────────────────────────────────────────────────────────

public partial class DownloadManagerViewModel : ViewModelBase
{
    public static DownloadManagerViewModel Current { get; } = new();

    public ObservableCollection<DownloadItemViewModel> Downloads { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDownloads))]
    private int _downloadCount;

    public bool HasDownloads => DownloadCount > 0;

    /// <summary>
    /// Fired on the UI thread when a clone succeeds.
    /// ProjectHubViewModel subscribes so it can add the new project card.
    /// </summary>
    public event Action<NebulaProject>? CloneCompleted;

    private DownloadManagerViewModel() { }

    // ── Public API ────────────────────────────────────────────

    /// <summary>Kicks off a git clone in the background and shows it in the tray.</summary>
    public void StartClone(string projectName, string cloneUrl, string dest, string location)
    {
        var slug = ExtractSlug(cloneUrl);
        var item = new DownloadItemViewModel(projectName, slug, Remove);

        Downloads.Add(item);
        DownloadCount = Downloads.Count;

        _ = item.RunCloneAsync(cloneUrl, dest, location, project =>
        {
            if (project is not null)
                CloneCompleted?.Invoke(project);
        });
    }

    // ── Internals ─────────────────────────────────────────────

    private void Remove(DownloadItemViewModel item)
    {
        Downloads.Remove(item);
        DownloadCount = Downloads.Count;
    }

    private static string ExtractSlug(string url)
    {
        // "https://github.com/user/repo" → "github.com/user/repo"
        try
        {
            var uri = new Uri(url);
            return uri.Host + uri.AbsolutePath.TrimEnd('/');
        }
        catch
        {
            return url;
        }
    }
}
