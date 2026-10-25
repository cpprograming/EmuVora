using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using EmuVora.Core;
using System.Text.Json;
namespace EmuVora.App;
public sealed partial class MainWindow
{
    private async Task<IReadOnlyList<SystemReadinessInfo>?> ReadSystemReadinessAsync()
    {
        var snapshot=JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings,SettingsStore.JsonOptions),SettingsStore.JsonOptions)!;var games=allGames.ToArray();IReadOnlyList<SystemReadinessInfo>? result=null;
        if(!await BusyAsync(async token=>result=await Task.Run(()=>SystemReadiness.Inspect(dataDir,snapshot,games,token),token)))return null;status.Text="Systemprüfung abgeschlossen. Keine Dateien verändert.";return result;
    }
    private async Task ShowSystemCenterAsync()
    {
        var owner=OwnedWindows.LastOrDefault(w=>w.IsActive)??this;var rows=await ReadSystemReadinessAsync();if(rows==null||isClosed||!owner.IsVisible)return;
        var panel=new StackPanel{Spacing=14,Margin=new(24)};panel.Children.Add(new TextBlock{Text="Deine Systeme",FontSize=28,FontWeight=FontWeight.Bold});
        panel.Children.Add(new TextBlock{Text="Emulatoren, Cores, BIOS und Bibliothek an einem Ort. Vorhandene Dateien sind noch kein erfolgreicher Spieltest.",TextWrapping=TextWrapping.Wrap});
        var summary=new TextBlock{FontWeight=FontWeight.SemiBold,TextWrapping=TextWrapping.Wrap};panel.Children.Add(summary);
        var query=new TextBox{Watermark="System suchen …"};panel.Children.Add(query);var filter=new ComboBox{ItemsSource=new[]{"Meine Systeme","Alle Systeme","Handlungsbedarf bei meinen Spielen"},SelectedIndex=rows.Any(r=>r.Files>0)?0:1,HorizontalAlignment=HorizontalAlignment.Stretch};panel.Children.Add(filter);
        var list=new ListBox{Height=280,ItemTemplate=new FuncDataTemplate<SystemReadinessInfo>((row,_)=>new StackPanel{Spacing=4,Margin=new(8),Children={new TextBlock{Text=row?.System,FontWeight=FontWeight.Bold},new TextBlock{Text=row==null?"":$"{row.Titles} Titel · {row.Files} Dateien · {row.Status}",TextWrapping=TextWrapping.Wrap,Opacity=.8}}})};
        var detail=new StackPanel{Spacing=10,Margin=new(16,0,0,0)};var name=new TextBlock{FontSize=23,FontWeight=FontWeight.Bold,TextWrapping=TextWrapping.Wrap};var state=new TextBlock{TextWrapping=TextWrapping.Wrap};var library=new TextBlock{TextWrapping=TextWrapping.Wrap};var firmware=new TextBlock{TextWrapping=TextWrapping.Wrap};var notes=new TextBlock{TextWrapping=TextWrapping.Wrap,Opacity=.8};detail.Children.Add(name);detail.Children.Add(state);detail.Children.Add(library);detail.Children.Add(firmware);detail.Children.Add(notes);
        var body=new Grid{ColumnDefinitions=new("*,*"),ColumnSpacing=8};body.Children.Add(list);Grid.SetColumn(detail,1);body.Children.Add(detail);panel.Children.Add(body);
        panel.Children.Remove(body);panel.Margin=new(0);var message=new TextBlock{TextWrapping=TextWrapping.Wrap};var actions=new WrapPanel{Orientation=Orientation.Horizontal};var footer=new StackPanel{Spacing=8,Margin=new(0,14,0,0)};footer.Children.Add(message);footer.Children.Add(actions);
        var layout=new Grid{RowDefinitions=new("Auto,*,Auto"),Margin=new(24)};panel.Margin=new(0,0,0,14);layout.Children.Add(panel);var middle=new ScrollViewer{Content=body};Grid.SetRow(middle,1);layout.Children.Add(middle);Grid.SetRow(footer,2);layout.Children.Add(footer);var dialog=FixedDialog("EmuVora · Systemübersicht",layout);dialog.Width=1000;dialog.Height=800;
        SystemReadinessInfo? SelectedSystem()=>list.SelectedItem as SystemReadinessInfo;
        Button setup=new(),preferences=new(),showGames=new(),showCovers=new();
        var centerActions=new List<Button>();void Add(Button button){button.Width=280;button.Margin=new(0,0,8,8);actions.Children.Add(button);if(button.Content as string!="Schließen")centerActions.Add(button);}
        setup=AsyncButton("Ausgewählte Konsole einrichten",async()=>{if(SelectedSystem() is{} row)await Install([row.System]);});Add(setup);
        Add(AsyncButton("Fehlendes für meine Spiele einrichten",()=>Install(rows.Where(r=>r.Files>0).Select(r=>r.System).ToArray())));
        Add(AsyncButton("Eigene BIOS-Dateien importieren …",async()=>{var files=await new ControllerPicker(dialog).OpenFilePathsAsync("Eigene BIOS-/Firmware-Dateien");if(files.Length>0){await ImportFirmwareFilesAsync(files);await Recheck();}}));
        preferences=AsyncButton("Emulator-Einstellungen öffnen",async()=>{if(SelectedSystem()?.EmulatorId is{} id){await ShowSettingsAsync(id);await Recheck();}});Add(preferences);
        void OpenLibrary(string target){if(operation!=null||SelectedSystem() is not{} row||row.Files==0)return;dialog.Close();if(owner!=this)owner.Close();view=target;search.Text="";systems.SelectedItem=row.System;Refresh();Show();Activate();gamesList.Focus();}
        showGames=ActionButton("Spiele dieser Konsole anzeigen",()=>OpenLibrary("Alle Spiele"));Add(showGames);showCovers=ActionButton("Fehlende Cover ansehen",()=>OpenLibrary("Fehlende Cover"));Add(showCovers);
        Add(AsyncButton("Status neu prüfen",Recheck));Add(ActionButton("Schließen",dialog.Close));
        void Detail()
        {
            var row=SelectedSystem();name.Text=row?.System??"System auswählen";state.Text=row==null?"":row.Status+"\n"+(row.EmulatorId==null?"Konsole in den Spieldetails zuordnen.":EmulatorCatalog.Get(row.EmulatorId).Name+": "+(row.EmulatorMissing?"Programm fehlt / Pfad prüfen":"Programmdatei erreichbar")+(row.EmulatorId=="retroarch"?row.CoreMissing?"\nCore fehlt / Pfad prüfen":"\nCore-Datei erreichbar":""));
            library.Text=row==null?"":$"{row.Titles} Titel · {row.Files} Dateien\n{row.MissingFiles} nicht erreichbare Spiele\n{row.MissingCovers} fehlende Cover";firmware.Text=row==null?"":!row.Known?"Firmwarestatus erst nach Konsolenzuordnung prüfbar.":row.FirmwareHint??"Keine fehlende Pflicht-Firmware erkannt.";notes.Text=row?.Note;ActionState();
        }
        void ActionState(){var row=SelectedSystem();foreach(var button in centerActions)button.IsEnabled=operation==null;setup.IsEnabled=row?.NeedsSetup==true&&operation==null;preferences.IsEnabled=row?.Known==true&&operation==null;showGames.IsEnabled=row?.Files>0&&operation==null;showCovers.IsEnabled=row?.Files>0&&operation==null;}
        void Filter()
        {
            var selected=SelectedSystem()?.System;var text=query.Text?.Trim()??"";var visible=rows.Where(r=>(filter.SelectedIndex==1||r.Files>0)&&(filter.SelectedIndex!=2||r.NeedsAttention)&&r.System.Contains(text,StringComparison.OrdinalIgnoreCase)).ToArray();list.ItemsSource=visible;list.SelectedItem=visible.FirstOrDefault(r=>r.System==selected)??visible.FirstOrDefault();
            summary.Text=$"{rows.Count(r=>r.Files>0)} Systeme mit Spielen · {rows.Count(r=>r.Files>0&&r.NeedsSetup)} benötigen Einrichtung · {rows.Count(r=>r.Files>0&&r.FirmwareHint!=null)} mit Firmwarehinweis\n{visible.Length} Systeme in dieser Ansicht";Detail();
        }
        async Task Recheck(){var fresh=await ReadSystemReadinessAsync();if(fresh!=null&&!isClosed&&dialog.IsVisible){rows=fresh;Filter();message.Text="Status aktualisiert. Es wurden keine Spieldateien verändert.";}}
        async Task Install(string[] scope)
        {
            if(scope.Length==0){message.Text="Zuerst Spiele importieren oder eine einzelne Konsole zur Einrichtung auswählen.";return;}
            if(running.Count>0){message.Text="Laufende Spiele zuerst schließen.";return;}var fresh=await ReadSystemReadinessAsync();if(fresh==null||!dialog.IsVisible)return;rows=fresh;Filter();var plan=SystemReadiness.MissingPackages(rows.Where(r=>scope.Contains(r.System)));
            if(plan.Length==0){message.Text="Keine fehlenden Programme oder Cores in dieser Auswahl. Firmwarehinweise separat prüfen; BIOS-Dateien selbst importieren.";return;}
            var preview=new StackPanel{Margin=new(24),Spacing=16};preview.Children.Add(new TextBlock{Text="Fehlende Komponenten einrichten",FontSize=24,FontWeight=FontWeight.Bold,TextWrapping=TextWrapping.Wrap});preview.Children.Add(new TextBlock{Text=string.Join("\n",plan.Select(id=>id=="retroarch-cores"?"RetroArch-Core-Paket":EmulatorCatalog.Get(id).Name))+"\n\nNur diese fehlenden Komponenten werden aus den offiziellen Quellen heruntergeladen und mit SHA-256 geprüft. Funktionierende eigene Programme/Cores bleiben erhalten. BIOS-Dateien werden nicht heruntergeladen. Abbruch ist während des Downloads möglich.",TextWrapping=TextWrapping.Wrap});
            var confirm=Dialog("Einrichtung bestätigen",preview);preview.Children.Add(ActionButton("Jetzt installieren",()=>confirm.Close(true)));preview.Children.Add(ActionButton("Abbrechen",()=>confirm.Close(false)));if(!await confirm.ShowDialog<bool>(dialog)||isClosed||!dialog.IsVisible)return;
            fresh=await ReadSystemReadinessAsync();if(fresh==null||running.Count>0)return;var current=SystemReadiness.MissingPackages(fresh.Where(r=>scope.Contains(r.System)));if(current.Except(plan).Any()){message.Text="Die benötigten Komponenten haben sich geändert. Bitte die Einrichtung erneut mit aktueller Vorschau öffnen.";return;}
            var success=await BusyAsync(async token=>{foreach(var id in current){token.ThrowIfCancellationRequested();await InstallManagedAsync(id,token);}});var failure=operationFailure;var feedback=status.Text;await Recheck();if(!success){operationFailure=failure;NotifyFeedback();}message.Text=success?"Fehlende Komponenten eingerichtet. Firmwarehinweise und echte Spieltests bleiben separat zu prüfen.":feedback;
        }
        list.SelectionChanged+=(_,_)=>Detail();query.TextChanged+=(_,_)=>Filter();filter.SelectionChanged+=(_,_)=>Filter();
        dialog.PropertyChanged+=(_,e)=>{if(e.Property==ClientSizeProperty){var compact=dialog.ClientSize.Width<760;body.ColumnDefinitions=new(compact?"*":"*,*");body.RowDefinitions=new(compact?"Auto,Auto":"Auto");Grid.SetColumn(detail,compact?0:1);Grid.SetRow(detail,compact?1:0);detail.Margin=new(compact?0:16,compact?16:0,0,0);list.Height=compact?180:280;foreach(var button in actions.Children.OfType<Button>())button.Width=Math.Clamp((dialog.ClientSize.Width-64)/2,180,280);}};
        dialog.Closing+=(_,e)=>{if(operation!=null){e.Cancel=true;RequestOperationCancel();}};FeedbackChanged+=ActionState;dialog.Closed+=(_,_)=>FeedbackChanged-=ActionState;Filter();await dialog.ShowDialog(owner);
    }
}
