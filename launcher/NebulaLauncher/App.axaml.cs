using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using NebulaLauncher.Services;
using NebulaLauncher.ViewModels;
using NebulaLauncher.Views;

namespace NebulaLauncher;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        RunFirstTimeSetup();

        // Apply the saved theme, fonts, and appearance flags before any windows open
        ThemeManager.Apply(ThemeService.LoadActive());
        ApplySavedFonts();
        AppearanceViewModel.Current.Apply();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    // ── Font restore ─────────────────────────────────────────

    private void ApplySavedFonts()
    {
        var settings = SettingsService.Load();
        var res      = Resources;

        if (!string.IsNullOrWhiteSpace(settings.UiFontFamily))
            res["NebulaUIFontFamily"] = FontFamily.Parse(settings.UiFontFamily);

        if (!string.IsNullOrWhiteSpace(settings.MonoFontFamily))
            res["NebulaMonoFontFamily"] = FontFamily.Parse(settings.MonoFontFamily);
    }

    // ── First-run setup ───────────────────────────────────────

    private static void RunFirstTimeSetup()
    {
        var settings = SettingsService.Load();
        if (!settings.FirstRun) return;

        // Detect installed IDEs and pick the best one
        var ideService = new IdeDetectionService();
        var preferred  = ideService.GetPreferred();

        if (preferred is not null)
        {
            settings.PreferredIdePath = preferred.ExecutablePath;
            settings.PreferredIdeName = preferred.Name;
        }

        settings.FirstRun = false;
        SettingsService.Save(settings);
    }
}
