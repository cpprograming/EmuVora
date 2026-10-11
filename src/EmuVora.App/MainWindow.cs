using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using EmuVora.Core;
using System.Diagnostics;
using System.Text.Json;

namespace EmuVora.App;

public sealed class MainWindow : Window
{
    private readonly string dataDir;
    private readonly LibraryStore store;
    private readonly SettingsStore settingsStore;
    private readonly AppSettings settings;
    private readonly PackageManager packages=new();
    private readonly ListBox gamesList=new();
    private readonly TextBox search=new(){Watermark="Spiele suchen …"};
    private readonly ComboBox systems=new(){MinWidth=150};
    private readonly ComboBox sort=new(){ItemsSource=new[]{"Titel A–Z","Konsole","Zuletzt gespielt"},SelectedIndex=0,MinWidth=160};
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap};
    private readonly TextBlock summary=new(){FontSize=13,Opacity=0.7};
    private readonly TextBlock empty=new(){TextWrapping=TextWrapping.Wrap,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,FontSize=18};
    private readonly ProgressBar progress=new(){IsVisible=false,Height=5};
    private readonly Button cancel=new(){Content="Abbrechen",IsVisible=false};
    private readonly TextBlock detailTitle=new(){FontSize=24,FontWeight=FontWeight.Bold,TextWrapping=TextWrapping.Wrap};
    private readonly TextBlock detailInfo=new(){TextWrapping=TextWrapping.Wrap,FontSize=13,Opacity=0.8};
    private readonly ContentControl detailCover=new(){Height=240};
    private readonly Button play=new(){Content="▶ Spielen",HorizontalAlignment=HorizontalAlignment.Stretch,IsEnabled=false};
    private readonly Button favorite=new(){Content="☆ Favorit",IsEnabled=false};
    private readonly Button edit=new(){Content="Titel / Konsole bearbeiten",IsEnabled=false};
    private readonly Button cover=new(){Content="Cover wählen",IsEnabled=false};
    private readonly Button onlineCover=new(){Content="Cover online suchen",IsEnabled=false};
    private readonly Button remove=new(){Content="Aus Bibliothek entfernen",IsEnabled=false};
    private readonly Button reveal=new(){Content="ROM-Ordner öffnen",IsEnabled=false};
    private readonly Border sideBorder=new();
    private readonly Border detailBorder=new();
    private readonly Grid main=new(){RowDefinitions=new("Auto,Auto,*,Auto,Auto"),RowSpacing=16,Margin=new(24)};
    private readonly Dictionary<string,Bitmap> coverImages=new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<Game> allGames=[];
    private string view="Alle Spiele";
    private CancellationTokenSource? operation;
    private PackageCatalog? catalog;
    private readonly Dictionary<string,Process> running=new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> runningEmulators=[];
    private readonly List<Button> operationButtons=[];
    private readonly Dictionary<string,Button> viewButtons=[];
    private readonly DispatcherTimer gamepadTimer=new(){Interval=TimeSpan.FromMilliseconds(100)};
    private ushort previousButtons;
    private readonly TextBlock controllerStatus=new(){TextWrapping=TextWrapping.Wrap,FontSize=12};
    private long repeatAt;
    private ushort previousDirection;
    private int controllerIndex=-1;
    private bool dialogOpen;
    public MainWindow():this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EmuVora")){ }
    public MainWindow(string directory)
    {
        dataDir=directory;Directory.CreateDirectory(dataDir);
        settingsStore=new(Path.Combine(dataDir,"settings.json"));settings=settingsStore.Load();store=new(Path.Combine(dataDir,"library.db"));allGames=store.GetAll();
        Title="EmuVora";Width=1320;Height=820;MinWidth=1000;MinHeight=640;
        var layout=new Grid {ColumnDefinitions=new("220,*,270")};
        var sidebar=new StackPanel {Spacing=10,Margin=new(18)};
        sidebar.Children.Add(new TextBlock {Text="◈ EmuVora",FontSize=27,FontWeight=FontWeight.Bold,Margin=new(0,4,0,20)});
        sidebar.Children.Add(new TextBlock {Text="DEINE BIBLIOTHEK",FontSize=11,Opacity=0.65});
        foreach(var name in new[]{"Alle Spiele","Favoriten","Zuletzt gespielt","Konsole zuordnen","Fehlende Dateien"})
        {
            var item=ActionButton(name,()=>{view=name;Refresh();});viewButtons[name]=item;sidebar.Children.Add(item);
        }
        sidebar.Children.Add(new Separator {Margin=new(0,12)});
        var import=AsyncButton("+ ROM-Ordner importieren",()=>ImportAsync());sidebar.Children.Add(import);operationButtons.Add(import);
        var rescan=AsyncButton("Ordner neu scannen",()=>RescanAsync());sidebar.Children.Add(rescan);operationButtons.Add(rescan);
        sidebar.Children.Add(AsyncButton("Emulatoren einrichten",()=>ShowSettingsAsync()));
        sidebar.Children.Add(AsyncButton("Bibliothek sichern",()=>ExportAsync()));
        sidebar.Children.Add(AsyncButton("Sicherung wiederherstellen",()=>RestoreAsync()));
        sidebar.Children.Add(ActionButton("Hilfe",()=>ShowHelp()));
        sidebar.Children.Add(controllerStatus);
        sidebar.Children.Add(new TextBlock {Text="Windows · v0.3\nDeine Spiele. Deine Bibliothek.",FontSize=12,Opacity=0.6,Margin=new(0,24,0,0)});
        sideBorder.Child=new ScrollViewer {Content=sidebar};layout.Children.Add(sideBorder);
        var header=new Grid {ColumnDefinitions=new("*,Auto")};
        header.Children.Add(new TextBlock {Text="Deine Spiele",FontSize=32,FontWeight=FontWeight.Bold});Grid.SetColumn(summary,1);header.Children.Add(summary);main.Children.Add(header);
        var filters=new Grid {ColumnDefinitions=new("*,Auto,Auto"),ColumnSpacing=10};filters.Children.Add(search);Grid.SetColumn(systems,1);filters.Children.Add(systems);Grid.SetColumn(sort,2);filters.Children.Add(sort);AddRow(main,filters,1);
        gamesList.ItemsPanel=new FuncTemplate<Panel?>(()=>new WrapPanel());gamesList.ItemTemplate=new FuncDataTemplate<Game>((game,_)=>CreateCard(game));
        ScrollViewer.SetHorizontalScrollBarVisibility(gamesList,Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled);
        var library=new Grid();library.Children.Add(gamesList);library.Children.Add(empty);AddRow(main,library,2);
        AddRow(main,progress,3);var footer=new Grid {ColumnDefinitions=new("*,Auto"),ColumnSpacing=12};footer.Children.Add(status);Grid.SetColumn(cancel,1);footer.Children.Add(cancel);AddRow(main,footer,4);
        Grid.SetColumn(main,1);layout.Children.Add(main);
        var details=new StackPanel {Spacing=14,Margin=new(18)};details.Children.Add(detailCover);details.Children.Add(detailTitle);details.Children.Add(detailInfo);
        foreach(var button in new[]{play,favorite,cover,onlineCover,edit,reveal,remove})details.Children.Add(button);
        detailBorder.Child=new ScrollViewer {Content=details};Grid.SetColumn(detailBorder,2);layout.Children.Add(detailBorder);Content=layout;
        play.Click+=async(_,_)=>await PlayAsync();favorite.Click+=(_,_)=>Guard(()=>{if(Selected is{} game){store.Update(game with {Favorite=!game.Favorite});Reload(game.Path);}});
        cover.Click+=async(_,_)=>await ChooseCoverAsync();edit.Click+=async(_,_)=>await EditAsync();remove.Click+=async(_,_)=>await RemoveAsync();reveal.Click+=(_,_)=>Guard(()=>{if(Selected is{} game)OpenPath(Path.GetDirectoryName(game.Path)!);});
        onlineCover.Click+=async(_,_)=>await FetchCoverAsync();ToolTip.SetTip(onlineCover,"Sucht den Spieltitel bei libretro-thumbnails. Titel und Konsole werden dafür an GitHub gesendet.");
        gamesList.SelectionChanged+=(_,_)=>UpdateSelected();gamesList.DoubleTapped+=async(_,_)=>await PlayAsync();
        gamesList.KeyDown+=async(_,e)=>{if(e.Key==Key.Enter){e.Handled=true;await PlayAsync();}};
        search.TextChanged+=(_,_)=>Refresh();systems.SelectionChanged+=(_,_)=>Refresh();sort.SelectionChanged+=(_,_)=>Refresh();cancel.Click+=(_,_)=>operation?.Cancel();
        KeyDown+=(_,e)=>{if(e.Key==Key.F&&e.KeyModifiers.HasFlag(KeyModifiers.Control)){search.Focus();e.Handled=true;}};
        UpdateSystems();ApplyTheme();Refresh();
        status.Text=settingsStore.LoadWarning??"ROM-Ordner importieren oder Emulatoren einrichten, um zu starten.";
        Opened+=async(_,_)=>{if(settings.CheckUpdatesOnStartup)await CheckUpdatesAsync();};
        gamepadTimer.Tick+=async(_,_)=>await HandleGamepadAsync();gamepadTimer.Start();
        Closed+=(_,_)=>{gamepadTimer.Stop();operation?.Cancel();foreach(var bitmap in coverImages.Values)bitmap.Dispose();foreach(var process in running.Values)process.Dispose();packages.Dispose();};
    }
    private static void AddRow(Grid grid,Control control,int row){Grid.SetRow(control,row);grid.Children.Add(control);}
    private Button ActionButton(string text,Action action)
    {var button=new Button {Content=text,HorizontalAlignment=HorizontalAlignment.Stretch};button.Click+=(_,_)=>Guard(action);return button;}
    private Button AsyncButton(string text,Func<Task> action)
    {var button=new Button {Content=text,HorizontalAlignment=HorizontalAlignment.Stretch};button.Click+=async(_,_)=>{try{await action();}catch(Exception ex){ShowError(ex);}};return button;}
    private void Guard(Action action){try{action();}catch(Exception ex){ShowError(ex);}}
    private void ShowError(Exception ex){status.Text="Fehler: "+ex.Message;Log(ex);}
    private void Log(Exception ex){try{File.AppendAllText(Path.Combine(dataDir,"errors.log"),$"{DateTimeOffset.Now:O} {ex}\n");}catch{ }}
    private Game? Selected=>gamesList.SelectedItem as Game;
    private void ApplyTheme()
    {
        var palette=settings.Theme switch
        {"Classic Light"=>("#F3F5F9","#172235","#E6EAF2","#315DBD"),"Retro Green"=>("#101A13","#D9FFE1","#18291D","#69E991"),"Neon Purple"=>("#1A1026","#F5E6FF","#291939","#CC8CFF"),_=>("#101722","#EAF0FA","#1A2433","#6AA8FF")};
        RequestedThemeVariant=settings.Theme=="Classic Light"?ThemeVariant.Light:ThemeVariant.Dark;
        Background=Brush.Parse(palette.Item1);Foreground=Brush.Parse(palette.Item2);sideBorder.Background=detailBorder.Background=Brush.Parse(palette.Item3);status.Foreground=Brush.Parse(palette.Item4);
        gamesList.Background=Brushes.Transparent;
    }
    private void UpdateSystems()
    {
        var previous=systems.SelectedItem as string;var choices=new[]{"Alle Konsolen"}.Concat(allGames.Select(g=>g.System).Distinct().Order()).ToArray();
        systems.ItemsSource=choices;systems.SelectedItem=previous!=null&&choices.Contains(previous)?previous:"Alle Konsolen";
    }
    private void Reload(string? selected=null)
    {allGames=store.GetAll();UpdateSystems();Refresh(selected);}
    private void Refresh(string? selectedPath=null)
    {
        selectedPath??=Selected?.Path;var query=search.Text?.Trim()??"";var console=systems.SelectedItem as string;
        var filtered=allGames.Where(g=>(console is null or "Alle Konsolen"||g.System==console)&&(g.Title.Contains(query,StringComparison.OrdinalIgnoreCase)||g.System.Contains(query,StringComparison.OrdinalIgnoreCase)));
        filtered=view switch {"Favoriten"=>filtered.Where(g=>g.Favorite),"Zuletzt gespielt"=>filtered.Where(g=>g.LastPlayed!=null),"Konsole zuordnen"=>filtered.Where(g=>!EmulatorCatalog.Systems.Contains(g.System)),"Fehlende Dateien"=>filtered.Where(g=>!File.Exists(g.Path)),_=>filtered};
        var result=(sort.SelectedIndex==2||view=="Zuletzt gespielt"?filtered.OrderByDescending(g=>g.LastPlayed):sort.SelectedIndex==1?filtered.OrderBy(g=>g.System).ThenBy(g=>g.Title):filtered.OrderBy(g=>g.Title)).ToArray();
        gamesList.ItemsSource=result;gamesList.SelectedItem=result.FirstOrDefault(g=>g.Path.Equals(selectedPath,StringComparison.OrdinalIgnoreCase));
        empty.IsVisible=result.Length==0;empty.Text=allGames.Count==0?"Deine Bibliothek ist leer.\nImportiere einen Ordner mit deinen ROMs.":"Hier sind keine Spiele.\nPasse Suche, Konsole oder Bibliotheksansicht an.";
        summary.Text=$"{view}\n{result.Length} von {allGames.Count} Spielen";UpdateSelected();
        foreach(var(name,button)in viewButtons)button.FontWeight=name==view?FontWeight.Bold:FontWeight.Normal;
    }
    private Control Artwork(Game game,int width)
    {
        if(settings.Covers.TryGetValue(game.Path,out var file)&&File.Exists(file))
        {
            try
            {
                if(!coverImages.TryGetValue(file,out var image)){using var stream=File.OpenRead(file);image=Bitmap.DecodeToWidth(stream,480);coverImages[file]=image;}
                return new Image {Source=image,Stretch=Stretch.Uniform};
            }
            catch(Exception ex) when(ex is IOException or ArgumentException or InvalidOperationException){Log(ex);}
        }
        return new Border {Background=Brush.Parse(settings.Theme=="Classic Light"?"#D7DFED":"#25334A"),CornerRadius=new(10),Child=new TextBlock {Text="◈\n"+game.System,FontSize=24,TextAlignment=TextAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center}};
    }
    private Control CreateCard(Game game)
    {
        var card=new StackPanel {Width=148,Spacing=7,Margin=new(5)};
        card.Children.Add(new Border {Height=188,Child=Artwork(game,148)});
        card.Children.Add(new TextBlock {Text=(game.Favorite?"★ ":"")+game.Title,TextWrapping=TextWrapping.Wrap,MaxHeight=44,FontWeight=FontWeight.SemiBold});
        card.Children.Add(new TextBlock {Text=game.System+(File.Exists(game.Path)?"":" · Datei fehlt"),FontSize=12,Opacity=0.7});ToolTip.SetTip(card,game.Title+"\n"+game.Path);return card;
    }
    private void UpdateSelected()
    {
        var game=Selected;foreach(var button in new[]{favorite,edit,cover,onlineCover,remove,reveal})button.IsEnabled=game!=null&&operation==null;
        play.IsEnabled=game!=null&&operation==null&&!running.ContainsKey(game.Path);
        detailTitle.Text=game?.Title??"Spiel auswählen";detailCover.Content=game==null?null:Artwork(game,220);
        favorite.Content=game?.Favorite==true?"★ Favorit entfernen":"☆ Als Favorit merken";
        detailInfo.Text=game==null?"Wähle ein Spiel, um zu starten oder Details zu bearbeiten.":$"{game.System}\n{game.PlayCount} Starts · {(game.LastPlayed is{} last?last.ToLocalTime().ToString("g"):"Noch nicht gespielt")}\n\n{game.Path}";
    }
    private async Task<bool> BusyAsync(Func<CancellationToken,Task> action)
    {
        if(operation!=null){status.Text="Bitte den aktuellen Vorgang abschließen oder abbrechen.";return false;}
        operation=new();foreach(var button in operationButtons)button.IsEnabled=false;cancel.IsVisible=progress.IsVisible=true;progress.IsIndeterminate=true;UpdateSelected();
        try{await action(operation.Token);return true;}catch(OperationCanceledException){status.Text="Vorgang abgebrochen.";return false;}catch(Exception ex){ShowError(ex);return false;}
        finally{operation.Dispose();operation=null;foreach(var button in operationButtons)button.IsEnabled=true;cancel.IsVisible=progress.IsVisible=false;UpdateSelected();}
    }
    private IProgress<DownloadProgress> DownloadProgress()=>new Progress<DownloadProgress>(p=>{status.Text=p.Message;progress.IsIndeterminate=p.Fraction==null;if(p.Fraction is{} value)progress.Value=value*100;});
    private async Task ImportAsync()
    {
        var folders=await StorageProvider.OpenFolderPickerAsync(new(){Title="ROM-Ordner wählen",AllowMultiple=true});if(folders.Count==0)return;
        var paths=folders.Select(f=>f.TryGetLocalPath()).Where(p=>p!=null).Cast<string>().ToArray();await ScanAsync(paths,true);
    }
    private async Task RescanAsync()
    {if(settings.RomFolders.Count==0){status.Text="Zuerst einen ROM-Ordner importieren.";return;}await ScanAsync(settings.RomFolders.ToArray(),false);}
    private Task ScanAsync(string[] folders,bool remember)=>BusyAsync(async token=>
    {
        var results=await Task.Run(()=>folders.Select(path=>{token.ThrowIfCancellationRequested();try{return RomScanner.ScanDetailed(path,token);}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){return new ScanResult([],[$"{path}: {ex.Message}"]);}}).ToArray(),token);
        var before=allGames.Count;await Task.Run(()=>store.Upsert(results.SelectMany(r=>r.Games)),token);
        if(remember){foreach(var path in folders)if(!settings.RomFolders.Contains(path,StringComparer.OrdinalIgnoreCase))settings.RomFolders.Add(path);settingsStore.Save(settings);}
        Reload();status.Text=$"{allGames.Count-before} neue Spiele · {results.Sum(r=>r.Warnings.Count)} übersprungene oder nicht lesbare Einträge. Unklare Discs unter „Konsole zuordnen“ bearbeiten.";
        foreach(var warning in results.SelectMany(r=>r.Warnings))Log(new IOException(warning));
    });
    private async Task EditAsync()
    {
        if(Selected is not{} game)return;var title=new TextBox {Text=game.Title};var console=new ComboBox {ItemsSource=EmulatorCatalog.Systems,SelectedItem=game.System};
        var panel=new StackPanel {Spacing=12,Margin=new(24)};panel.Children.Add(new TextBlock {Text="Spieldetails",FontSize=24});panel.Children.Add(new TextBlock {Text="Titel"});panel.Children.Add(title);panel.Children.Add(new TextBlock {Text="Konsole"});panel.Children.Add(console);
        var dialog=Dialog("Spieldetails",panel);panel.Children.Add(ActionButton("Speichern",()=>{if(string.IsNullOrWhiteSpace(title.Text)||console.SelectedItem is not string system){status.Text="Titel und Konsole wählen.";return;}store.Update(game with {Title=title.Text.Trim(),System=system});Reload(game.Path);dialog.Close();}));await dialog.ShowDialog(this);
    }
    private Window Dialog(string title,Control content)
    {
        var dialog=new Window {Title=title,Width=640,Height=640,MinWidth=520,MinHeight=220,WindowStartupLocation=WindowStartupLocation.CenterOwner,Content=new ScrollViewer {Content=content},RequestedThemeVariant=RequestedThemeVariant};
        dialog.Opened+=(_,_)=>dialogOpen=true;dialog.Closed+=(_,_)=>dialogOpen=false;return dialog;
    }
    private async Task<bool> ConfirmAsync(string message)
    {
        var panel=new StackPanel {Spacing=20,Margin=new(24)};panel.Children.Add(new TextBlock {Text=message,TextWrapping=TextWrapping.Wrap});var dialog=Dialog("Bestätigen",panel);dialog.Height=260;
        panel.Children.Add(ActionButton("Bestätigen",()=>dialog.Close(true)));panel.Children.Add(ActionButton("Abbrechen",()=>dialog.Close(false)));return await dialog.ShowDialog<bool>(this);
    }
    private async Task RemoveAsync()
    {if(Selected is{} game&&await ConfirmAsync($"„{game.Title}“ aus der Bibliothek entfernen? Deine ROM-Datei bleibt erhalten.")){Guard(()=>{store.Remove(game);Reload();});}}
    private async Task ChooseCoverAsync()
    {
        if(Selected is not{} game)return;
        var files=await StorageProvider.OpenFilePickerAsync(new(){Title="Cover auswählen",AllowMultiple=false,FileTypeFilter=[new("Bilder"){Patterns=["*.png","*.jpg","*.jpeg","*.webp","*.bmp"]}]});if(files.Count==0)return;
        Guard(()=>
        {
            var source=files[0].TryGetLocalPath()??throw new InvalidOperationException("Bitte eine lokale Bilddatei auswählen.");
            using(var stream=File.OpenRead(source))using(var image=Bitmap.DecodeToWidth(stream,480)){ }
            var folder=Path.Combine(dataDir,"covers");Directory.CreateDirectory(folder);var path=Path.Combine(folder,Guid.NewGuid().ToString("N")+Path.GetExtension(source));File.Copy(source,path);
            settings.Covers[game.Path]=path;settingsStore.Save(settings);Refresh(game.Path);status.Text="Cover gespeichert.";
        });
    }
    private async Task PlayAsync()
    {
        if(Selected is not{} game||operation!=null||running.ContainsKey(game.Path))return;
        try
        {
            var id=settings.PreferredEmulators.GetValueOrDefault(game.System);var definition=EmulatorCatalog.Known.FirstOrDefault(e=>e.Id==id&&e.Systems.Contains(game.System))??EmulatorCatalog.Known.FirstOrDefault(e=>e.Systems.Contains(game.System));
            if(definition==null){status.Text="Zuerst die Konsole des Spiels zuordnen.";return;}
            if(runningEmulators.Contains(definition.Id)){status.Text="Das laufende Spiel zuerst im Emulator beenden.";return;}
            if(settings.AutomaticSetup&&!await BusyAsync(async token=>await EnsureReadyAsync(definition.Id,game.System,token)))return;
            var executable=settings.EmulatorPaths.GetValueOrDefault(definition.Id);
            if(string.IsNullOrEmpty(executable)||!File.Exists(executable)){status.Text=$"{definition.Name} zuerst unter „Emulatoren einrichten“ installieren oder auswählen.";await ShowSettingsAsync(definition.Id);return;}
            if(settings.Packages.TryGetValue(definition.Id,out var managed)&&managed.ExecutablePath==executable)
            {
                var hint=EmulatorSetup.FirmwareHint(game.System,dataDir,EmulatorSetup.ConfiguredFirmwareDirectory(definition.Id,executable,dataDir));
                if(hint!=null){status.Text=hint;await ShowSettingsAsync(definition.Id);return;}
            }
            var process=new ExternalEmulatorAdapter(definition).Launch(game,executable,settings.CorePaths.GetValueOrDefault(game.System),settings.Fullscreen);running[game.Path]=process;runningEmulators.Add(definition.Id);
            store.MarkPlayed(game);Reload(game.Path);status.Text=$"{game.Title} läuft mit {definition.Name}.";
            _=TrackProcessAsync(process,game,definition.Id);
        }
        catch(Exception ex){ShowError(ex);}
    }
    private Task FetchCoverAsync()
    {
        if(Selected is not{} game)return Task.CompletedTask;
        return BusyAsync(async token=>
        {
            status.Text="Cover bei libretro-thumbnails suchen …";var file=await CoverLookup.DownloadAsync(game,Path.Combine(dataDir,"covers"),token);
            if(file==null){status.Text="Kein exakter Cover-Treffer. No-Intro/Redump-Titel mit Region verwenden oder eigenes Cover wählen.";return;}
            try{using var stream=File.OpenRead(file);using var image=Bitmap.DecodeToWidth(stream,480);}
            catch{File.Delete(file);throw;}
            settings.Covers[game.Path]=file;settingsStore.Save(settings);Refresh(game.Path);status.Text="Cover gespeichert · Quelle: libretro-thumbnails.";
        });
    }
    private async Task TrackProcessAsync(Process process,Game game,string emulator)
    {
        try
        {
            await process.WaitForExitAsync();var code=process.ExitCode;
            Dispatcher.UIThread.Post(()=>{running.Remove(game.Path);if(!running.Values.Any(p=>!p.HasExited))runningEmulators.Clear();else runningEmulators.Remove(emulator);process.Dispose();UpdateSelected();status.Text=code==0?$"{game.Title} beendet.":$"Emulator beendet (Code {code}). BIOS, Grafiktreiber und Emulator-Einstellungen prüfen.";});
        }
        catch(Exception ex){Log(ex);}
    }
    private static void OpenPath(string path)=>Process.Start(new ProcessStartInfo(path){UseShellExecute=true});
    private async Task ExportAsync()
    {
        var file=await StorageProvider.SaveFilePickerAsync(new(){Title="Bibliothek sichern",SuggestedFileName="EmuVora-Bibliothek.json",DefaultExtension="json"});if(file==null)return;
        Guard(()=>{var path=file.TryGetLocalPath()??throw new InvalidOperationException("Lokale Datei wählen.");AtomicFile.WriteText(path,JsonSerializer.Serialize(allGames,SettingsStore.JsonOptions));status.Text="Bibliothek gesichert. Cover und Emulator-Einstellungen bleiben im EmuVora-Datenordner.";});
    }
    private async Task RestoreAsync()
    {
        var files=await StorageProvider.OpenFilePickerAsync(new(){Title="Bibliothek wiederherstellen",AllowMultiple=false,FileTypeFilter=[new("Bibliothek"){Patterns=["*.json"]}]});if(files.Count==0)return;
        if(await ConfirmAsync("Bibliothek aus der Sicherung ergänzen? Vorhandene Einträge mit gleichem Dateipfad werden ersetzt."))Guard(()=>{var games=JsonSerializer.Deserialize<Game[]>(File.ReadAllText(files[0].TryGetLocalPath()!),SettingsStore.JsonOptions)??throw new InvalidDataException("Sicherung ist leer.");store.Restore(games);Reload();status.Text="Bibliothek wiederhergestellt.";});
    }
    private async Task HandleGamepadAsync()
    {
        var pads=Gamepad.Read();controllerStatus.Text=pads.Length==0?"Xbox-Controller: nicht verbunden":"Xbox-Controller: "+pads.Length+" verbunden\nStick / D-Pad: wählen · A: spielen\nY: Favorit · LB/RB: System";
        var active=pads.FirstOrDefault(p=>Gamepad.NavigationButtons(p)!=0);if(active==default&&pads.Length>0)active=pads[0];
        if(active.Index!=controllerIndex){previousButtons=0;previousDirection=0;controllerIndex=active.Index;}
        var buttons=Gamepad.NavigationButtons(active);var pressed=buttons&~previousButtons;previousButtons=buttons;
        var direction=(ushort)(buttons&15);var now=Environment.TickCount64;
        if(direction!=previousDirection){previousDirection=direction;repeatAt=now+400;}
        else if(direction!=0&&now>=repeatAt){pressed|=direction;repeatAt=now+140;}
        if(!IsActive||dialogOpen||operation!=null||running.Count>0||pressed==0)return;
        if((pressed&0x300)!=0&&systems.ItemCount>0)systems.SelectedIndex=(systems.SelectedIndex+((pressed&0x200)!=0?1:systems.ItemCount-1))%systems.ItemCount;
        if((pressed&0x2000)!=0){view="Alle Spiele";search.Text="";systems.SelectedIndex=0;Refresh();}
        var count=gamesList.ItemCount;if(count==0)return;var index=Math.Max(0,gamesList.SelectedIndex);var columns=Math.Max(1,(int)(gamesList.Bounds.Width/174));
        if((pressed&1)!=0)index-=columns;if((pressed&2)!=0)index+=columns;if((pressed&4)!=0)index--;if((pressed&8)!=0)index++;
        gamesList.SelectedIndex=Math.Clamp(index,0,count-1);gamesList.ScrollIntoView(gamesList.SelectedItem!);
        if((pressed&0x1000)!=0)await PlayAsync();
        if((pressed&0x8000)!=0&&Selected is{} game)Guard(()=>{store.Update(game with {Favorite=!game.Favorite});Reload(game.Path);});
    }
    private async Task InstallManagedAsync(string id,CancellationToken token)
    {
        catalog??=await packages.GetCatalogAsync(token);var package=catalog.Packages.First(p=>p.Id==id);
        if(settings.Packages.TryGetValue(id,out var same)&&same.Version==package.Version&&File.Exists(same.ExecutablePath))
        {
            if(id=="retroarch-cores")ConfigureCores(same.ExecutablePath);
            else{settings.EmulatorPaths[id]=same.ExecutablePath;EmulatorSetup.Configure(id,same.ExecutablePath,dataDir,settings.XboxDefaults,settings.Fullscreen,firstController:Gamepad.Read().FirstOrDefault().Index);}
            return;
        }
        var executable=await packages.InstallAsync(package,Path.Combine(dataDir,"emulators"),DownloadProgress(),token);
        var previous=id=="retroarch-cores"?settings.Packages.GetValueOrDefault(id)?.ExecutablePath:settings.EmulatorPaths.GetValueOrDefault(id);
        if(id=="retroarch-cores")ConfigureCores(executable,previous==null?null:Path.GetDirectoryName(previous));
        else
        {
            if(previous!=null&&File.Exists(previous))await Task.Run(()=>PreserveConfiguration(previous,executable),token);
            EmulatorSetup.Configure(id,executable,dataDir,settings.XboxDefaults,settings.Fullscreen,reset:previous==null,firstController:Gamepad.Read().FirstOrDefault().Index);
            settings.EmulatorPaths[id]=executable;
        }
        settings.Packages[id]=new(package.Version,executable,previous);settingsStore.Save(settings);
    }
    private async Task EnsureReadyAsync(string id,string system,CancellationToken token)
    {
        var executable=settings.EmulatorPaths.GetValueOrDefault(id);
        if(executable==null||!File.Exists(executable))await InstallManagedAsync(id,token);
        if(id=="retroarch"&&(!settings.CorePaths.TryGetValue(system,out var core)||!File.Exists(core)))await InstallManagedAsync("retroarch-cores",token);
        executable=settings.EmulatorPaths.GetValueOrDefault(id);
        if(executable!=null&&settings.Packages.TryGetValue(id,out var managed)&&managed.ExecutablePath==executable)EmulatorSetup.Configure(id,executable,dataDir,settings.XboxDefaults,settings.Fullscreen,firstController:Gamepad.Read().FirstOrDefault().Index);
        settingsStore.Save(settings);
    }
    private async Task CheckUpdatesAsync()
    {
        try
        {
            catalog=await packages.GetCatalogAsync();var updates=catalog.Packages.Where(p=>settings.Packages.TryGetValue(p.Id,out var current)&&current.Version!=p.Version).ToArray();
            if(updates.Length==0)return;
            if(!settings.AutoInstallUpdates){status.Text=$"{updates.Length} Emulator-/Core-Updates verfügbar. Unter „Emulatoren einrichten“ installieren.";return;}
            await BusyAsync(async token=>
            {
                foreach(var package in updates)
                {
                    if(runningEmulators.Contains(package.Id))continue;var old=settings.Packages[package.Id];
                    var path=await packages.InstallAsync(package,Path.Combine(dataDir,"emulators"),DownloadProgress(),token);
                    if(package.Id!="retroarch-cores"&&File.Exists(old.ExecutablePath))await Task.Run(()=>PreserveConfiguration(old.ExecutablePath,path),token);
                    settings.Packages[package.Id]=new(package.Version,path,old.ExecutablePath);
                    if(package.Id=="retroarch-cores")
                    {ConfigureCores(path,Path.GetDirectoryName(old.ExecutablePath));}
                    else{EmulatorSetup.Configure(package.Id,path,dataDir,settings.XboxDefaults,settings.Fullscreen);settings.EmulatorPaths[package.Id]=path;}
                    settingsStore.Save(settings);
                }
                status.Text="Verfügbare Updates installiert. Vorige Emulator-Versionen bleiben erhalten.";
            });
        }
        catch(Exception ex){Log(ex);}
    }
    private async Task ShowSettingsAsync(string? initialEmulator=null)
    {
        var panel=new StackPanel {Spacing=12,Margin=new(24)};panel.Children.Add(new TextBlock {Text="Emulatoren & Einstellungen",FontSize=25,FontWeight=FontWeight.Bold});
        var theme=new ComboBox {ItemsSource=new[]{"Midnight","Classic Light","Retro Green","Neon Purple"},SelectedItem=settings.Theme};panel.Children.Add(new TextBlock {Text="Darstellung"});panel.Children.Add(theme);
        var fullscreen=new CheckBox {Content="Spiele im Vollbild starten (wenn unterstützt)",IsChecked=settings.Fullscreen};var auto=new CheckBox {Content="Beim Start auf Updates prüfen",IsChecked=settings.CheckUpdatesOnStartup};panel.Children.Add(fullscreen);panel.Children.Add(auto);
        var autoInstall=new CheckBox {Content="Updates für verwaltete Installationen automatisch installieren",IsChecked=settings.AutoInstallUpdates};panel.Children.Add(autoInstall);
        autoInstall.IsCheckedChanged+=(_,_)=>Guard(()=>{settings.AutoInstallUpdates=autoInstall.IsChecked==true;settingsStore.Save(settings);});
        AddAutomaticSetupControls(panel);
        theme.SelectionChanged+=(_,_)=>Guard(()=>{settings.Theme=theme.SelectedItem as string??"Midnight";settingsStore.Save(settings);ApplyTheme();Refresh();});fullscreen.IsCheckedChanged+=(_,_)=>Guard(()=>{settings.Fullscreen=fullscreen.IsChecked==true;settingsStore.Save(settings);});auto.IsCheckedChanged+=(_,_)=>Guard(()=>{settings.CheckUpdatesOnStartup=auto.IsChecked==true;settingsStore.Save(settings);});
        var emulator=new ComboBox {ItemsSource=EmulatorCatalog.Known.Select(e=>e.Name).ToArray(),SelectedIndex=Math.Max(0,Array.FindIndex(EmulatorCatalog.Known,e=>e.Id==initialEmulator))};panel.Children.Add(new Separator());panel.Children.Add(emulator);
        var path=new TextBox {Watermark="Pfad zur Emulator-EXE"};var packageInfo=new TextBlock {TextWrapping=TextWrapping.Wrap};panel.Children.Add(path);panel.Children.Add(packageInfo);
        EmulatorDefinition Current()=>EmulatorCatalog.Known[emulator.SelectedIndex];
        void Load(){path.Text=settings.EmulatorPaths.GetValueOrDefault(Current().Id)??"";packageInfo.Text=settings.Packages.TryGetValue(Current().Id,out var installed)?"Installiert: "+installed.Version:"Noch keine verwaltete Installation. Eigene EXE wählen oder offiziell herunterladen.";}
        emulator.SelectionChanged+=(_,_)=>{if(emulator.SelectedIndex>=0)Load();};Load();
        panel.Children.Add(AsyncButton("Vorhandene EXE auswählen",async()=>{var files=await StorageProvider.OpenFilePickerAsync(new(){Title="Emulator-EXE",AllowMultiple=false,FileTypeFilter=[new("Windows-Programme"){Patterns=["*.exe"]}]});if(files.Count>0)path.Text=files[0].TryGetLocalPath();}));
        panel.Children.Add(ActionButton("EXE-Pfad speichern",()=>{var full=Path.GetFullPath(path.Text?.Trim().Trim('"')??"");new ExternalEmulatorAdapter(Current()).CreateStartInfo(null,full);settings.EmulatorPaths[Current().Id]=full;settingsStore.Save(settings);status.Text="Emulator gespeichert.";}));
        panel.Children.Add(ActionButton("Emulator öffnen · BIOS / Controller einrichten",()=>{using var process=new ExternalEmulatorAdapter(Current()).Launch(null,settings.EmulatorPaths.GetValueOrDefault(Current().Id)??"");}));
        panel.Children.Add(ActionButton("Offizielle Webseite / Lizenz",()=>OpenPath(Current().Homepage)));
        panel.Children.Add(ActionButton("Microsoft Visual C++ Runtime herunterladen",()=>OpenPath("https://aka.ms/vs/17/release/vc_redist.x64.exe")));
        panel.Children.Add(AsyncButton("Offiziell herunterladen / aktualisieren",async()=>
        {
            var id=Current().Id;
            if(runningEmulators.Contains(id)){status.Text="Emulator vor dem Update schließen.";return;}
            await BusyAsync(async token=>
            {
                catalog=await packages.GetCatalogAsync(token);var package=catalog.Packages.First(p=>p.Id==id);
                if(settings.Packages.TryGetValue(id,out var same)&&same.Version==package.Version&&File.Exists(same.ExecutablePath)){status.Text="Bereits aktuell.";return;}
                var executable=await packages.InstallAsync(package,Path.Combine(dataDir,"emulators"),DownloadProgress(),token);
                var previous=settings.EmulatorPaths.GetValueOrDefault(id);
                if(!string.IsNullOrEmpty(previous)&&File.Exists(previous))await Task.Run(()=>PreserveConfiguration(previous,executable),token);
                EmulatorSetup.Configure(id,executable,dataDir,settings.XboxDefaults,settings.Fullscreen,reset:previous==null,firstController:Gamepad.Read().FirstOrDefault().Index);
                settings.Packages[id]=new(package.Version,executable,previous);settings.EmulatorPaths[id]=executable;settingsStore.Save(settings);Load();status.Text=Current().Name+" automatisch eingerichtet. Eigene BIOS-Dateien bei Bedarf importieren.";
            });
        }));
        panel.Children.Add(ActionButton("Zur vorigen Version zurück",()=>{var id=Current().Id;if(!settings.Packages.TryGetValue(id,out var installed)||!File.Exists(installed.PreviousExecutablePath)){status.Text="Keine vorige Installation vorhanden.";return;}settings.EmulatorPaths[id]=installed.PreviousExecutablePath!;settings.Packages[id]=installed with {Version="rollback",ExecutablePath=installed.PreviousExecutablePath!,PreviousExecutablePath=installed.ExecutablePath};settingsStore.Save(settings);Load();status.Text="Vorige Version aktiviert.";}));
        panel.Children.Add(AsyncButton("Xbox-Standardprofil erneut anwenden",async()=>
        {
            var id=Current().Id;if(runningEmulators.Contains(id)){status.Text="Emulator zuerst schließen.";return;}
            if(!settings.Packages.TryGetValue(id,out var installed)||settings.EmulatorPaths.GetValueOrDefault(id)!=installed.ExecutablePath){status.Text="Automatische Profile sind für verwaltete Downloads verfügbar.";return;}
            if(await ConfirmAsync("Xbox-Standardprofil erneut anwenden? Die bisherigen Konfigurationsdateien werden als .bak gesichert."))Guard(()=>{EmulatorSetup.Configure(id,installed.ExecutablePath,dataDir,true,settings.Fullscreen,true,Gamepad.Read().FirstOrDefault().Index);status.Text="Xbox-Profil angewendet.";});
        }));
        var downloadStatus=new TextBlock {TextWrapping=TextWrapping.Wrap};
        void FollowStatus(object? sender,AvaloniaPropertyChangedEventArgs e){if(e.Property==TextBlock.TextProperty)downloadStatus.Text=status.Text;}
        status.PropertyChanged+=FollowStatus;panel.Children.Add(downloadStatus);
        panel.Children.Add(ActionButton("Laufenden Download abbrechen",()=>operation?.Cancel()));
        panel.Children.Add(new Separator());panel.Children.Add(new TextBlock {Text="RetroArch-Core je Konsole",FontWeight=FontWeight.Bold});
        var coreSystem=new ComboBox {ItemsSource=EmulatorCatalog.Get("retroarch").Systems,SelectedIndex=0};var corePath=new TextBox();panel.Children.Add(coreSystem);panel.Children.Add(corePath);
        void LoadCore()=>corePath.Text=settings.CorePaths.GetValueOrDefault((string)coreSystem.SelectedItem!)??"";
        coreSystem.SelectionChanged+=(_,_)=>LoadCore();LoadCore();
        panel.Children.Add(AsyncButton("Core-Datei wählen",async()=>{var files=await StorageProvider.OpenFilePickerAsync(new(){Title="RetroArch-Core",FileTypeFilter=[new("Core"){Patterns=["*.dll"]}]});if(files.Count>0)corePath.Text=files[0].TryGetLocalPath();}));
        panel.Children.Add(ActionButton("Core-Pfad speichern",()=>{if(!File.Exists(corePath.Text)||Path.GetExtension(corePath.Text)!=".dll")throw new InvalidOperationException("Gültige Core-DLL wählen.");settings.CorePaths[(string)coreSystem.SelectedItem!]=corePath.Text!;settingsStore.Save(settings);status.Text="Core gespeichert.";}));
        panel.Children.Add(AsyncButton("Stabiles Core-Paket herunterladen / aktualisieren",async()=>
        {
            var system=(string)coreSystem.SelectedItem!;await BusyAsync(async token=>
            {catalog=await packages.GetCatalogAsync(token);var id="retroarch-cores";var package=catalog.Packages.First(p=>p.Id==id);var path=settings.Packages.TryGetValue(id,out var installed)&&installed.Version==package.Version&&File.Exists(installed.ExecutablePath)?installed.ExecutablePath:await packages.InstallAsync(package,Path.Combine(dataDir,"emulators"),DownloadProgress(),token);ConfigureCores(path);settings.Packages[id]=new(package.Version,path,installed?.ExecutablePath);settingsStore.Save(settings);LoadCore();status.Text="Cores für alle unterstützten RetroArch-Konsolen eingerichtet.";});
        }));
        panel.Children.Add(new TextBlock {Text="Downloads stammen aus offiziellen Projektquellen und werden mit SHA-256 geprüft. Alte Installationen bleiben erhalten. Bei fehlenden VCRUNTIME/MSVCP-DLLs die Microsoft Visual C++ Runtime installieren. BIOS-Dateien werden nicht heruntergeladen. Heimcomputer, DS-Touchscreen und spezielle Controller können zusätzliche Eingaben benötigen.",TextWrapping=TextWrapping.Wrap,FontSize=12});
        panel.Children.Add(ActionButton("Datenordner öffnen",()=>OpenPath(dataDir)));
        panel.Children.Add(AsyncButton("Jetzt auf Updates prüfen",()=>CheckUpdatesAsync()));
        panel.Children.Add(new Separator());panel.Children.Add(new TextBlock {Text="Gespeicherte ROM-Ordner",FontWeight=FontWeight.Bold});
        var folders=new ListBox {ItemsSource=settings.RomFolders.ToArray(),MaxHeight=150};panel.Children.Add(folders);
        panel.Children.Add(ActionButton("Ordner aus dem Neu-Scan entfernen",()=>{if(folders.SelectedItem is string folder){settings.RomFolders.Remove(folder);settingsStore.Save(settings);folders.ItemsSource=settings.RomFolders.ToArray();status.Text="Ordner aus dem Neu-Scan entfernt. Spiele und Dateien bleiben erhalten.";}}));
        var dialog=Dialog("EmuVora einrichten",panel);dialog.Height=760;dialog.Closed+=(_,_)=>status.PropertyChanged-=FollowStatus;dialog.Closing+=(_,e)=>{if(operation!=null){e.Cancel=true;operation.Cancel();status.Text="Vorgang wird abgebrochen. Danach kann das Fenster geschlossen werden.";}};await dialog.ShowDialog(this);
    }
    private void AddAutomaticSetupControls(StackPanel panel)
    {
        var automaticSetup=new CheckBox {Content="Beim Spielen fehlende Emulatoren und Cores automatisch einrichten",IsChecked=settings.AutomaticSetup};panel.Children.Add(automaticSetup);
        automaticSetup.IsCheckedChanged+=(_,_)=>Guard(()=>{settings.AutomaticSetup=automaticSetup.IsChecked==true;settingsStore.Save(settings);});
        var xbox=new CheckBox {Content="Xbox-Standardbelegung bei neuer Einrichtung verwenden",IsChecked=settings.XboxDefaults};panel.Children.Add(xbox);
        xbox.IsCheckedChanged+=(_,_)=>Guard(()=>{settings.XboxDefaults=xbox.IsChecked==true;settingsStore.Save(settings);});
        panel.Children.Add(AsyncButton("Alle Emulatoren + Cores automatisch einrichten",async()=>
        {
            if(running.Count>0){status.Text="Laufende Spiele zuerst schließen.";return;}
            await BusyAsync(async token=>{catalog=await packages.GetCatalogAsync(token);foreach(var id in EmulatorCatalog.Known.Select(e=>e.Id).Append("retroarch-cores"))await InstallManagedAsync(id,token);status.Text="Emulatoren, Cores und Xbox-Standardbelegung eingerichtet. Eigene BIOS-Dateien bei Bedarf importieren.";});
        }));
        panel.Children.Add(new TextBlock {Text="Xbox 360 / One / Series: USB, Bluetooth oder Xbox-Adapter, sobald Windows den Controller als XInput erkennt. Neue Profile belegen Buttons, Sticks, Trigger und unterstütztes Rumble. Eigene Belegungen bleiben bei späteren Starts erhalten. RetroArch / PS1 / PS2: View + Menu beendet das Spiel; View + RB öffnet das Menü. Dolphin: View + Menu beendet; PPSSPP: LT öffnet das Menü. Wii nutzt ein Wiimote/Nunchuk-Grundprofil; Bewegungsspiele können Anpassungen benötigen.",TextWrapping=TextWrapping.Wrap,FontSize=12});
        panel.Children.Add(AsyncButton("Eigene BIOS-/Firmware-Dateien importieren",async()=>{var files=await StorageProvider.OpenFilePickerAsync(new(){Title="Eigene BIOS-/Firmware-Dateien",AllowMultiple=true});Guard(()=>{foreach(var file in files)if(file.TryGetLocalPath() is{} source)EmulatorSetup.ImportFirmware(source,dataDir);status.Text=$"{files.Count} Firmware-Dateien importiert. PS1 / PS2: BIOS im Emulator prüfen und auswählen.";});}));
        panel.Children.Add(AsyncButton("Firmware-Ordner importieren (z. B. MSX)",async()=>
        {
            var folders=await StorageProvider.OpenFolderPickerAsync(new(){Title="Ordner mit eigenen Firmware-Dateien"});if(folders.Count==0||folders[0].TryGetLocalPath() is not{} folder)return;
            if(await BusyAsync(async token=>await Task.Run(()=>EmulatorSetup.ImportFirmwareFolder(folder,dataDir,token),token)))status.Text="Firmware-Ordner importiert.";
        }));
        panel.Children.Add(ActionButton("BIOS-/Firmware-Ordner öffnen",()=>{Directory.CreateDirectory(EmulatorSetup.FirmwareDirectory(dataDir));OpenPath(EmulatorSetup.FirmwareDirectory(dataDir));}));
        panel.Children.Add(new TextBlock {Text="Systemhinweise",FontWeight=FontWeight.Bold});
        var hints=new ComboBox {ItemsSource=SystemCatalog.RetroSystems.Select(s=>s.Name).ToArray(),SelectedIndex=0};var hintText=new TextBlock {TextWrapping=TextWrapping.Wrap};
        void LoadHint(){var system=SystemCatalog.Find((string)hints.SelectedItem!)!;hintText.Text=(EmulatorSetup.FirmwareHint(system.Name,dataDir)??"Firmware-Prüfung: keine fehlende Pflichtdatei erkannt.")+"\n"+system.Note+"\nDateien: "+system.Extensions+" · Ordner: "+system.Aliases.Split('|')[0];}
        hints.SelectionChanged+=(_,_)=>LoadHint();LoadHint();panel.Children.Add(hints);panel.Children.Add(hintText);
    }
    private void ConfigureCores(string executable,string? oldDirectory=null)
    {
        var directory=Path.GetDirectoryName(executable)!;
        foreach(var (system,id) in PackageManager.CoreIds)
        {
            var current=settings.CorePaths.GetValueOrDefault(system);
            if(oldDirectory!=null&&current!=null&&!Path.GetFullPath(current).StartsWith(Path.GetFullPath(oldDirectory)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))continue;
            var file=Directory.EnumerateFiles(directory,id+"_libretro.dll",SearchOption.AllDirectories).FirstOrDefault();
            if(file!=null)settings.CorePaths[system]=file;
        }
    }
    private static void PreserveConfiguration(string previous,string next)
    {
        var source=Path.GetDirectoryName(previous)!;var destination=Path.GetDirectoryName(next)!;
        foreach(var name in new[]{"retroarch.cfg","retroarch-core-options.cfg","retroarch-core-options.cfg.emuvora-defaults.json","portable.ini","portable.txt","settings.ini","retroarch.cfg.emuvora-defaults.json","settings.ini.emuvora-defaults.json",".emuvora-configured"})if(File.Exists(Path.Combine(source,name))&&(File.GetAttributes(Path.Combine(source,name))&FileAttributes.ReparsePoint)==0)File.Copy(Path.Combine(source,name),Path.Combine(destination,name),true);
        foreach(var name in new[]{"memstick","inis","memcards","sstates","bios","system","saves","states","config","autoconfig","playlists","cheats","User"})
        {
            var folder=Path.Combine(source,name);if(!Directory.Exists(folder)||(File.GetAttributes(folder)&FileAttributes.ReparsePoint)!=0)continue;
            foreach(var file in Directory.EnumerateFiles(folder,"*",new EnumerationOptions {RecurseSubdirectories=true,AttributesToSkip=FileAttributes.ReparsePoint}))
            {var target=Path.Combine(destination,name,Path.GetRelativePath(folder,file));Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(file,target,true);}
        }
        var marker=Path.Combine(destination,".emuvora-configured");
        if(!File.Exists(marker))AtomicFile.WriteText(marker,"Existing settings preserved; only missing defaults will be added.");
    }
    private void ShowHelp()
    {
        var text=new TextBlock {TextWrapping=TextWrapping.Wrap,Margin=new(24),Text="EmuVora – Erste Schritte\n\n1. ROM-Ordner importieren. Unterstützte Dateien werden rekursiv aufgenommen. Mehrdisc-M3U-Dateien bündeln ihre Discs.\n\n2. Unter „Konsole zuordnen“ unklare Disc-Dateien auswählen und Konsole bearbeiten.\n\n3. Unter „Emulatoren einrichten“ eine vorhandene EXE auswählen oder ein offizielles Paket herunterladen. Fehlende Emulatoren und Cores werden beim ersten Spielstart automatisch heruntergeladen und eingerichtet.\n\n4. Xbox-Controller anschließen. Eigene BIOS-Dateien bei Bedarf in den Einstellungen importieren. DS, Wii und Heimcomputer können zusätzliche Eingaben benötigen.\n\n5. Spiel wählen und „Spielen“ drücken. Doppelklick / Eingabetaste startet ebenfalls. Strg+F öffnet die Suche.\n\nCover, Favoriten und Startverlauf bleiben gespeichert. Entfernen betrifft nur den Bibliothekseintrag. Erneutes Scannen übernimmt neue Dateien und bewahrt korrigierte Titel und Konsolen.\n\nUpdates werden geprüft und in neuen Ordnern installiert. Bei Problemen lässt sich die vorige Emulator-Version wieder aktivieren. Speicherstände verwaltet der jeweilige Emulator. EmuVora liefert keine ROMs oder BIOS-Dateien.\n\nBibliothek sichern exportiert Titel, Pfade, Konsolen, Favoriten und Startverlauf. Für eine vollständige Sicherung auch den EmuVora-Datenordner kopieren (Einstellungen → Datenordner öffnen)."};
        _=Dialog("Hilfe",text).ShowDialog(this);
    }
}
