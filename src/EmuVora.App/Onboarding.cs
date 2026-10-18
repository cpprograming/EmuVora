using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using EmuVora.Core;

namespace EmuVora.App;
public sealed partial class MainWindow
{
    private async Task ShowOnboardingAsync()
    {
        if(operation!=null||running.Count>0||esDeProcess!=null){status.Text="Laufende Spiele und Vorgänge zuerst beenden.";return;}
        int step=0;var panel=new StackPanel{Margin=new(24),Spacing=16};var title=new TextBlock{FontSize=26,TextWrapping=TextWrapping.Wrap};var content=new StackPanel{Spacing=12};var feedback=new TextBlock{TextWrapping=TextWrapping.Wrap};var pad=new TextBlock{TextWrapping=TextWrapping.Wrap};var navigation=new StackPanel{Orientation=Orientation.Horizontal,Spacing=12};
        panel.Children.Add(title);panel.Children.Add(content);panel.Children.Add(feedback);panel.Children.Add(navigation);panel.Children.Add(new TextBlock{Text="D-Pad: wählen · A: bestätigen / Texteingabe · B: zurück · LB/RB: Einstellungen-Tabs",TextWrapping=TextWrapping.Wrap,Opacity=0.75});
        var dialog=Dialog("EmuVora · Erste Einrichtung",panel);dialog.Width=760;dialog.Height=690;
        var previous=new Button{Content="Zurück"};var next=new Button{Content="Weiter"};
        previous.Click+=(_,_)=>{if(operation==null&&step>0){step--;Render();}};next.Click+=(_,_)=>{if(operation!=null)return;if(step<4){step++;Render();}else{settings.OnboardingCompleted=true;settingsStore.Save(settings);dialog.Close();status.Text="Einrichtung gespeichert. Spiel auswählen und A drücken.";}};
        ControllerUi.SetBack(dialog,()=>{if(operation!=null){operation.Cancel();return;}if(step>0){step--;Render();}else dialog.Close();});
        navigation.Children.Add(previous);navigation.Children.Add(next);navigation.Children.Add(ActionButton("Später einrichten",()=>{if(operation!=null){operation.Cancel();return;}settings.OnboardingCompleted=true;settingsStore.Save(settings);dialog.Close();}));
        void Text(string value)=>content.Children.Add(new TextBlock{Text=value,TextWrapping=TextWrapping.Wrap});
        void Render()
        {
            content.Children.Clear();feedback.Text="";previous.IsEnabled=step>0;next.Content=step==4?"Zur Bibliothek":"Weiter";
            title.Text=(step+1)+" / 5 · "+new[]{"Dein Speicherort","Deine Spiele","Dein Xbox-Controller","Emulatoren und BIOS","Bereit zum Spielen"}[step];
            if(step==0)
            {
                Text("Datenordner: "+dataDir);Text("Hier bleiben Bibliothek, Cover, Emulatoren und verwaltete Spielstände. Deine ROMs bleiben in ihren bisherigen Ordnern. Den Programmordner hast du im Setup gewählt; portable Kopien wählen den Datenordner vor diesem Assistenten.");
                Text("Monitor: "+(DisplayDetection.Current(TryGetPlatformHandle()?.Handle??IntPtr.Zero)?.ToString()??"wird beim Spielstart erkannt"));Text("Automatische Bildprofile verwenden das Originalformat. Schwarze Ränder verhindern Verzerrungen.");
            }
            if(step==1)
            {
                Text(allGames.Count+" Spiele · "+settings.RomFolders.Count+" ROM-Ordner");Text("Wähle den Ordner mit deinen vorhandenen Spielen. Unterordner werden mitgescannt; unklare Disc-Dateien kannst du anschließend einer Konsole zuordnen.");
                content.Children.Add(AsyncButton("ROM-Ordner hinzufügen",async()=>{await ImportAsync();Render();feedback.Text=status.Text;}));content.Children.Add(AsyncButton("Vorhandene Ordner neu scannen",async()=>{await RescanAsync();Render();feedback.Text=status.Text;}));
                var covers=new CheckBox{Content="Fehlende Cover automatisch laden",IsChecked=settings.AutoDownloadCovers};covers.IsCheckedChanged+=(_,_)=>{settings.AutoDownloadCovers=covers.IsChecked==true;settingsStore.Save(settings);};content.Children.Add(covers);
            }
            if(step==2)
            {
                Text("Xbox-Controller per USB, Bluetooth oder Xbox-Adapter verbinden. EmuVora erkennt Windows-XInput-Controller automatisch. Du kannst ohne Controller fortfahren und ihn später anschließen.");content.Children.Add(pad);content.Children.Add(AsyncButton("Xbox-Controller prüfen",ShowControllerDiagnosticsAsync));
                var xbox=new CheckBox{Content="Xbox-Standardbelegung bei neuen Emulator-Profilen",IsChecked=settings.XboxDefaults};xbox.IsCheckedChanged+=(_,_)=>{settings.XboxDefaults=xbox.IsChecked==true;settingsStore.Save(settings);};content.Children.Add(xbox);Text("Bibliothek: Stick/D-Pad wählen · A spielen · Y Favorit · LB/RB Konsole · Menu Einstellungen · View Bedienelemente, B zurück zur Spieleliste. Texteingabe öffnet eine Bildschirmtastatur. View + Menu beendet unterstützte Emulatoren.");
            }
            if(step==3)
            {
                Text("Es werden nur die Emulatoren für deine importierten Konsolen eingerichtet. Ohne Spiele starten wir mit RetroArch. Eigene funktionierende Emulator-Pfade bleiben erhalten.");
                content.Children.Add(AsyncButton("Benötigte Emulatoren jetzt einrichten",async()=>
                {
                    var definitions=SetupPlan.RequiredEmulators(allGames.Select(g=>g.System),settings);
                    await BusyAsync(async token=>{foreach(var definition in definitions){var executable=settings.EmulatorPaths.GetValueOrDefault(definition.Id);if(executable==null||!File.Exists(executable))await InstallManagedAsync(definition.Id,token);}if(definitions.Any(e=>e.Id=="retroarch"))await InstallManagedAsync("retroarch-cores",token);status.Text="Benötigte Emulatoren und Cores eingerichtet.";});Render();feedback.Text=status.Text;
                }));content.Children.Add(ActionButton("Download abbrechen",()=>operation?.Cancel()));
                content.Children.Add(AsyncButton("Eigene BIOS-Dateien importieren",async()=>{var files=await new ControllerPicker(dialog).OpenFilePathsAsync("Eigene BIOS-/Firmware-Dateien");if(files.Length==0)return;await ImportFirmwareFilesAsync(files);Render();feedback.Text=status.Text;}));
                foreach(var system in allGames.Select(g=>g.System).Distinct().Order())Text(system+": "+(SetupPlan.FirmwareHint(system,dataDir,settings)??"Keine fehlende Pflicht-Firmware erkannt."));Text("BIOS-Dateien werden nicht heruntergeladen. Falls du sie noch nicht hast, kannst du die Einrichtung später fortsetzen. Fehlende Emulatoren werden mit aktivierter automatischer Einrichtung beim ersten Spielstart nachgeladen.");
            }
            if(step==4)
            {
                Text(allGames.Count+" Spiele in deiner Bibliothek. "+(settings.AutoDownloadCovers?"Fehlende Cover werden automatisch gesucht.":"Cover kannst du später herunterladen."));
                Text("Einstellungen → Einrichtungsassistent öffnet diese Schritte erneut. Bei Problemen zeigt EmuVora konkrete Hinweise; eigene Einstellungen werden bei Updates bewahrt.");
                foreach(var system in allGames.Select(g=>g.System).Distinct().Order()){var definition=EmulatorCatalog.Known.FirstOrDefault(e=>e.Systems.Contains(system));var installed=definition!=null&&File.Exists(settings.EmulatorPaths.GetValueOrDefault(definition.Id));Text(system+": "+(installed?"Emulator vorhanden":"Emulator beim ersten Start einrichten")+(SetupPlan.FirmwareHint(system,dataDir,settings) is{} hint?" · "+hint:""));}
            }
        }
        var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(250)};timer.Tick+=(_,_)=>{var connected=Gamepad.Read();pad.Text=connected.Length==0?"Xbox-Controller: noch nicht verbunden":"Xbox-Controller: "+connected.Length+" verbunden · Eingabe: "+string.Join(", ",connected.Select(p=>"#"+(p.Index+1)+" · "+Gamepad.NavigationButtons(p).ToString("X4")));if(operation!=null)feedback.Text=status.Text;};
        dialog.Opened+=(_,_)=>timer.Start();dialog.Closed+=(_,_)=>timer.Stop();dialog.Closing+=(_,e)=>{if(operation!=null){e.Cancel=true;operation.Cancel();feedback.Text="Vorgang wird abgebrochen …";}};Render();await dialog.ShowDialog(this);
    }
}


