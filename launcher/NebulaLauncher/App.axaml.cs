using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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

        // Apply the saved theme before any windows open
        ThemeManager.Apply(ThemeService.LoadActive());

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
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
