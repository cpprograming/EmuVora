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
        panel.Children.Add(new TextBlock{Text="Sichert Einstellungen, Bibliothek, Cover, erkannte Emulator-Spielstände und ausgewählte zusätzliche Spielstandsordner. Wähle nur Ordner mit Spielständen: Zusätzliche Ordner werden vollständig kopiert. Fehlende Ordner stoppen die Sicherung. Laufende Spiele und Emulatoren vorher beenden.",TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(ExternalSaveOptions());
        var firmware=new CheckBox{Content="Importierte BIOS-/Firmware-Dateien mitsichern",IsChecked=settings.IncludeFirmwareInBackups};
        firmware.IsCheckedChanged+=(_,_)=>Guard(()=>{settings.IncludeFirmwareInBackups=firmware.IsChecked==true;settingsStore.Save(settings);});panel.Children.Add(firmware);
        panel.Children.Add(new TextBlock{Text="Optional: Sichert alle Dateien im firmware-Unterordner dieses Datenordners, einschließlich vorhandener .bak-Dateien. Externe BIOS-Ordner werden nicht automatisch kopiert. Das Archiv enthält dann deine eigenen BIOS-Dateien und ist nicht verschlüsselt. Bewahre es an einem privaten Sicherungsort auf.",TextWrapping=TextWrapping.Wrap,FontSize=12});
        panel.Children.Add(new TextBlock{Text="Sicherungserinnerung beim Programmstart"});
        var reminder=new ComboBox{ItemsSource=new[]{"Ausgeschaltet","Alle 7 Tage","Alle 14 Tage","Alle 30 Tage"},SelectedIndex=Math.Max(0,Array.IndexOf(new[]{0,7,14,30},settings.BackupReminderDays))};
        reminder.SelectionChanged+=(_,_)=>Guard(()=>{settings.BackupReminderDays=new[]{0,7,14,30}[Math.Max(0,reminder.SelectedIndex)];settingsStore.Save(settings);UpdateBackupReminder();});panel.Children.Add(reminder);
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
                settings.LastBackupAt=DateTimeOffset.UtcNow;settingsStore.Save(settings);UpdateBackupReminder();
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
            var firmwareFiles=manifest.Files.Where(f=>f.Path.StartsWith("firmware/",StringComparison.OrdinalIgnoreCase)).ToArray();
            preview.Children.Add(new TextBlock{Text=firmwareFiles.Length==0?"Keine BIOS-/Firmware-Dateien in diesem Archiv. Bei Bedarf nach der Wiederherstellung erneut importieren.":$"BIOS/Firmware enthalten: {firmwareFiles.Length} Dateien · {firmwareFiles.Sum(f=>f.Size)/1024d/1024:0.0} MiB. Werden mit SHA-256 geprüft und ausschließlich im neuen Datenordner unter firmware wiederhergestellt. Externe BIOS-Pfade im Emulator bei Bedarf anpassen.",TextWrapping=TextWrapping.Wrap});
            if(manifest.ExternalFolders?.Count>0)preview.Children.Add(new TextBlock{Text="Zusätzliche Spielstandsordner:\n"+string.Join("\n",manifest.ExternalFolders.Select(folder=>folder.Path))+"\n\nDiese Dateien werden ausschließlich im neuen Datenordner unter external-saves wiederhergestellt. Die ursprünglichen Ordner bleiben unverändert. Im Emulator musst du den neuen Speicherort bei Bedarf selbst auswählen.",TextWrapping=TextWrapping.Wrap});
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
                message.Text="Geprüft und wiederhergestellt:\n"+result+"\nEmulatoren müssen gegebenenfalls erneut eingerichtet werden. Enthaltene BIOS-/Firmware-Dateien liegen im neuen Datenordner unter firmware; externe BIOS-Pfade bei Bedarf im Emulator anpassen. Erkannte Emulator-Spielstände werden bei der nächsten automatischen Einrichtung übernommen. Zusätzliche Ordner liegen getrennt unter external-saves; ihre Zuordnung steht in der Übersicht. Im Emulator bei Bedarf den neuen Speicherort auswählen. ROM-Pfade bleiben erhalten.";
            });
            if(!completed)message.Text=status.Text;
        }));
        panel.Children.Add(ActionButton("Sicherungsvorgang abbrechen",RequestOperationCancel));
        panel.Children.Add(ActionButton("Wiederhergestellten Ordner öffnen",()=>{if(restored!=null)OpenPath(restored);else message.Text="Bitte zuerst eine Datensicherung wiederherstellen.";}));
        panel.Children.Add(AsyncButton("Mit wiederhergestellter Bibliothek neu starten",async()=>
        {
            if(restored==null){message.Text="Bitte zuerst eine Datensicherung wiederherstellen.";return;}
            await SwitchDataDirectoryAsync(restored,message);
        }));
        panel.Children.Add(ActionButton("Spielstandsbericht öffnen",()=>{var report=Path.Combine(dataDir,"save-restore-report.txt");if(File.Exists(report))OpenPath(report);else message.Text="Noch keine automatische Spielstandsübernahme erfolgt.";}));
        panel.Children.Add(ActionButton("Übersicht zusätzlicher Spielstände öffnen",()=>{var report=Path.Combine(restored??dataDir,"external-save-folders.json");if(File.Exists(report))OpenPath(report);else message.Text="Noch keine zusätzlichen Spielstandsordner wiederhergestellt.";}));
        panel.Children.Add(message);return panel;
    }
    private Expander ExternalSaveOptions()
    {
        var message=new TextBlock{TextWrapping=TextWrapping.Wrap};
        var panel=new StackPanel{Spacing=10};var list=new ListBox{Height=120};
        list.ItemTemplate=new Avalonia.Controls.Templates.FuncDataTemplate<ExternalSaveFolder>((folder,_)=>new TextBlock{Text=folder?.ToString(),TextWrapping=TextWrapping.Wrap,MaxWidth=580});
        var section=new Expander{Header="Zusätzliche Spielstandsordner",Content=panel};
        void RefreshFolders(){list.ItemsSource=settings.ExternalSaveFolders.ToArray();section.Header=$"Zusätzliche Spielstandsordner ({settings.ExternalSaveFolders.Count})";}
        panel.Children.Add(new TextBlock{Text="Zusätzliche oder externe Speicherorte auswählen. Entfernen ändert nur die Sicherungsauswahl. Dateien werden nicht gelöscht. Ziel der Sicherungsdatei außerhalb aller ausgewählten Ordner wählen.",TextWrapping=TextWrapping.Wrap});panel.Children.Add(list);
        panel.Children.Add(message);
        panel.Children.Add(AsyncButton("Spielstandsordner hinzufügen …",async()=>
        {
            if(operation!=null){message.Text="Bitte den laufenden Vorgang abschließen oder abbrechen.";return;}
            var folders=await new ControllerPicker(this).OpenFolderPickerAsync(new(){Title="Zusätzliche Spielstandsordner auswählen",AllowMultiple=true});
            if(operation!=null){message.Text="Bitte den laufenden Vorgang abschließen oder abbrechen.";return;}
            var selected=settings.ExternalSaveFolders.ToList();
            foreach(var folder in folders)if(folder.TryGetLocalPath() is{} path)selected.Add(new(Guid.NewGuid().ToString("N"),path));
            if(folders.Count==0)return;
            try{settings.ExternalSaveFolders=ExternalSaves.ValidateSelection(dataDir,selected).ToList();settingsStore.Save(settings);RefreshFolders();message.Text="Zusätzliche Spielstandsordner gespeichert.";}
            catch(Exception ex){message.Text=ex.Message;}
        }));
        panel.Children.Add(ActionButton("Aus Sicherungsauswahl entfernen",()=>
        {
            if(operation!=null){message.Text="Bitte den laufenden Vorgang abschließen oder abbrechen.";return;}
            if(list.SelectedItem is not ExternalSaveFolder folder){message.Text="Zuerst einen zusätzlichen Spielstandsordner auswählen.";return;}
            settings.ExternalSaveFolders.Remove(folder);settingsStore.Save(settings);RefreshFolders();message.Text="Ordner aus der Auswahl entfernt. Seine Dateien bleiben erhalten.";
        }));
        RefreshFolders();return section;
    }
    private bool CanBackUp()
    {
        if(running.Count==0)return true;
        status.Text="Bitte zuerst laufende Spiele beenden, damit Spielstände vollständig gesichert werden.";return false;
    }
}

