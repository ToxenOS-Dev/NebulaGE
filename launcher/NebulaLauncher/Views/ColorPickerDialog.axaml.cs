using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace NebulaLauncher.Views;

public partial class ColorPickerDialog : Window
{
    private readonly ColorView _picker = null!;

    // Required for XAML loader
    public ColorPickerDialog()
    {
        InitializeComponent();
        _picker = this.FindControl<ColorView>("Picker")
                  ?? throw new InvalidOperationException("ColorView 'Picker' not found.");
    }

    public ColorPickerDialog(Color initial)
    {
        InitializeComponent();
        _picker = this.FindControl<ColorView>("Picker")
                  ?? throw new InvalidOperationException("ColorView 'Picker' not found in XAML.");
        _picker.Color = initial;
    }

    private void OnApply(object? sender, RoutedEventArgs e) =>
        Close(_picker.Color);

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close(null);
}
