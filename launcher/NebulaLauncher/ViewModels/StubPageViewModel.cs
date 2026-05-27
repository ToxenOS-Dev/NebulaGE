namespace NebulaLauncher.ViewModels;

/// <summary>Placeholder page VM for sections not yet implemented.</summary>
public class StubPageViewModel(string title, string message) : ViewModelBase
{
    public string Title   { get; } = title;
    public string Message { get; } = message;
}
