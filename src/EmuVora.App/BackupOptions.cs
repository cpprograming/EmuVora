using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using EmuVora.Core;
using System.Diagnostics;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private StackPanel BackupOptions()
    {
        var panel=new StackPanel{Spacing=14,Margin=new(24)};
        var message=new TextBlock{TextWrapping=TextWrapping.Wrap};string? restored=null;
        panel.Children.Add(new TextBlock{Text="Sicherung & Wiederherstellung",FontSize=25,FontWeight=FontWeight.Bold});
        panel.Children.Add(new TextBlock{Text="Sichert Einstellungen, Bibliothek, Cover im Datenordner sowie saves und states. Erkannte Speicherordner verwalteter DuckStation-, PCSX2-, Dolphin- und PPSSPP-Installationen werden ebenfalls kopiert. ROMs, BIOS, Emulatorprogramme und Themes sind nicht enthalten. Externe Speicherordner bitte separat sichern. Spiele und ES-DE vorher beenden.",TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(AsyncButton("Datensicherung erstellen …",async()=>
        {
            if(!CanBackUp())return;
            var folders=await new ControllerPicker(this).OpenFolderPickerAsync(new(){Title="Sicherungsordner außerhalb des EmuVora-Datenordners wählen"});
            if(folders.Count==0||folders[0].TryGetLocalPath() is not{} folder)return;
            var completed=await BusyAsync(async token=>
            {
                if(!CanBackUp())return;settingsStore.Save(settings);
                var file=Path.Combine(folder,"EmuVora-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..6]+".emuvora.zip");
                message.Text="Datensicherung wird erstellt …";
                await Task.Run(()=>Backups.CreateAsync(dataDir,file,token),token);
                message.Text="Datensicherung erstellt und mit SHA-256-Prüfsummen versehen:\n"+file;
            });
            if(!completed)message.Text=status.Text;
        }));
        panel.Children.Add(AsyncButton("Datensicherung prüfen und wiederherstellen …",async()=>
        {
            if(!CanBackUp())return;
            var files=await new ControllerPicker(this).OpenFilePickerAsync(new(){Title="EmuVora-Datensicherung wählen",FileTypeFilter=[new("Datensicherung"){Patterns=["*.emuvora.zip"]}]});
            if(files.Count==0||files[0].TryGetLocalPath() is not{} file)return;
            var manifest=await Task.Run(()=>Backups.Inspect(file));
            var preview=new StackPanel{Spacing=14,Margin=new(24)};
            preview.Children.Add(new TextBlock{Text=$"Sicherung vom {manifest.Created.ToLocalTime():g}\n{manifest.Files.Count} Dateien · {manifest.Files.Sum(f=>f.Size)/1024d/1024:0.0} MiB\n\nAlle Dateien werden beim Entpacken geprüft. Als Ziel wird ein neuer Unterordner angelegt; deine aktuelle Bibliothek bleibt erhalten. Erst nach erfolgreicher Prüfung kannst du die wiederhergestellte Bibliothek öffnen. Erkannte Emulator-Spielstände werden bei der nächsten automatischen Emulator-Einrichtung übernommen. Vorhandene Zieldateien und die gesicherte Quelle bleiben erhalten.",TextWrapping=TextWrapping.Wrap});
            var dialog=Dialog("Datensicherung wiederherstellen",preview);
            preview.Children.Add(ActionButton("Zielordner auswählen",()=>dialog.Close(true)));
            preview.Children.Add(ActionButton("Abbrechen",()=>dialog.Close(false)));
            if(!await dialog.ShowDialog<bool>(OwnedWindows.LastOrDefault(w=>w.IsActive)??this))return;
            var folders=await new ControllerPicker(this).OpenFolderPickerAsync(new(){Title="Übergeordneten Zielordner wählen"});
            if(folders.Count==0||folders[0].TryGetLocalPath() is not{} parent)return;
            var completed=await BusyAsync(async token=>
            {
                message.Text="Datensicherung wird geprüft und wiederhergestellt …";
                var result=await Task.Run(()=>Backups.RestoreAsync(file,parent,token),token);restored=result;
                message.Text="Geprüft und wiederhergestellt:\n"+result+"\nEmulatoren und BIOS müssen gegebenenfalls erneut eingerichtet werden. Erkannte Emulator-Spielstände werden bei der nächsten automatischen Einrichtung übernommen. Bestehende Zieldateien bleiben erhalten; Details stehen im Spielstandsbericht. ROM-Pfade bleiben erhalten.";
            });
            if(!completed)message.Text=status.Text;
        }));
        panel.Children.Add(ActionButton("Sicherungsvorgang abbrechen",()=>operation?.Cancel()));
        panel.Children.Add(ActionButton("Wiederhergestellten Ordner öffnen",()=>{if(restored!=null)OpenPath(restored);else message.Text="Bitte zuerst eine Datensicherung wiederherstellen.";}));
        panel.Children.Add(AsyncButton("Mit wiederhergestellter Bibliothek neu starten",async()=>
        {
            if(restored==null){message.Text="Bitte zuerst eine Datensicherung wiederherstellen.";return;}
            if(operation!=null||!CanBackUp())return;
            var start=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false};start.ArgumentList.Add("--data-dir");start.ArgumentList.Add(restored);
            var locator=Path.Combine(AppContext.BaseDirectory,"emuvora-location.json");
            new DataLocation(File.Exists(locator)?locator:DataLocation.BootstrapFile).Select(restored);
            _=Process.Start(start)??throw new IOException("Neustart fehlgeschlagen.");
            if(Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime)lifetime.Shutdown();
            await Task.CompletedTask;
        }));
        panel.Children.Add(ActionButton("Spielstandsbericht öffnen",()=>{var report=Path.Combine(dataDir,"save-restore-report.txt");if(File.Exists(report))OpenPath(report);else message.Text="Noch keine automatische Spielstandsübernahme erfolgt.";}));
        panel.Children.Add(message);return panel;
    }
    private bool CanBackUp()
    {
        if(running.Count==0&&(esDeProcess==null||esDeProcess.HasExited))return true;
        status.Text="Bitte zuerst laufende Spiele und ES-DE beenden, damit Spielstände vollständig gesichert werden.";return false;
    }
}
