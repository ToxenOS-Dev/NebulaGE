using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NebulaLauncher.Models;
using NebulaLauncher.Services;

namespace NebulaLauncher.ViewModels;

// ── Single installed engine item VM ─────────────────────────────────────────

public partial class EngineItemViewModel : ViewModelBase
{
    public EngineInstall Install { get; }

    private readonly Action<EngineItemViewModel> _onSetDefault;
    private readonly Action<EngineItemViewModel> _onRemove;

    public string Version => Install.Version;
    public string Path    => Install.Path;

    /// Shortened version for the badge: "0.1.0-dev" → "0.1"
    public string VersionShort
    {
        get
        {
            var clean = Version.TrimStart('v');
            var parts = clean.Split('.');
            return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : clean;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotDefault))]
    private bool _isDefault;

    public bool IsNotDefault => !IsDefault;

    public EngineItemViewModel(EngineInstall install,
                               Action<EngineItemViewModel> onSetDefault,
                               Action<EngineItemViewModel> onRemove)
    {
        Install       = install;
        _isDefault    = install.IsDefault;
        _onSetDefault = onSetDefault;
        _onRemove     = onRemove;
    }

    [RelayCommand] private void SetDefault()   => _onSetDefault(this);
    [RelayCommand] private void Remove()       => _onRemove(this);

    [RelayCommand]
    private void ShowInFiles()
    {
        try
        {
            Process.Start(new ProcessStartInfo("xdg-open", $"\"{Install.Path}\"")
                { UseShellExecute = true });
        }
        catch { }
    }
}

// ── Engine Versions page VM ──────────────────────────────────────────────────

public partial class EngineVersionsViewModel : ViewModelBase
{
    private EngineRegistry _registry;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEngines))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(SectionHeader))]
    private ObservableCollection<EngineItemViewModel> _engines = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool   HasEngines   => Engines.Count > 0;
    public bool   IsEmpty      => Engines.Count == 0;
    public bool   HasError     => !string.IsNullOrEmpty(ErrorMessage);
    public string SectionHeader => Engines.Count switch
    {
        0 => "INSTALLED VERSIONS",
        1 => "INSTALLED VERSIONS — 1",
        _ => $"INSTALLED VERSIONS — {Engines.Count}",
    };

    public EngineVersionsViewModel()
    {
        _registry = EngineRegistry.Load();
        Reload();
    }

    // ── Public API (called from code-behind) ─────────────────

    /// <summary>
    /// Attempts to register an installation folder.
    /// Reads version from nebula.json → VERSION file → nebula binary.
    /// Sets ErrorMessage if detection fails.
    /// </summary>
    public void AddFromPath(string folderPath)
    {
        ErrorMessage = null;

        var version = DetectVersion(folderPath);
        if (version is null)
        {
            ErrorMessage = $"No NebulaGE installation found at:\n{folderPath}";
            return;
        }

        var install = new EngineInstall
        {
            Version   = version,
            Path      = folderPath,
            IsDefault = Engines.Count == 0,   // first install becomes default automatically
            AddedAt   = DateTime.UtcNow,
        };

        _registry.AddOrUpdate(install);

        // Replace existing VM if path already in list, else insert at top
        var existing = Engines.FirstOrDefault(e => e.Install.Path == folderPath);
        if (existing is not null) Engines.Remove(existing);
        Engines.Insert(0, new EngineItemViewModel(install, HandleSetDefault, HandleRemove));
        NotifyChanged();
    }

    // ── Commands ─────────────────────────────────────────────

    [RelayCommand] private void DismissError() => ErrorMessage = null;

    // ── Internals ────────────────────────────────────────────

    private void Reload()
    {
        var items = _registry.Engines
            .Select(e => new EngineItemViewModel(e, HandleSetDefault, HandleRemove));
        Engines = new ObservableCollection<EngineItemViewModel>(items);
    }

    private void HandleSetDefault(EngineItemViewModel item)
    {
        _registry.SetDefault(item.Install);
        foreach (var e in Engines)
        {
            e.IsDefault      = e == item;
            e.Install.IsDefault = e == item;
        }
    }

    private void HandleRemove(EngineItemViewModel item)
    {
        _registry.Remove(item.Install);
        Engines.Remove(item);
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        OnPropertyChanged(nameof(HasEngines));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(SectionHeader));
    }

    // ── Version detection ────────────────────────────────────

    private static string? DetectVersion(string folderPath)
    {
        // 1. nebula.json manifest
        var manifest = Path.Combine(folderPath, "nebula.json");
        if (File.Exists(manifest))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(manifest));
                if (doc.RootElement.TryGetProperty("version", out var v))
                    return v.GetString();
            }
            catch { }
        }

        // 2. Plain VERSION file
        var versionFile = Path.Combine(folderPath, "VERSION");
        if (File.Exists(versionFile))
            return File.ReadAllText(versionFile).Trim();

        // 3. Run the binary with --version
        var exe = Path.Combine(folderPath, "nebula");
        if (File.Exists(exe))
        {
            try
            {
                using var proc = Process.Start(new ProcessStartInfo(exe, "--version")
                {
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow         = true,
                });
                var output = proc?.StandardOutput.ReadLine()?.Trim();
                proc?.WaitForExit(3000);
                if (!string.IsNullOrEmpty(output))
                    return output.TrimStart('v');
            }
            catch { }
        }

        return null;
    }
}
