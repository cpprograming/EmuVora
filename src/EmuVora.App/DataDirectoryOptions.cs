using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using EmuVora.Core;
using System.Diagnostics;

namespace EmuVora.App;
public sealed partial class MainWindow
{
    private Expander DataDirectoryOptions()
    {
        var panel=new StackPanel{Spacing=12};var message=new TextBlock{TextWrapping=TextWrapping.Wrap};
        panel.Children.Add(new TextBlock{Text="Aktueller Datenordner:\n"+dataDir,TextWrapping=TextWrapping.Wrap});
        string? previous=null;
        try{previous=DataDirectorySwitch.Previous(dataDir);}
        catch(Exception ex){message.Text="Gespeicherter Rückweg konnte nicht gelesen werden: "+ex.Message;}
        panel.Children.Add(new TextBlock{Text=previous==null?"Noch kein vorheriger Datenordner gespeichert.":"Vorheriger Datenordner:\n"+previous+(Directory.Exists(previous)?"":"\nDer Ordner ist derzeit nicht erreichbar."),TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(new TextBlock{Text="Öffnet eine vorhandene Bibliothek nach Prüfung und Bestätigung mit einem Neustart. Es werden keine Spiele oder Spielstände zwischen Ordnern verschoben. Laufende Spiele und Vorgänge zuerst beenden.",TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(AsyncButton("Vorhandenen Datenordner wählen …",async()=>
        {
            if(operation!=null||!CanBackUp()){message.Text="Bitte laufende Spiele und Vorgänge zuerst beenden.";return;}
            var folders=await new ControllerPicker(this).OpenFolderPickerAsync(new(){Title="Vorhandenen EmuVora-Datenordner wählen"});
            if(folders.Count>0&&folders[0].TryGetLocalPath() is{} path)await SwitchDataDirectoryAsync(path,message);
        }));
        var back=AsyncButton("Zur vorherigen Bibliothek zurück",async()=>{if(previous!=null)await SwitchDataDirectoryAsync(previous,message);});back.IsEnabled=previous!=null;panel.Children.Add(back);
        panel.Children.Add(message);return new Expander{Header="Bibliothek wechseln",Content=panel};
    }
    private async Task SwitchDataDirectoryAsync(string target,TextBlock message)
    {
        if(operation!=null||!CanBackUp()){message.Text="Bitte laufende Spiele und Vorgänge zuerst beenden.";return;}
        try
        {
            var owner=OwnedWindows.LastOrDefault(window=>window.IsActive)??this;
            var summary=await Task.Run(()=>DataDirectorySwitch.Inspect(target));
            if(isClosed||!owner.IsVisible)return;
            if(ExternalSavesPathEquals(dataDir,summary.Directory)){message.Text="Diese Bibliothek ist bereits geöffnet.";return;}
            var panel=new StackPanel{Spacing=14,Margin=new(24)};
            panel.Children.Add(new TextBlock{Text=$"Aktuell:\n{dataDir}\n\nÖffnen:\n{summary.Directory}\n{summary.Games} Spiele\n\nEmuVora wird neu gestartet. Bibliotheksdateien und Spielstände bleiben an ihren bisherigen Orten. Der aktuelle Datenordner wird als Rückweg gespeichert.",TextWrapping=TextWrapping.Wrap});
            var dialog=Dialog("Bibliothek wechseln",panel);dialog.Height=460;
            panel.Children.Add(ActionButton("Wechseln und neu starten",()=>dialog.Close(true)));panel.Children.Add(ActionButton("Abbrechen",()=>dialog.Close(false)));
            if(!await dialog.ShowDialog<bool>(owner))return;
            if(operation!=null||!CanBackUp()){message.Text="Bitte laufende Spiele und Vorgänge zuerst beenden.";return;}
            var switched=await BusyAsync(async token=>
            {
                settingsStore.Save(settings);
                var local=Path.Combine(AppContext.BaseDirectory,"emuvora-location.json");var locator=File.Exists(local)?local:DataLocation.BootstrapFile;
                await Task.Run(()=>new DataDirectorySwitch(locator).Switch(dataDir,summary.Directory,path=>
                {
                    var start=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false};start.ArgumentList.Add("--data-dir");start.ArgumentList.Add(path);
                    using var process=Process.Start(start)??throw new IOException("EmuVora konnte nicht neu gestartet werden.");
                },token),token);
            });
            if(!switched){message.Text=status.Text;return;}
            if(Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime)lifetime.Shutdown();
        }
        catch(Exception ex){message.Text="Bibliothek konnte nicht gewechselt werden: "+ex.Message;}
    }
    private static bool ExternalSavesPathEquals(string first,string second)=>Path.GetFullPath(first).TrimEnd(Path.DirectorySeparatorChar).Equals(Path.GetFullPath(second).TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase);
    private void UpdateBackupReminder()=>backupReminder.IsVisible=BackupReminders.IsDue(settings,DateTimeOffset.UtcNow);
}
