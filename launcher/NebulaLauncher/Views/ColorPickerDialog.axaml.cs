using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace NebulaLauncher.Views;

public partial class ColorPickerDialog : Window
{
    private readonly ColorPicker _picker = null!;

    // Required for XAML loader
    public ColorPickerDialog()
    {
        InitializeComponent();
        _picker = this.FindControl<ColorPicker>("Picker")
                  ?? throw new InvalidOperationException("ColorPicker 'Picker' not found.");
    }

    public ColorPickerDialog(Color initial)
    {
        InitializeComponent();
        _picker = this.FindControl<ColorPicker>("Picker")
                  ?? throw new InvalidOperationException("ColorPicker 'Picker' not found in XAML.");
        _picker.Color = initial;
    }

    private void OnApply(object? sender, RoutedEventArgs e) =>
        Close(_picker.Color);

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close(null);
}
