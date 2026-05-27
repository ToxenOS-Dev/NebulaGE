using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NebulaLauncher.ViewModels;

public record TemplateOption(string Id, string Label, string Description);

public partial class NewProjectDialogViewModel : ViewModelBase
{
    // ── Available templates ───────────────────────────────────
    public static readonly List<TemplateOption> AvailableTemplates =
    [
        new("empty",    "Empty",    "Bare project — just the folder structure"),
        new("2d-game",  "2D Game",  "Coming soon"),
        new("3d-game",  "3D Game",  "Coming soon"),
    ];

    // ── Observable state ──────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    [NotifyPropertyChangedFor(nameof(FullPath))]
    private string _projectName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullPath))]
    private string _location = DefaultProjectsPath;

    [ObservableProperty]
    private TemplateOption _selectedTemplate = AvailableTemplates[0];

    // ── Derived ───────────────────────────────────────────────
    public bool   CanCreate => !string.IsNullOrWhiteSpace(ProjectName);
    public string FullPath  => string.IsNullOrWhiteSpace(ProjectName)
        ? Location
        : Path.Combine(Location, ProjectName.Trim());

    // ── Default path ──────────────────────────────────────────
    public static string DefaultProjectsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Projects");
}
