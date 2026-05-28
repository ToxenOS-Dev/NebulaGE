using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NebulaLauncher.Services;

/// <summary>
/// Watches project parent directories with FileSystemWatcher (inotify on Linux).
/// Fires events when a project folder disappears or a new one appears.
/// All events are raised on thread-pool threads — callers must marshal to the UI thread.
/// </summary>
public sealed class ProjectWatcherService : IDisposable
{
    private const string ProjectMarker = "project.nebula";

    private readonly List<FileSystemWatcher> _watchers = [];

    // ── Events ────────────────────────────────────────────────

    /// <summary>A watched project folder was deleted or moved away.</summary>
    public event Action<string>? ProjectFolderDeleted;

    /// <summary>
    /// A new sub-folder appeared inside a watched directory that contains
    /// a valid <c>project.nebula</c> file (checked after a short settle delay).
    /// </summary>
    public event Action<string>? ProjectFolderAppeared;

    // ── Public API ────────────────────────────────────────────

    /// <summary>
    /// (Re-)builds the watcher set.
    /// Call this after any project is added, removed, or on first load.
    /// </summary>
    public void Refresh(IEnumerable<string> projectPaths, string defaultProjectsDir)
    {
        DisposeWatchers();

        // Collect unique parent directories
        var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (Directory.Exists(defaultProjectsDir))
            dirs.Add(defaultProjectsDir);

        foreach (var path in projectPaths)
        {
            var parent = Path.GetDirectoryName(path);
            if (parent is not null && Directory.Exists(parent))
                dirs.Add(parent);
        }

        foreach (var dir in dirs)
            TryAddWatcher(dir);
    }

    // ── Watcher setup ─────────────────────────────────────────

    private void TryAddWatcher(string dir)
    {
        try
        {
            var w = new FileSystemWatcher(dir)
            {
                NotifyFilter      = NotifyFilters.DirectoryName,
                IncludeSubdirectories = false,
                EnableRaisingEvents   = true,
            };

            w.Deleted += (_, e) => ProjectFolderDeleted?.Invoke(e.FullPath);

            w.Created += (_, e) => CheckAndRaiseAppeared(e.FullPath);

            // Rename = old path gone, new path appeared
            w.Renamed += (_, e) =>
            {
                ProjectFolderDeleted?.Invoke(e.OldFullPath);
                CheckAndRaiseAppeared(e.FullPath);
            };

            // Watcher can die if the watched directory is deleted; just let it go
            w.Error += (_, _) => { };

            _watchers.Add(w);
        }
        catch { /* Directory disappeared between check and watch creation */ }
    }

    /// <summary>
    /// Waits 800 ms for files to settle (e.g. a folder copy in progress),
    /// then checks for the project marker before raising the event.
    /// </summary>
    private void CheckAndRaiseAppeared(string path)
    {
        Task.Delay(800).ContinueWith(_ =>
        {
            try
            {
                if (Directory.Exists(path) &&
                    File.Exists(Path.Combine(path, ProjectMarker)))
                {
                    ProjectFolderAppeared?.Invoke(path);
                }
            }
            catch { }
        }, TaskScheduler.Default);
    }

    // ── Cleanup ───────────────────────────────────────────────

    public void Dispose() => DisposeWatchers();

    private void DisposeWatchers()
    {
        foreach (var w in _watchers)
        {
            try
            {
                w.EnableRaisingEvents = false;
                w.Dispose();
            }
            catch { }
        }
        _watchers.Clear();
    }
}
