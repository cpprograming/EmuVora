using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using EmuVora.Core;
using System.Text.Json;
namespace EmuVora.App;
public sealed partial class MainWindow
{
    private async Task ShowDiagnosticsAsync()
    {
        var snapshot=JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings,SettingsStore.JsonOptions),SettingsStore.JsonOptions)!;
        var games=allGames.ToArray();var display=DisplayDetection.Current(TryGetPlatformHandle()?.Handle??IntPtr.Zero);var hasError=operationFailure!=null;var version=typeof(MainWindow).Assembly.GetName().Version?.ToString()??"unbekannt";string? report=null;
        if(!await BusyAsync(async token=>{report=await Task.Run(()=>Diagnostics.Create(dataDir,snapshot,games,version,display,hasError,token),token);})||report==null||isClosed)return;
        status.Text="Diagnosebericht erstellt. Noch keine Datei gespeichert oder hochgeladen.";
        var panel=new StackPanel{Margin=new(24),Spacing=14};panel.Children.Add(new TextBlock{Text="Diagnosebericht · Vorschau",FontSize=24,FontWeight=FontWeight.Bold});
        var notice=new TextBlock{Text="Nur zusammengefasste Prüfergebnisse. Keine Spielnamen, privaten Pfade, Benutzernamen, Gerätekennungen oder Protokollinhalte. Der Bericht wird nicht hochgeladen. Speichern ist freiwillig.",TextWrapping=TextWrapping.Wrap};panel.Children.Add(notice);
        panel.Children.Add(new TextBox{Text=report,IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,Height=300,FontFamily=new FontFamily("Consolas, monospace")});
        var dialog=Dialog("EmuVora · Diagnosebericht",panel);dialog.Height=650;
        panel.Children.Add(AsyncButton("Bericht lokal speichern …",async()=>
        {
            var folders=await new ControllerPicker(dialog).OpenFolderPickerAsync(new(){Title="Zielordner für den Diagnosebericht wählen"});if(folders.Count==0||folders[0].TryGetLocalPath() is not{} folder)return;
            var file=Path.Combine(folder,"EmuVora-Diagnose-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..8]+".txt");
            await using(var stream=new FileStream(file,FileMode.CreateNew,FileAccess.Write,FileShare.None))await using(var writer=new StreamWriter(stream)){await writer.WriteAsync(report);}notice.Text=status.Text="Diagnosebericht lokal gespeichert: "+file+"\nKein Upload.";
        }));panel.Children.Add(ActionButton("Schließen",dialog.Close));await dialog.ShowDialog(OwnedWindows.LastOrDefault(w=>w.IsActive)??this);
    }
}
