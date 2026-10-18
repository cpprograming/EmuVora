namespace EmuVora.App;

public sealed partial class MainWindow
{
    private async Task ShowControllerDiagnosticsAsync()
    {
        var dialog=new ControllerDiagnosticsWindow{Background=Background,Foreground=Foreground,RequestedThemeVariant=RequestedThemeVariant};
        await dialog.ShowDialog(OwnedWindows.LastOrDefault(w=>w.IsActive)??this);
    }
}
