using Avalonia.Controls;
using NebulaLauncher.Models;

namespace NebulaLauncher.Views;

public partial class SplashScreen : Window
{
    public SplashScreen()
    {
        InitializeComponent();
    }

    public SplashScreen(NebulaProject project, EngineInstall engine)
    {
        InitializeComponent();
        ProjectNameText.Text = project.Name;
        VersionText.Text     = $"NebulaGE  v{engine.Version}";
    }

    public void SetStatus(string status)
    {
        if (StatusText is not null)
            StatusText.Text = status;
    }
}
