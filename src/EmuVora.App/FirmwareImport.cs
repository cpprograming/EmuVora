using EmuVora.Core;
using Avalonia.Controls;
using Avalonia.Media;

namespace EmuVora.App;

public partial class MainWindow
{
    private async Task ImportFirmwareFilesAsync(string[] files)
    {
        var imported=new List<string>();var failed=new List<string>();
        await BusyAsync(async token=>await Task.Run(()=>
        {
            foreach(var source in files)
            {
                token.ThrowIfCancellationRequested();
                try{var target=EmulatorSetup.ImportFirmware(source,dataDir);imported.Add(Path.GetFileName(source)+" → "+Path.GetRelativePath(EmulatorSetup.FirmwareDirectory(dataDir),target));}
                catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException){failed.Add(Path.GetFileName(source)+": "+ex.Message);}
            }
        },token));
        status.Text=$"{imported.Count} BIOS-/Firmware-Dateien importiert. Bekannte Prüfsummen erhalten automatisch den passenden Namen; unbekannte Dateien behalten ihren Namen.\n"+string.Join("\n",imported.Concat(failed.Select(f=>"Nicht importiert: "+f)));
        var owner=OwnedWindows.LastOrDefault(w=>w.IsActive)??this;
        var dialog=new Window{Title="BIOS-Import",Width=680,Height=480,Background=Background,Foreground=Foreground,RequestedThemeVariant=RequestedThemeVariant,WindowStartupLocation=WindowStartupLocation.CenterOwner};WindowAppearance.Apply(dialog);ControllerUi.Attach(dialog);
        var layout=new DockPanel{Margin=new Avalonia.Thickness(24)};var close=new Button{Content="Schließen",Margin=new Avalonia.Thickness(0,16,0,0)};close.Click+=(_,_)=>dialog.Close();DockPanel.SetDock(close,Dock.Bottom);layout.Children.Add(close);layout.Children.Add(new ScrollViewer{Content=new TextBlock{Text=status.Text,TextWrapping=TextWrapping.Wrap}});dialog.Content=layout;await dialog.ShowDialog(owner);
    }
}
