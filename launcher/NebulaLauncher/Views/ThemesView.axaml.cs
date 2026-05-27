using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using NebulaLauncher.ViewModels;

namespace NebulaLauncher.Views;

public partial class ThemesView : UserControl
{
    public ThemesView()
    {
        InitializeComponent();
    }

    private async void OnSwatchClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not ColorEntryViewModel entry) return;

        var dialog = new ColorPickerDialog(entry.Color);
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is not Window window) return;

        var result = await dialog.ShowDialog<Color?>(window);
        if (result.HasValue)
            entry.Color = result.Value;
    }
}
