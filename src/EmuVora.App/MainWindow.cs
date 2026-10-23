using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.Core;
using System.Diagnostics;
using System.Text.Json;

namespace EmuVora.App;

public sealed partial class MainWindow : Window
{
    private readonly string dataDir;
    private readonly Button backupReminder=new(){Content="Datensicherung fällig",HorizontalAlignment=HorizontalAlignment.Stretch,IsVisible=false};
    private readonly LibraryStore store;
    private readonly SettingsStore settingsStore;
    private AppSettings settings;
    private Process? esDeProcess;
    private readonly HttpClient updateHttp=new(){Timeout=TimeSpan.FromMinutes(10)};
    private AppRelease? appRelease;
    private string? appUpdateFile;
    private readonly TextBlock appUpdateStatus=new(){TextWrapping=TextWrapping.Wrap,Text=""};
    private static Version AppVersion=>typeof(MainWindow).Assembly.GetName().Version??new Version(0,7,0);
    private bool isClosed;
    private readonly PackageManager packages=new();
    private readonly ListBox gamesList=new();
    private readonly TextBox search=new(){Watermark="Spiele suchen …"};
    private readonly ComboBox systems=new(){MinWidth=150};
    private readonly ComboBox sort=new(){ItemsSource=new[]{"Titel A–Z","Konsole","Zuletzt gespielt","Zuletzt hinzugefügt"},SelectedIndex=0,MinWidth=160};
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap};
    private readonly TextBlock summary=new(){FontSize=13,Opacity=0.7};
    private readonly FluentAvalonia.UI.Controls.InfoBar empty=new(){IsOpen=true,IsClosable=false,VerticalAlignment=VerticalAlignment.Center,Margin=new(20)};
    private readonly StackPanel emptyState=new(){Spacing=16,MaxWidth=460,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
    private readonly Button emptyAction=new(){HorizontalAlignment=HorizontalAlignment.Center};
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
    private readonly Button protectCover=new(){Content="Cover schützen",IsEnabled=false};
    private readonly Button manageCover=new(){Content="Cover bearbeiten · X",IsEnabled=false,HorizontalAlignment=HorizontalAlignment.Stretch};
    private readonly Button remove=new(){Content="Aus Bibliothek entfernen",IsEnabled=false};
    private readonly Button reveal=new(){Content="ROM-Ordner öffnen",IsEnabled=false};
    private readonly Border sideBorder=new();
    private readonly Border detailBorder=new();
    private Button? detailsToggle;
    private readonly StackPanel consoleNavigation=new(){Spacing=4};
    private readonly ConsoleLogos consoleLogos=new();
    private readonly TextBlock displayStatus=new(){FontSize=12,Opacity=0.75,TextWrapping=TextWrapping.Wrap};
    private readonly Grid layout=new(){ColumnDefinitions=new("240,*,300")};
    private readonly TextBlock libraryHeading=new(){Text="Deine Spiele",FontSize=34,FontWeight=FontWeight.Bold,LetterSpacing=-0.8,TextWrapping=TextWrapping.Wrap};
    private readonly TextBlock libraryEyebrow=new(){Text="DEINE SAMMLUNG",FontSize=10,LetterSpacing=2,Opacity=0.65};
    private readonly Image themeBackground=new(){Stretch=Stretch.UniformToFill,Opacity=0.25,IsHitTestVisible=false};
    private readonly Image themeLogo=new(){Height=70,MaxWidth=200,Stretch=Stretch.Uniform,IsVisible=false};
    private readonly Dictionary<string,Bitmap> themeImages=new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string,EmulationStationTheme> loadedThemes=new(StringComparer.OrdinalIgnoreCase);
    private string? currentThemeSystem;
    private readonly TextBlock themeStatus=new(){TextWrapping=TextWrapping.Wrap,FontSize=12};
    private readonly Grid main=new(){RowDefinitions=new("Auto,Auto,*,Auto,Auto"),RowSpacing=16,Margin=new(24)};
    private readonly Dictionary<string,Bitmap> coverImages=new(StringComparer.OrdinalIgnoreCase);
    private const int LibraryPageSize=48;
    private int libraryPage;
    private int libraryResultCount;
    private readonly TextBlock pageLabel=new(){VerticalAlignment=VerticalAlignment.Center};
    private readonly Button previousPage=new(){Content="Vorherige Seite"};
    private readonly Button nextPage=new(){Content="Nächste Seite"};
    private readonly StackPanel libraryPager=new(){Spacing=6,Margin=new(0,8,0,0)};
    private IReadOnlyList<Game> allGames=[];
    private string view="Alle Spiele";
    private CancellationTokenSource? operation;
    private PackageCatalog? catalog;
    private readonly Dictionary<string,Process> running=new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> runningEmulators=[];
    private readonly List<Button> operationButtons=[];
    private readonly Dictionary<string,Button> viewButtons=[];
    private readonly DispatcherTimer gamepadTimer=new(){Interval=TimeSpan.FromMilliseconds(100)};
    private readonly GamepadNavigation gamepadNavigation=new();
    private readonly TextBlock controllerStatus=new(){TextWrapping=TextWrapping.Wrap,FontSize=12,Opacity=0.6};
    private bool dialogOpen;
    private bool controllerUiMode;
    public MainWindow():this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EmuVora")){ }
    public MainWindow(string directory,bool setupOnly=false)
    {
        dataDir=directory;Directory.CreateDirectory(dataDir);
        settingsStore=new(Path.Combine(dataDir,"settings.json"));settings=settingsStore.Load();store=new(Path.Combine(dataDir,"library.db"));allGames=store.GetAll();addedTimes=store.GetAddedTimes();
        Title="EmuVora";Width=1320;Height=820;MinWidth=820;MinHeight=520;
        WindowAppearance.Apply(this);
        var sidebar=new StackPanel {Spacing=7,Margin=new(18,16)};
        sidebar.Children.Add(BrandIdentity.Lockup());
        sidebar.Children.Add(new TextBlock {Text="DEINE BIBLIOTHEK",FontSize=11,Opacity=0.65});
        sidebar.Children.Add(backupReminder);backupReminder.Click+=async(_,_)=>await ShowSettingsAsync(initialTab:5);UpdateBackupReminder();
        foreach(var name in new[]{"Alle Spiele","Favoriten","Zuletzt gespielt","Zuletzt hinzugefügt","Fehlende Cover"})
        {
            var item=ActionButton(name,()=>{view=name;collectionChoice.SelectedItem=null;systems.SelectedItem="Alle Konsolen";Refresh();});StyleNavigation(item);viewButtons[name]=item;sidebar.Children.Add(item);
        }
        sidebar.Children.Add(new TextBlock{Text="SAMMLUNGEN",FontSize=11,Opacity=0.65,Margin=new(0,12,0,4)});sidebar.Children.Add(collectionChoice);sidebar.Children.Add(AsyncButton("Sammlungen verwalten",()=>ManageCollectionsAsync()));
        collectionChoice.SelectionChanged+=(_,_)=>{if(!updatingCollections&&collectionChoice.SelectedItem is string){view="Sammlung";Refresh();}};UpdateCollections();
        sidebar.Children.Add(new TextBlock{Text="KONSOLEN",FontSize=11,Opacity=0.65,Margin=new(0,16,0,4)});sidebar.Children.Add(consoleNavigation);
        sidebar.Children.Add(new Separator {Margin=new(0,12)});
        var import=AsyncButton("+ ROM-Ordner importieren",()=>ImportAsync());import.Classes.Add("primary-action");import.Classes.Add("accent");sidebar.Children.Add(import);operationButtons.Add(import);
        var rescan=AsyncButton("Ordner neu scannen",()=>RescanAsync());operationButtons.Add(rescan);
        var covers=AsyncButton("Fehlende Cover laden",()=>FetchAllCoversAsync());sidebar.Children.Add(covers);operationButtons.Add(covers);
        var preferences=AsyncButton("Einstellungen",()=>ShowSettingsAsync());StyleNavigation(preferences);sidebar.Children.Add(preferences);
        var tools=new StackPanel{Spacing=6};foreach(var name in new[]{"Konsole zuordnen","Fehlende Dateien"}){var item=ActionButton(name,()=>{view=name;Refresh();});viewButtons[name]=item;tools.Children.Add(item);}
        tools.Children.Add(rescan);tools.Children.Add(AsyncButton("Einrichtungsassistent",()=>ShowOnboardingAsync()));tools.Children.Add(AsyncButton("AlekFull NX · ES-DE öffnen",()=>OpenEsDeAsync()));
        tools.Children.Add(AsyncButton("Bibliothek sichern",()=>ExportAsync()));tools.Children.Add(AsyncButton("Sicherung wiederherstellen",()=>RestoreAsync()));tools.Children.Add(ActionButton("Hilfe",()=>ShowHelp()));sidebar.Children.Add(new Expander{Header="Verwaltung",Content=tools});
        sidebar.Children.Add(appUpdateStatus);sidebar.Children.Add(controllerStatus);
        sidebar.Children.Add(themeStatus);
        sidebar.Children.Add(new TextBlock {Text="Windows · v"+AppVersion.ToString(3)+"\nDeine Spiele. Deine Bibliothek.",FontSize=12,Opacity=0.6,Margin=new(0,24,0,0)});
        sideBorder.Child=new ScrollViewer {Content=sidebar};layout.Children.Add(sideBorder);
        var header=new Grid {ColumnDefinitions=new("Auto,*,Auto"),ColumnSpacing=16};
        header.Children.Add(themeLogo);var heading=new StackPanel{Spacing=5};heading.Children.Add(libraryEyebrow);heading.Children.Add(libraryHeading);Grid.SetColumn(heading,1);header.Children.Add(heading);summary.VerticalAlignment=VerticalAlignment.Center;summary.TextAlignment=TextAlignment.Right;Grid.SetColumn(summary,2);header.Children.Add(summary);main.Children.Add(header);
        var filters=new Grid {ColumnDefinitions=new("*,Auto,Auto"),RowDefinitions=new("Auto,Auto"),ColumnSpacing=10,RowSpacing=10};Grid.SetColumnSpan(search,3);filters.Children.Add(search);Grid.SetRow(systems,1);filters.Children.Add(systems);Grid.SetRow(sort,1);Grid.SetColumn(sort,1);filters.Children.Add(sort);detailsToggle=ActionButton("Details",()=>{settings.ShowGameDetails=!settings.ShowGameDetails;settingsStore.Save(settings);ApplyLayout();});Grid.SetRow(detailsToggle,1);Grid.SetColumn(detailsToggle,2);filters.Children.Add(detailsToggle);AddRow(main,filters,1);
        gamesList.Classes.Add("game-library");
        filters.RowDefinitions.Add(new RowDefinition(GridLength.Auto));Grid.SetRow(manageCover,2);var gameActions=new Grid{ColumnDefinitions=new("*,*"),ColumnSpacing=10};Grid.SetRow(gameActions,2);Grid.SetColumnSpan(gameActions,3);gameActions.Children.Add(manageCover);Grid.SetColumn(collectionGame,1);gameActions.Children.Add(collectionGame);filters.Children.Add(gameActions);
        collectionGame.Click+=async(_,_)=>{try{await ChooseCollectionsAsync();}catch(Exception ex){ShowError(ex);}};
        manageCover.Click+=async(_,_)=>{try{await ManageCoverAsync();}catch(Exception ex){ShowError(ex);}};
        gamesList.Styles.Add(new Style(s=>s.OfType<ListBoxItem>()){Setters={new Setter(ListBoxItem.PaddingProperty,new Thickness(0)),new Setter(ListBoxItem.MarginProperty,new Thickness(0,0,8,8)),new Setter(ListBoxItem.BorderThicknessProperty,new Thickness(2)),new Setter(ListBoxItem.CornerRadiusProperty,new CornerRadius(16))}});
        gamesList.Styles.Add(new Style(s=>s.OfType<ListBoxItem>().Class(":selected")){Setters={new Setter(ListBoxItem.BorderBrushProperty,new DynamicResourceExtension("EmuAccent"))}});
        gamesList.ItemsPanel=new FuncTemplate<Panel?>(()=>new WrapPanel());gamesList.ItemTemplate=new FuncDataTemplate<Game>((game,_)=>CreateCard(game));
        ScrollViewer.SetHorizontalScrollBarVisibility(gamesList,Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled);
        var library=new Grid{RowDefinitions=new("*,Auto")};library.Children.Add(gamesList);
        var pageButtons=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};pageButtons.Children.Add(previousPage);pageButtons.Children.Add(nextPage);libraryPager.Children.Add(pageLabel);libraryPager.Children.Add(pageButtons);Grid.SetRow(libraryPager,1);library.Children.Add(libraryPager);
        previousPage.Click+=(_,_)=>ChangeLibraryPage(-1);nextPage.Click+=(_,_)=>ChangeLibraryPage(1);emptyState.Children.Add(BrandIdentity.CreateMark(72));emptyState.Children.Add(empty);emptyState.Children.Add(emptyAction);operationButtons.Add(emptyAction);emptyAction.Click+=async(_,_)=>{try{if(allGames.Count==0)await ImportAsync();else{search.Text="";systems.SelectedItem="Alle Konsolen";view="Alle Spiele";Refresh();}}catch(Exception ex){ShowError(ex);}};library.Children.Add(emptyState);AddRow(main,library,2);
        AddRow(main,progress,3);var footer=new Grid {ColumnDefinitions=new("*,Auto"),ColumnSpacing=12};footer.Children.Add(status);Grid.SetColumn(cancel,1);footer.Children.Add(cancel);AddRow(main,footer,4);
        var themedMain=new Grid();themedMain.Children.Add(themeBackground);themedMain.Children.Add(main);Grid.SetColumn(themedMain,1);layout.Children.Add(themedMain);
        var details=new StackPanel {Spacing=16,Margin=new(22,30)};details.Children.Add(new TextBlock{Text="IM FOKUS",FontSize=10,LetterSpacing=2,Opacity=0.6});details.Children.Add(new Border{CornerRadius=new(16),Padding=new Thickness(14),Background=new SolidColorBrush(Color.Parse("#10203030")),Child=detailCover});details.Children.Add(detailTitle);details.Children.Add(detailInfo);
        details.Children.Add(displayStatus);details.Children.Add(play);details.Children.Add(favorite);
        var more=new StackPanel{Spacing=8};foreach(var button in new[]{cover,onlineCover,protectCover,edit,reveal,remove})more.Children.Add(button);details.Children.Add(new Expander{Header="Spieldetails bearbeiten",Content=more});
        detailBorder.Child=new ScrollViewer {Content=details};Grid.SetColumn(detailBorder,2);layout.Children.Add(detailBorder);Content=layout;
        play.Click+=async(_,_)=>await PlayAsync();favorite.Click+=(_,_)=>Guard(()=>{if(Selected is{} game){store.Update(game with {Favorite=!game.Favorite});Reload(game.Path);}});
        cover.Click+=async(_,_)=>await ChooseCoverAsync();edit.Click+=async(_,_)=>await EditAsync();remove.Click+=async(_,_)=>await RemoveAsync();reveal.Click+=(_,_)=>Guard(()=>{if(Selected is{} game)OpenPath(Path.GetDirectoryName(game.Path)!);});
        protectCover.Click+=(_,_)=>Guard(()=>{if(Selected is{} game){if(!settings.ProtectedCovers.Remove(game.Path))settings.ProtectedCovers.Add(game.Path);settingsStore.Save(settings);UpdateSelected();}});
        onlineCover.Click+=async(_,_)=>await FetchCoverAsync();ToolTip.SetTip(onlineCover,"Sucht den Spieltitel bei libretro-thumbnails. Titel und Konsole werden dafür an GitHub gesendet.");
        gamesList.SelectionChanged+=(_,_)=>UpdateSelected();gamesList.DoubleTapped+=async(_,_)=>await PlayAsync();
        gamesList.KeyDown+=async(_,e)=>{if(e.Key==Key.Enter){e.Handled=true;await PlayAsync();}};
        search.TextChanged+=(_,_)=>Refresh();systems.SelectionChanged+=(_,_)=>Refresh();sort.SelectionChanged+=(_,_)=>Refresh();cancel.Click+=(_,_)=>operation?.Cancel();
        KeyDown+=(_,e)=>{if(e.Key==Key.F&&e.KeyModifiers.HasFlag(KeyModifiers.Control)){search.Focus();e.Handled=true;}};
        UpdateSystems();ApplyTheme();ApplyLibraryAppearance();Refresh();
        PropertyChanged+=(_,e)=>{if(e.Property==ClientSizeProperty)ApplyLayout();};
        status.Text=settingsStore.LoadWarning??(allGames.Count==0?"Importiere deine Spiele, um loszulegen.":"Bereit zum Spielen · Strg+F zum Suchen");
        Opened+=async(_,_)=>{ApplyLayout();FitMonitor();if(setupOnly){await ShowSettingsAsync();return;}if(!settings.OnboardingCompleted&&allGames.Count==0&&settings.RomFolders.Count==0)await ShowOnboardingAsync();if(isClosed)return;if(settings.CheckAppUpdatesOnStartup)await CheckAppUpdatesAsync();if(settings.CheckUpdatesOnStartup)await CheckUpdatesAsync();if(settings.AutoDownloadCovers&&allGames.Count>0)await FetchAllCoversAsync();if(settings.StartInEsDe&&allGames.Count>0)await OpenEsDeAsync();};
        gamepadTimer.Tick+=async(_,_)=>await HandleGamepadAsync();gamepadTimer.Start();
        Closed+=(_,_)=>{isClosed=true;gamepadTimer.Stop();operation?.Cancel();consoleLogos.Dispose();foreach(var bitmap in coverImages.Values)bitmap.Dispose();foreach(var bitmap in themeImages.Values)bitmap.Dispose();foreach(var process in running.Values)process.Dispose();packages.Dispose();updateHttp.Dispose();};
    }
    private static void AddRow(Grid grid,Control control,int row){Grid.SetRow(control,row);grid.Children.Add(control);}
    private void ApplyLayout(){if(detailsToggle!=null){detailsToggle.IsEnabled=!settings.TvMode&&(ClientSize.Width==0||ClientSize.Width>=1120);ToolTip.SetTip(detailsToggle,"Die rechte Detailspalte ist im Desktop-Modus ab 1120 Pixel Fensterbreite verfügbar.");}var show=!settings.TvMode&&settings.ShowGameDetails&&(ClientSize.Width==0||ClientSize.Width>=1120);detailBorder.IsVisible=show;layout.ColumnDefinitions[0].Width=new GridLength(settings.TvMode?320:240);layout.ColumnDefinitions[2].Width=new GridLength(show?300:0);}
    private void FitMonitor(){var screen=Screens.ScreenFromWindow(this);if(screen==null)return;Width=Math.Min(Width,Math.Max(MinWidth,screen.WorkingArea.Width/screen.Scaling-32));Height=Math.Min(Height,Math.Max(MinHeight,screen.WorkingArea.Height/screen.Scaling-48));}
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
        {"Classic Light"=>("#F5F7F8","#182C32","#E9EEF0","#087F73"),"Retro Green"=>("#101B17","#EDF7EF","#14231C","#8AE6A8"),"Neon Purple"=>("#191623","#F5F0FF","#211C2E","#C9B1FF"),_=>("#11171D","#F0F4F6","#171F27","#71E1C4")};
        RequestedThemeVariant=settings.Theme=="Classic Light"?ThemeVariant.Light:ThemeVariant.Dark;
        Background=Brush.Parse(palette.Item1);Foreground=Brush.Parse(palette.Item2);sideBorder.Background=detailBorder.Background=Brush.Parse(palette.Item3);status.Opacity=0.7;play.Background=Brush.Parse(palette.Item4);play.Foreground=Brush.Parse(settings.Theme=="Classic Light"?"#FFFFFF":"#102720");play.FontWeight=FontWeight.Bold;play.MinHeight=46;
        Resources["EmuAccent"]=Brush.Parse(palette.Item4);Resources["EmuSelection"]=Brush.Parse(settings.Theme=="Classic Light"?"#D5E9E5":"#273D3D");
        Resources["EmuAccentText"]=play.Foreground;play.Classes.Add("accent");
        foreach(var key in new[]{"AccentButtonBackground","AccentButtonBackgroundPointerOver","AccentButtonBackgroundPressed"})Resources[key]=Brush.Parse(palette.Item4);
        foreach(var key in new[]{"AccentButtonForeground","AccentButtonForegroundPointerOver","AccentButtonForegroundPressed"})Resources[key]=play.Foreground;
        Resources["EmuSurface"]=Brush.Parse(palette.Item3);Resources["EmuText"]=Foreground;
        sideBorder.BorderThickness=new(0,0,1,0);detailBorder.BorderThickness=new(1,0,0,0);sideBorder.BorderBrush=detailBorder.BorderBrush=Brush.Parse(settings.Theme=="Classic Light"?"#D8E0E2":"#28323A");
        gamesList.Background=Brushes.Transparent;
        gamesList.Foreground=Foreground;detailTitle.Foreground=Foreground;detailInfo.Foreground=Foreground;
        currentThemeSystem=null;ApplyExternalTheme();
    }
    private void ApplyExternalTheme()
    {
        var system=systems.SelectedItem as string; if(system is null or "Alle Konsolen")system=Selected?.System??allGames.FirstOrDefault()?.System??"NES";
        if(currentThemeSystem==system)return;currentThemeSystem=system;
        themeBackground.Source=null;themeLogo.Source=null;themeLogo.IsVisible=false;themeStatus.Text="";
        gamesList.Foreground=Foreground;detailTitle.Foreground=Foreground;detailInfo.Foreground=Foreground;detailTitle.FontSize=24;detailInfo.FontSize=13;
        if(settings.EmulationStationTheme==null)return;
        try
        {
            if(!loadedThemes.TryGetValue(system,out var external)){external=EmulationStationTheme.Load(settings.EmulationStationTheme,system);loadedThemes[system]=external;}
            Bitmap? ImageFor(ThemeElement? element)
            {
                var path=element?.Properties.GetValueOrDefault("path");if(string.IsNullOrEmpty(path)||!File.Exists(path))return null;
                if(!themeImages.TryGetValue(path,out var bitmap)){bitmap=ThemeArtwork.Load(path);themeImages[path]=bitmap;}return bitmap;
            }
            IBrush? ColorFor(ThemeElement? element,string key)
            {
                var value=element?.Properties.GetValueOrDefault(key)?.TrimStart('#');if(value==null)return null;
                if(value.Length==8)value=value[6..]+value[..6];
                return (value.Length is 6 or 8)&&Color.TryParse("#"+value,out var color)?new SolidColorBrush(color):null;
            }
            ThemeElement? Find(string name)=>external.Elements.LastOrDefault(e=>e.Name==name);
            themeBackground.Source=ImageFor(Find("background"));themeLogo.Source=ImageFor(Find("logo"));themeLogo.IsVisible=themeLogo.Source!=null;
            var gameList=Find("gamelist");gamesList.Foreground=ColorFor(gameList,"primaryColor")??Foreground;
            detailTitle.Foreground=ColorFor(Find("md_name"),"color")??Foreground;detailInfo.Foreground=ColorFor(Find("md_description")??Find("description"),"color")??Foreground;
            themeStatus.Text="ES-Theme · "+system+"\nFarben / Hintergrund / Logo"+(external.Warnings.Count>0?"\n"+string.Join(" ",external.Warnings.Distinct()):"");
        }
        catch(Exception ex){themeStatus.Text="Theme konnte nicht geladen werden: "+ex.Message;Log(ex);}
    }
    private void UpdateSystems()
    {
        var previous=systems.SelectedItem as string;var choices=new[]{"Alle Konsolen"}.Concat(allGames.Select(g=>g.System).Distinct().Order()).ToArray();
        systems.ItemsSource=choices;systems.SelectedItem=previous!=null&&choices.Contains(previous)?previous:"Alle Konsolen";
        consoleNavigation.Children.Clear();foreach(var group in allGames.GroupBy(g=>g.System).OrderBy(g=>g.Key)){var console=group.Key;var item=ActionButton(console,()=>{systems.SelectedItem=console;view="Alle Spiele";Refresh();});item.Tag=console;item.Classes.Add("library-navigation");item.Classes.Add("console-filter");item.HorizontalContentAlignment=HorizontalAlignment.Stretch;item.ContentTemplate=null;item.Content=consoleLogos.CreateContent(console,group.Count());
            Avalonia.Automation.AutomationProperties.SetName(item,console+" - "+group.Count()+" Spiele");Avalonia.Automation.AutomationProperties.SetAutomationId(item,"console-filter-"+Uri.EscapeDataString(console));ToolTip.SetTip(item,console+" - "+group.Count()+" Spiele");consoleNavigation.Children.Add(item);}
    }
    private void Reload(string? selected=null)
    {allGames=store.GetAll();addedTimes=store.GetAddedTimes();UpdateSystems();Refresh(selected);}
    private void Refresh(string? selectedPath=null,bool keepPage=false)
    {
        var explicitSelection=selectedPath!=null;if(!keepPage)libraryPage=0;selectedPath??=Selected?.Path;var query=search.Text?.Trim()??"";var console=systems.SelectedItem as string;
        var filtered=allGames.Where(g=>(console is null or "Alle Konsolen"||g.System==console)&&(g.Title.Contains(query,StringComparison.OrdinalIgnoreCase)||g.System.Contains(query,StringComparison.OrdinalIgnoreCase)));
        filtered=view switch {"Favoriten"=>filtered.Where(g=>g.Favorite),"Zuletzt gespielt"=>filtered.Where(g=>g.LastPlayed!=null),"Konsole zuordnen"=>filtered.Where(g=>!EmulatorCatalog.Systems.Contains(g.System)),"Fehlende Dateien"=>filtered.Where(g=>!File.Exists(g.Path)),"Fehlende Cover"=>filtered.Where(g=>CoverLibrary.IsMissing(settings,g.Path)),"Zuletzt hinzugefügt"=>filtered.Where(g=>addedTimes.ContainsKey(g.Path)),"Sammlung"=>filtered.Where(g=>collectionChoice.SelectedItem is string name&&settings.Collections.TryGetValue(name,out var members)&&members.Contains(g.Path)),_=>filtered};
        var result=(sort.SelectedIndex==3||view=="Zuletzt hinzugefügt"?filtered.OrderByDescending(g=>addedTimes.GetValueOrDefault(g.Path)).ThenBy(g=>g.Title):sort.SelectedIndex==2||view=="Zuletzt gespielt"?filtered.OrderByDescending(g=>g.LastPlayed):sort.SelectedIndex==1?filtered.OrderBy(g=>g.System).ThenBy(g=>g.Title):filtered.OrderBy(g=>g.Title)).ToArray();
        libraryResultCount=result.Length;
        if(explicitSelection&&selectedPath!=null&&!keepPage){var position=Array.FindIndex(result,g=>g.Path.Equals(selectedPath,StringComparison.OrdinalIgnoreCase));if(position>=0)libraryPage=position/LibraryPageSize;}
        libraryPage=Math.Clamp(libraryPage,0,Math.Max(0,(result.Length-1)/LibraryPageSize));
        var page=result.Skip(libraryPage*LibraryPageSize).Take(LibraryPageSize).ToArray();
        foreach(var imageControl in gamesList.GetVisualDescendants().OfType<Image>())imageControl.Source=null;
        gamesList.ItemsSource=null;detailCover.Content=null;
        foreach(var image in coverImages.Values)image.Dispose();coverImages.Clear();
        gamesList.ItemsSource=page;gamesList.SelectedItem=page.FirstOrDefault(g=>g.Path.Equals(selectedPath,StringComparison.OrdinalIgnoreCase));
        libraryPager.IsVisible=result.Length>LibraryPageSize;previousPage.IsEnabled=libraryPage>0;nextPage.IsEnabled=(libraryPage+1)*LibraryPageSize<result.Length;
        pageLabel.Text=$"Seite {libraryPage+1} / {Math.Max(1,(result.Length+LibraryPageSize-1)/LibraryPageSize)} · {libraryPage*LibraryPageSize+1}–{libraryPage*LibraryPageSize+page.Length} von {result.Length}";
        emptyState.IsVisible=empty.IsVisible=result.Length==0;emptyAction.Content=allGames.Count==0?"Ersten ROM-Ordner importieren":"Filter zurücksetzen";empty.Title=allGames.Count==0?"Deine Bibliothek ist leer":"Keine passenden Spiele";empty.Message=allGames.Count==0?"Deine Klassiker bekommen hier ein Zuhause. Importiere einen Ordner mit deinen ROMs.":"Passe Suche, Konsole oder Bibliotheksansicht an.";
        libraryHeading.Text=view=="Sammlung"?collectionChoice.SelectedItem as string??"Sammlung":view=="Alle Spiele"?(console is null or "Alle Konsolen"?"Deine Spiele":console):view;
        if(view=="Fehlende Cover"&&allGames.Count>0){empty.Title="Keine fehlenden Cover in dieser Auswahl";empty.Message="Alle passenden Spiele haben ein Cover. Suche oder Konsole ändern, um andere Einträge zu prüfen.";}
        if(view=="Zuletzt hinzugefügt"&&result.Length==0){empty.Title="Noch keine neuen Importe in dieser Auswahl";empty.Message="Das Importdatum wird ab dieser Version erfasst. Ältere Einträge bleiben unter Alle Spiele erhalten; ihr ursprüngliches Importdatum ist nicht bekannt.";}
        libraryEyebrow.Text=view=="Favoriten"?"DEINE LIEBLINGSSTÜCKE":view=="Zuletzt gespielt"?"WEITERSPIELEN":"DEINE SAMMLUNG";
        var consoleCount=allGames.Select(g=>g.System).Distinct().Count();summary.Text=$"{result.Length} von {allGames.Count} Spielen\n{consoleCount} "+(consoleCount==1?"Konsole":"Konsolen");UpdateSelected();
        ApplyExternalTheme();
        var selectedBrush=Brush.Parse(settings.Theme=="Classic Light"?"#D5E9E5":"#273D3D");foreach(var(name,button)in viewButtons){button.FontWeight=name==view?FontWeight.Bold:FontWeight.Normal;button.Background=name==view?selectedBrush:Brushes.Transparent;}
        foreach(var button in consoleNavigation.Children.OfType<Button>()){var selectedConsole=button.Tag as string==console;button.Background=selectedConsole?selectedBrush:Brushes.Transparent;button.Classes.Set("selected-console",selectedConsole);}
    }
    private void ChangeLibraryPage(int direction)
    {
        var target=libraryPage+direction;
        if(target<0||target*LibraryPageSize>=libraryResultCount)return;
        libraryPage=target;Refresh(keepPage:true);gamesList.SelectedIndex=direction>0?0:gamesList.ItemCount-1;
        if(gamesList.SelectedItem!=null)gamesList.ScrollIntoView(gamesList.SelectedItem);
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
        return MissingArtwork(game);
    }
    private Control CreateCard(Game game)
    {
        var width=CardWidth;var card=new StackPanel {Width=width,Spacing=9,Margin=new(8,8,8,12)};
        card.Children.Add(new Border {Height=width*1.34,Padding=new Thickness(5),CornerRadius=new(12),Background=Brush.Parse(settings.Theme=="Classic Light"?"#E4EAEC":"#1E2932"),Child=Artwork(game,width),ClipToBounds=true});
        card.Children.Add(new TextBlock {Text=(game.Favorite?"★ ":"")+game.Title,TextWrapping=TextWrapping.Wrap,MaxHeight=settings.TvMode?60:44,FontSize=settings.TvMode?18:14,FontWeight=FontWeight.SemiBold});
        card.Children.Add(new TextBlock {Text=game.System+(File.Exists(game.Path)?"":" · Datei fehlt"),FontSize=settings.TvMode?16:12,Opacity=0.7});ToolTip.SetTip(card,game.Title+"\n"+game.Path);return card;
    }
    private void UpdateSelected()
    {
        var game=Selected;foreach(var button in new[]{favorite,edit,cover,onlineCover,protectCover,manageCover,collectionGame,remove,reveal})button.IsEnabled=game!=null&&operation==null;
        protectCover.Content=game!=null&&settings.ProtectedCovers.Contains(game.Path)?"Cover-Schutz aufheben":"Cover schützen";
        play.IsEnabled=game!=null&&operation==null&&!running.ContainsKey(game.Path);
        detailTitle.Text=game?.Title??"Spiel auswählen";detailCover.Content=game==null?null:Artwork(game,220);
        favorite.Content=game?.Favorite==true?"★ Favorit entfernen":"☆ Als Favorit merken";
        detailInfo.Text=game==null?"Wähle ein Spiel, um zu starten oder Details zu bearbeiten.":$"{game.System}\n{game.PlayCount} Starts · {(game.LastPlayed is{} last?last.ToLocalTime().ToString("g"):"Noch nicht gespielt")}";
        ToolTip.SetTip(detailInfo,game?.Path);displayStatus.Text=game==null?"":DisplayProfiles.Description(game.System,DisplayDetection.Current(TryGetPlatformHandle()?.Handle??IntPtr.Zero),settings,game.Path);
        if(game!=null&&settings.ProtectedCovers.Contains(game.Path))detailInfo.Text+="\nCover geschützt"+(CoverLibrary.IsMissing(settings,game.Path)?" · Bilddatei fehlt; eigenes Bild wählen oder Schutz aufheben.":"");
        ApplyExternalTheme();
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
        var folders=await new ControllerPicker(this).OpenFolderPickerAsync(new(){Title="ROM-Ordner wählen",AllowMultiple=true});if(folders.Count==0)return;
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
        if(settings.AutoDownloadCovers)await DownloadMissingCoversAsync(allGames,token);
    });
    private async Task EditAsync()
    {
        if(Selected is not{} game)return;var title=new TextBox {Text=game.Title};var console=new ComboBox {ItemsSource=EmulatorCatalog.Systems,SelectedItem=game.System};
        var panel=new StackPanel {Spacing=12,Margin=new(24)};panel.Children.Add(new TextBlock {Text="Spieldetails",FontSize=24});panel.Children.Add(new TextBlock {Text="Titel"});panel.Children.Add(title);panel.Children.Add(new TextBlock {Text="Konsole"});panel.Children.Add(console);
        panel.Children.Add(new TextBlock{Text="Bildformat für dieses Spiel"});var aspect=new ComboBox{ItemsSource=new[]{"Automatisch · Originalformat","4:3","16:9"},SelectedIndex=settings.GameAspectRatios.GetValueOrDefault(game.Path) switch{"4:3"=>1,"16:9"=>2,_=>0}};panel.Children.Add(aspect);panel.Children.Add(new TextBlock{Text="Bei PS2-Breitbildspielen 16:9 hier und im Spiel wählen. Für andere Spiele bleibt die Formaterkennung des Emulators aktiv. Gilt für verwaltete Downloads mit aktivem Bildprofil.",TextWrapping=TextWrapping.Wrap});
        var dialog=Dialog("Spieldetails",panel);panel.Children.Add(ActionButton("Speichern",()=>{if(string.IsNullOrWhiteSpace(title.Text)||console.SelectedItem is not string system){status.Text="Titel und Konsole wählen.";return;}store.Update(game with {Title=title.Text.Trim(),System=system});if(aspect.SelectedIndex==0)settings.GameAspectRatios.Remove(game.Path);else settings.GameAspectRatios[game.Path]=aspect.SelectedIndex==1?"4:3":"16:9";settingsStore.Save(settings);Reload(game.Path);dialog.Close();}));await dialog.ShowDialog(this);
    }
    private Window Dialog(string title,Control content)
    {
        var dialog=new Window {Title=title,Width=640,Height=640,MinWidth=520,MinHeight=220,WindowStartupLocation=WindowStartupLocation.CenterOwner,Content=new ScrollViewer {Content=content},RequestedThemeVariant=RequestedThemeVariant,Background=Background,Foreground=Foreground};WindowAppearance.Apply(dialog);ControllerUi.Attach(dialog);
        dialog.Opened+=(_,_)=>{var screen=Screens.ScreenFromWindow(this);if(screen!=null){dialog.Width=Math.Min(dialog.Width,Math.Max(dialog.MinWidth,screen.WorkingArea.Width/screen.Scaling-32));dialog.Height=Math.Min(dialog.Height,Math.Max(dialog.MinHeight,screen.WorkingArea.Height/screen.Scaling-48));}};
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
        if(Selected is not{} game||operation!=null)return;
        try
        {
            var files=await new ControllerPicker(this).OpenFilePickerAsync(new(){Title="Cover auswählen",AllowMultiple=false,FileTypeFilter=[new("Bilder"){Patterns=["*.png","*.jpg","*.jpeg","*.webp","*.bmp"]}]});
            if(files.Count==0||isClosed)return;
            var source=files[0].TryGetLocalPath()??throw new IOException("Bitte eine lokale Bilddatei auswählen.");
            await PreviewCoverAsync(game,source,"Eigenes Bild · "+Path.GetFileName(source));
        }
        catch(Exception ex){ShowError(ex);}
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
                DisplayProfiles.Apply(definition.Id,executable,game.System,DisplayDetection.Current(TryGetPlatformHandle()?.Handle??IntPtr.Zero),settings,gamePath:game.Path);
                var hint=EmulatorSetup.FirmwareHint(game.System,dataDir,EmulatorSetup.ConfiguredFirmwareDirectory(definition.Id,executable,dataDir));
                if(hint!=null){status.Text=hint;await ShowSettingsAsync(definition.Id);return;}
            }
            var process=new ExternalEmulatorAdapter(definition).Launch(game,executable,settings.CorePaths.GetValueOrDefault(game.System),settings.Fullscreen);running[game.Path]=process;runningEmulators.Add(definition.Id);
            store.MarkPlayed(game);Reload(game.Path);status.Text=$"{game.Title} läuft mit {definition.Name}.";
            _=TrackProcessAsync(process,game,definition.Id);
        }
        catch(Exception ex){ShowError(ex);}
    }
    private async Task FetchCoverAsync()
    {
        if(Selected is not{} game||operation!=null)return;
        var panel=new StackPanel{Spacing=14,Margin=new(24)};
        panel.Children.Add(new TextBlock{Text="Mit einem abweichenden Titel nach einem Cover suchen. Der gespeicherte Spielname bleibt erhalten. Anschließend den Treffer vor dem Übernehmen prüfen.",TextWrapping=TextWrapping.Wrap});
        var title=new TextBox{Text=game.Title,Watermark="Titel für die Cover-Suche"};panel.Children.Add(title);
        var dialog=Dialog("Cover online suchen",panel);
        panel.Children.Add(ActionButton("Treffer suchen",()=>{if(!string.IsNullOrWhiteSpace(title.Text))dialog.Close(title.Text.Trim());}));
        panel.Children.Add(ActionButton("Abbrechen",()=>dialog.Close()));
        var query=await dialog.ShowDialog<string?>(this);if(query==null||isClosed)return;
        await BusyAsync(async token=>
        {
            var temporary=Path.Combine(dataDir,"cache","cover-preview",Guid.NewGuid().ToString("N"));string? file=null;
            try
            {
                status.Text="Cover bei libretro-thumbnails suchen …";using var http=new HttpClient{Timeout=TimeSpan.FromSeconds(40)};
                var source=new CoverCatalog(http,Path.Combine(dataDir,"cache","cover-index"),settings.CoverRegion);
                file=await source.DownloadAsync(game with{Title=query},temporary,token);
                if(file==null){status.Text="Kein passender Cover-Titel. Anderen Suchnamen oder eigenes Bild wählen.";return;}
                token.ThrowIfCancellationRequested();if(!isClosed)await PreviewCoverAsync(game,file,"libretro-thumbnails · Suchname: "+query);
            }
            finally{if(file!=null&&File.Exists(file))File.Delete(file);if(Directory.Exists(temporary)&&!Directory.EnumerateFileSystemEntries(temporary).Any())Directory.Delete(temporary);}
        });
    }
    private Task FetchAllCoversAsync()=>BusyAsync(token=>DownloadMissingCoversAsync(allGames,token));
    private async Task DownloadMissingCoversAsync(IReadOnlyList<Game> games,CancellationToken token)
    {
        var coverSnapshot=new Dictionary<string,string>(settings.Covers,StringComparer.OrdinalIgnoreCase);
        var coverSettings=new AppSettings{Covers=coverSnapshot,ProtectedCovers=new(settings.ProtectedCovers,StringComparer.OrdinalIgnoreCase)};
        var pending=await Task.Run(()=>games.Where(g=>CoverLibrary.CanDownload(coverSettings,g.Path)).ToArray(),token);if(pending.Length==0){status.Text="Keine ungeschützten Spiele ohne Cover vorhanden.";return;}
        status.Text="Coverkatalog wird geladen …";using var http=new HttpClient{Timeout=TimeSpan.FromSeconds(40)};var source=new CoverCatalog(http,Path.Combine(dataDir,"cache","cover-index"),settings.CoverRegion);
        var complete=false;var updates=new Progress<CoverBatchProgress>(p=>{if(complete||isClosed)return;progress.IsIndeterminate=false;progress.Value=p.Completed*100.0/Math.Max(1,p.Total);status.Text=$"Cover {p.Completed}/{p.Total} · {p.Found} geladen · {p.Title}";});
        try
        {
            var result=await Task.Run(()=>source.DownloadMissingAsync(pending,coverSnapshot,Path.Combine(dataDir,"covers"),(game,file)=>{coverSnapshot[game.Path]=file;},updates,token),token);
            AtomicFile.WriteText(Path.Combine(dataDir,"cover-report.txt"),string.Join(Environment.NewLine,result.Report));
            status.Text=$"Cover fertig · {result.Found} geladen · {result.Missing} ohne Treffer · {result.Failed} Fehler. Details: cover-report.txt im Datenordner.";
        }
        finally{complete=true;foreach(var entry in coverSnapshot)settings.Covers.TryAdd(entry.Key,entry.Value);foreach(var game in pending)if(coverSnapshot.TryGetValue(game.Path,out var file))settings.Covers[game.Path]=file;settingsStore.Save(settings);if(!isClosed)Refresh(keepPage:true);}
    }
    private async Task TrackProcessAsync(Process process,Game game,string emulator)
    {
        try
        {
            await process.WaitForExitAsync();var code=process.ExitCode;
            Dispatcher.UIThread.Post(()=>{running.Remove(game.Path);if(!running.Values.Any(p=>!p.HasExited))runningEmulators.Clear();else runningEmulators.Remove(emulator);process.Dispose();UpdateSelected();if(!isClosed&&running.Count==0&&esDeProcess==null&&!dialogOpen){Show();Activate();gamesList.Focus();}status.Text=code==0?$"{game.Title} beendet.":$"Emulator beendet (Code {code}). BIOS, Grafiktreiber und Emulator-Einstellungen prüfen.";});
        }
        catch(Exception ex){Log(ex);}
    }
    private static void OpenPath(string path)=>Process.Start(new ProcessStartInfo(path){UseShellExecute=true});
    private async Task ExportAsync()
    {
        var file=await new ControllerPicker(this).SaveFilePickerAsync(new(){Title="Bibliothek sichern",SuggestedFileName="EmuVora-Bibliothek.json",DefaultExtension="json"});if(file==null)return;
        Guard(()=>{var path=file.TryGetLocalPath()??throw new InvalidOperationException("Lokale Datei wählen.");AtomicFile.WriteText(path,JsonSerializer.Serialize(allGames,SettingsStore.JsonOptions));status.Text="Bibliothek gesichert. Cover und Emulator-Einstellungen bleiben im EmuVora-Datenordner.";});
    }
    private async Task RestoreAsync()
    {
        var files=await new ControllerPicker(this).OpenFilePickerAsync(new(){Title="Bibliothek wiederherstellen",AllowMultiple=false,FileTypeFilter=[new("Bibliothek"){Patterns=["*.json"]}]});if(files.Count==0)return;
        if(await ConfirmAsync("Bibliothek aus der Sicherung ergänzen? Vorhandene Einträge mit gleichem Dateipfad werden ersetzt."))Guard(()=>{var games=JsonSerializer.Deserialize<Game[]>(File.ReadAllText(files[0].TryGetLocalPath()!),SettingsStore.JsonOptions)??throw new InvalidDataException("Sicherung ist leer.");store.Restore(games);Reload();status.Text="Bibliothek wiederhergestellt.";});
    }
    private async Task HandleGamepadAsync()
    {
        var pads=Gamepad.Read();controllerStatus.Text=pads.Length==0?"Controller: nicht verbunden":"Controller: "+pads.Length+" verbunden · "+Gamepad.Label(pads[0])+"\nD-Pad: wählen · A: spielen · Y: Favorit\nLB/RB: System · Menu: Einstellungen\nView: Bedienelemente · B: Spieleliste";
        ToolTip.SetTip(controllerStatus,Gamepad.BackendName+(Gamepad.BackendWarning is{} warning?" · Rückfall auf XInput: "+warning:" · Automatische Belegung für EmuVora"));
        var pressed=gamepadNavigation.Poll(pads,IsActive&&!dialogOpen&&running.Count==0,Environment.TickCount64);
        if(pressed==0)return;
        await HandleLibraryControllerAsync((ushort)pressed);
    }
    public async Task HandleLibraryControllerAsync(ushort pressed)
    {
        if(operation!=null&&(pressed&0x2000)!=0){operation.Cancel();return;}
        if(running.Count>0||pressed==0)return;
        if((pressed&0x10)!=0){if(operation==null)await ShowSettingsAsync();return;}
        if((pressed&0x20)!=0){controllerUiMode=!controllerUiMode;if(controllerUiMode)ControllerUi.Handle(this,2);else gamesList.Focus();return;}
        if(controllerUiMode){if((pressed&0x2000)!=0){controllerUiMode=false;gamesList.Focus();}else ControllerUi.Handle(this,(ushort)pressed);return;}
        if((pressed&0x300)!=0&&systems.ItemCount>0)systems.SelectedIndex=(systems.SelectedIndex+((pressed&0x200)!=0?1:systems.ItemCount-1))%systems.ItemCount;
        if((pressed&0x2000)!=0){search.Text="";gamesList.Focus();return;}
        var count=gamesList.ItemCount;if(count==0)return;var index=Math.Max(0,gamesList.SelectedIndex);var columns=LibraryColumns();
        if((pressed&1)!=0)index-=columns;if((pressed&2)!=0)index+=columns;if((pressed&4)!=0)index--;if((pressed&8)!=0)index++;
        if(index>=count&&(libraryPage+1)*LibraryPageSize<libraryResultCount)ChangeLibraryPage(1);
        else if(index<0&&libraryPage>0)ChangeLibraryPage(-1);
        else gamesList.SelectedIndex=Math.Clamp(index,0,count-1);gamesList.ScrollIntoView(gamesList.SelectedItem!);
        if(operation==null&&(pressed&0x1000)!=0)await PlayAsync();
        if(operation==null&&(pressed&0x4000)!=0)await ManageCoverAsync();
        if(operation==null&&(pressed&0x8000)!=0&&Selected is{} game)Guard(()=>{store.Update(game with {Favorite=!game.Favorite});Reload(game.Path);});
    }
    private async Task InstallManagedAsync(string id,CancellationToken token)
    {
        catalog??=await packages.GetCatalogAsync(token);var package=catalog.Packages.First(p=>p.Id==id);
        if(settings.Packages.TryGetValue(id,out var same)&&same.Version==package.Version&&File.Exists(same.ExecutablePath))
        {
            if(id=="retroarch-cores")ConfigureCores(same.ExecutablePath);
            else{settings.EmulatorPaths[id]=same.ExecutablePath;EmulatorSetup.Configure(id,same.ExecutablePath,dataDir,settings.XboxDefaults,settings.Fullscreen,firstController:Gamepad.FirstXInputIndex);}
            return;
        }
        var executable=await packages.InstallAsync(package,Path.Combine(dataDir,"emulators"),DownloadProgress(),token);
        var previous=id=="retroarch-cores"?settings.Packages.GetValueOrDefault(id)?.ExecutablePath:settings.EmulatorPaths.GetValueOrDefault(id);
        if(id=="retroarch-cores")ConfigureCores(executable,previous==null?null:Path.GetDirectoryName(previous));
        else
        {
            if(previous!=null&&File.Exists(previous))await Task.Run(()=>PreserveConfiguration(previous,executable),token);
            EmulatorSetup.Configure(id,executable,dataDir,settings.XboxDefaults,settings.Fullscreen,reset:previous==null,firstController:Gamepad.FirstXInputIndex);
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
        if(executable!=null&&settings.Packages.TryGetValue(id,out var managed)&&managed.ExecutablePath==executable)EmulatorSetup.Configure(id,executable,dataDir,settings.XboxDefaults,settings.Fullscreen,firstController:Gamepad.FirstXInputIndex);
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
    private async Task OpenEsDeAsync()
    {
        if(esDeProcess!=null||operation!=null){status.Text="ES-DE oder ein anderer Vorgang läuft bereits.";return;}
        if(running.Count>0){status.Text="Das laufende Spiel zuerst schließen.";return;}
        if(!allGames.Any(g=>EmulatorCatalog.Systems.Contains(g.System))){status.Text="Zuerst mindestens ein Spiel importieren und seine Konsole zuordnen. Danach AlekFull NX öffnen.";return;}
        try
        {
            if(!await BusyAsync(async token=>
            {
                await EsDeFrontend.InstallAsync(dataDir,settings,packages,DownloadProgress(),token);
                EsDeFrontend.ImportChanges(dataDir,store,settings);
                var launcher=Environment.ProcessPath??throw new IOException("EmuVora-Programmdatei wurde nicht gefunden.");
                if(Path.GetFileNameWithoutExtension(launcher).Equals("dotnet",StringComparison.OrdinalIgnoreCase))throw new IOException("Den ES-DE-Modus bitte mit der veröffentlichten EmuVora.App.exe starten.");
                await Task.Run(()=>EsDeFrontend.Export(dataDir,launcher,store.GetAll(),settings),token);settingsStore.Save(settings);
            }))return;
            esDeProcess=Process.Start(EsDeFrontend.CreateStartInfo(settings.EsDeExecutable!,dataDir))??throw new IOException("ES-DE konnte nicht gestartet werden.");
            Hide();await esDeProcess.WaitForExitAsync();
            if(esDeProcess.ExitCode!=0)throw new IOException("ES-DE wurde mit Fehlercode "+esDeProcess.ExitCode+" beendet. Protokoll: "+Path.Combine(EsDeFrontend.AppData(dataDir),"logs","es_log.txt"));
            settings=settingsStore.Load();if(settingsStore.LoadWarning!=null)throw new IOException(settingsStore.LoadWarning);
            EsDeFrontend.ImportChanges(dataDir,store,settings);settingsStore.Save(settings);
            status.Text="Zurück aus ES-DE. Favoriten, Cover und Spielverlauf sind abgeglichen.";
        }
        catch(Exception ex){ShowError(ex);}
        finally
        {
            esDeProcess?.Dispose();esDeProcess=null;
            if(!isClosed)
            {
                var latest=settingsStore.Load();if(settingsStore.LoadWarning==null)settings=latest;
                foreach(var image in coverImages.Values)image.Dispose();coverImages.Clear();Reload();Show();Activate();
            }
        }
    }
    private async Task ShowSettingsAsync(string? initialEmulator=null,int? initialTab=null)
    {
        var panel=new StackPanel {Spacing=12,Margin=new(24)};panel.Children.Add(new TextBlock {Text="Emulatoren & Einstellungen",FontSize=25,FontWeight=FontWeight.Bold});
        if(initialEmulator!=null)panel.Children.Add(new TextBlock {Text=status.Text,TextWrapping=TextWrapping.Wrap});
        var theme=new ComboBox {ItemsSource=new[]{"Midnight","Classic Light","Retro Green","Neon Purple"},SelectedItem=settings.Theme};panel.Children.Add(new TextBlock {Text="Darstellung"});panel.Children.Add(theme);
        var esDeStartup=new CheckBox{Content="Beim Start AlekFull NX in ES-DE öffnen",IsChecked=settings.StartInEsDe};esDeStartup.IsCheckedChanged+=(_,_)=>Guard(()=>{settings.StartInEsDe=esDeStartup.IsChecked==true;settingsStore.Save(settings);});panel.Children.Add(esDeStartup);
        panel.Children.Add(new TextBlock{Text="AlekFull NX nutzt die originale ES-DE-Oberfläche mit Varianten, Schriftarten, Sounds und Animationen. Spiele starten über EmuVora mit automatischer Emulator-/Core-Einrichtung. ES-DE und Theme werden beim ersten Öffnen auf dein gewähltes Laufwerk heruntergeladen. Theme-Optionen änderst du in ES-DE unter Menü → UI-Einstellungen.",TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(AsyncButton("ES-DE + AlekFull NX vorab herunterladen",async()=>{await BusyAsync(async token=>{await EsDeFrontend.InstallAsync(dataDir,settings,packages,DownloadProgress(),token);settingsStore.Save(settings);status.Text="ES-DE und AlekFull NX sind installiert. Links „AlekFull NX · ES-DE öffnen“ wählen.";});}));
        panel.Children.Add(new TextBlock {Text="Datenordner: "+dataDir,TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(ActionButton("Datenordner öffnen",()=>OpenPath(dataDir)));
        panel.Children.Add(new TextBlock {Text="Klassische EmulationStation-Themes: importierter Hintergrund, PNG/JPEG/SVG-Konsolenlogo und Listen-/Detailfarben. EmuVora behält sein Layout und seine Bedienelemente. Eigene Schriftdateien, Animationen, Videos, Extras und ES-DE-Varianten sind noch nicht kompatibel.",TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(AsyncButton("EmulationStation-Theme importieren …",async()=>
        {
            var folders=await new ControllerPicker(this).OpenFolderPickerAsync(new(){Title="Theme-Set mit theme.xml oder Konsolen-Unterordnern",AllowMultiple=false});
            if(folders.Count==0||folders[0].TryGetLocalPath() is not{} path)return;
            await BusyAsync(async token=>{var imported=await Task.Run(()=>EmulationStationTheme.Import(path,dataDir),token);settings.EmulationStationTheme=imported;settingsStore.Save(settings);loadedThemes.Clear();ApplyTheme();Refresh();status.Text="EmulationStation-Theme importiert. Darstellung wechselt mit dem Konsolenfilter.";});
        }));
        panel.Children.Add(ActionButton("EmuVora-Standarddarstellung verwenden",()=>{settings.EmulationStationTheme=null;settingsStore.Save(settings);loadedThemes.Clear();ApplyTheme();Refresh();}));
        var themePanel=new StackPanel{Spacing=12,Margin=new(24)};themePanel.Children.Add(TvModeOption());foreach(var child in panel.Children.Skip(1).ToArray()){panel.Children.Remove(child);themePanel.Children.Add(child);}
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
        panel.Children.Add(AsyncButton("Vorhandene EXE auswählen",async()=>{var files=await new ControllerPicker(this).OpenFilePickerAsync(new(){Title="Emulator-EXE",AllowMultiple=false,FileTypeFilter=[new("Windows-Programme"){Patterns=["*.exe"]}]});if(files.Count>0)path.Text=files[0].TryGetLocalPath();}));
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
                EmulatorSetup.Configure(id,executable,dataDir,settings.XboxDefaults,settings.Fullscreen,reset:previous==null,firstController:Gamepad.FirstXInputIndex);
                settings.Packages[id]=new(package.Version,executable,previous);settings.EmulatorPaths[id]=executable;settingsStore.Save(settings);Load();status.Text=Current().Name+" automatisch eingerichtet. Eigene BIOS-Dateien bei Bedarf importieren.";
            });
        }));
        panel.Children.Add(ActionButton("Zur vorigen Version zurück",()=>{var id=Current().Id;if(!settings.Packages.TryGetValue(id,out var installed)||!File.Exists(installed.PreviousExecutablePath)){status.Text="Keine vorige Installation vorhanden.";return;}settings.EmulatorPaths[id]=installed.PreviousExecutablePath!;settings.Packages[id]=installed with {Version="rollback",ExecutablePath=installed.PreviousExecutablePath!,PreviousExecutablePath=installed.ExecutablePath};settingsStore.Save(settings);Load();status.Text="Vorige Version aktiviert.";}));
        panel.Children.Add(AsyncButton("Xbox-Standardprofil erneut anwenden",async()=>
        {
            var id=Current().Id;if(runningEmulators.Contains(id)){status.Text="Emulator zuerst schließen.";return;}
            if(!settings.Packages.TryGetValue(id,out var installed)||settings.EmulatorPaths.GetValueOrDefault(id)!=installed.ExecutablePath){status.Text="Automatische Profile sind für verwaltete Downloads verfügbar.";return;}
            if(await ConfirmAsync("Xbox-Standardprofil erneut anwenden? Die bisherigen Konfigurationsdateien werden als .bak gesichert."))Guard(()=>{EmulatorSetup.Configure(id,installed.ExecutablePath,dataDir,true,settings.Fullscreen,true,Gamepad.FirstXInputIndex);status.Text="Xbox-Profil angewendet.";});
        }));
        var downloadStatus=new TextBlock {TextWrapping=TextWrapping.Wrap};
        void FollowStatus(object? sender,AvaloniaPropertyChangedEventArgs e){if(e.Property==TextBlock.TextProperty)downloadStatus.Text=status.Text;}
        status.PropertyChanged+=FollowStatus;panel.Children.Add(downloadStatus);
        panel.Children.Add(ActionButton("Laufenden Download abbrechen",()=>operation?.Cancel()));
        panel.Children.Add(new Separator());panel.Children.Add(new TextBlock {Text="RetroArch-Core je Konsole",FontWeight=FontWeight.Bold});
        var coreSystem=new ComboBox {ItemsSource=EmulatorCatalog.Get("retroarch").Systems,SelectedIndex=0};var corePath=new TextBox();panel.Children.Add(coreSystem);panel.Children.Add(corePath);
        void LoadCore()=>corePath.Text=settings.CorePaths.GetValueOrDefault((string)coreSystem.SelectedItem!)??"";
        coreSystem.SelectionChanged+=(_,_)=>LoadCore();LoadCore();
        panel.Children.Add(AsyncButton("Core-Datei wählen",async()=>{var files=await new ControllerPicker(this).OpenFilePickerAsync(new(){Title="RetroArch-Core",FileTypeFilter=[new("Core"){Patterns=["*.dll"]}]});if(files.Count>0)corePath.Text=files[0].TryGetLocalPath();}));
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
        var tabs=new TabControl{ItemsSource=new[]{new TabItem{Header="Bibliothek",Content=new ScrollViewer{Content=LibraryOptions()}},new TabItem{Header="Bild & Monitor",Content=new ScrollViewer{Content=DisplayOptions()}},new TabItem{Header="Darstellung",Content=new ScrollViewer{Content=themePanel}},new TabItem{Header="Emulatoren",Content=new ScrollViewer{Content=panel}},new TabItem{Header="App-Updates",Content=new ScrollViewer{Content=AppUpdateOptions()}},new TabItem{Header="Sicherung",Content=new ScrollViewer{Content=BackupOptions()}}},SelectedIndex=initialTab??(initialEmulator==null?0:3)};
        var dialog=Dialog("EmuVora · Einstellungen",tabs);dialog.Width=760;dialog.Height=760;dialog.Content=tabs;dialog.Closed+=(_,_)=>status.PropertyChanged-=FollowStatus;dialog.Closing+=(_,e)=>{if(operation!=null){e.Cancel=true;operation.Cancel();status.Text="Vorgang wird abgebrochen. Danach kann das Fenster geschlossen werden.";}};await dialog.ShowDialog(this);
    }
    private async Task CheckAppUpdatesAsync()
    {
        try
        {
            var release=await new AppUpdates(updateHttp).CheckAsync(AppVersion);
            if(isClosed)return;
            if(appRelease?.Version!=release?.Version)appUpdateFile=null;
            appRelease=release;
            appUpdateStatus.Text=release==null?"EmuVora ist aktuell · v"+AppVersion.ToString(3):"EmuVora v"+release.Version+" verfügbar · Einstellungen → App-Updates";
        }
        catch(Exception ex){if(!isClosed)appUpdateStatus.Text="EmuVora-Updateprüfung fehlgeschlagen: "+ex.Message;}
    }
    private StackPanel AppUpdateOptions()
    {
        var panel=new StackPanel{Spacing=14,Margin=new(24)};
        panel.Children.Add(new TextBlock{Text="EmuVora · v"+AppVersion.ToString(3),FontSize=25,FontWeight=FontWeight.Bold});
        var automatic=new CheckBox{Content="Beim Start nach neuen EmuVora-Versionen suchen",IsChecked=settings.CheckAppUpdatesOnStartup};
        automatic.IsCheckedChanged+=(_,_)=>Guard(()=>{settings.CheckAppUpdatesOnStartup=automatic.IsChecked==true;settingsStore.Save(settings);});panel.Children.Add(automatic);
        var message=new TextBlock{Text=appUpdateStatus.Text,TextWrapping=TextWrapping.Wrap};panel.Children.Add(message);
        panel.Children.Add(AsyncButton("Jetzt auf App-Updates prüfen",async()=>{await CheckAppUpdatesAsync();message.Text=appUpdateStatus.Text;}));
        panel.Children.Add(AsyncButton("Update herunterladen",async()=>
        {
            if(operation!=null)return;
            await CheckAppUpdatesAsync();message.Text=appUpdateStatus.Text;if(appRelease==null)return;
            var release=appRelease;
            bool complete=false;message.Text="EmuVora-Update wird heruntergeladen …";
            try{await BusyAsync(async token=>{status.Text=message.Text;appUpdateFile=await new AppUpdates(updateHttp).DownloadAsync(release,Path.Combine(dataDir,"updates"),new Progress<double>(value=>{if(!complete)message.Text="EmuVora-Update wird heruntergeladen · "+value.ToString("0")+" %";progress.IsIndeterminate=false;progress.Value=value;}),token);status.Text="Update geprüft und bereit. Unter App-Updates neu starten.";});}
            finally{complete=true;}
            message.Text=appUpdateFile==null?status.Text:"Download geprüft · v"+release.Version+" · bereit zum Installieren";
        }));
        panel.Children.Add(ActionButton("Download abbrechen",()=>operation?.Cancel()));
        panel.Children.Add(AsyncButton("Aktualisieren und neu starten",async()=>
        {
            if(operation!=null||running.Count>0||esDeProcess!=null){message.Text="Downloads, Spiele und ES-DE zuerst beenden.";return;}
            if(appRelease==null||appUpdateFile==null){message.Text="Zuerst ein verfügbares Update herunterladen.";return;}
            try
            {
            await AppUpdates.VerifyAsync(appUpdateFile,appRelease);
            settingsStore.Save(settings);
            var root=AppUpdates.InstallationRoot(Environment.ProcessPath!);
            if(root==null)
            {
                var start=new ProcessStartInfo(appUpdateFile){UseShellExecute=false};start.ArgumentList.Add("--data-dir");start.ArgumentList.Add(dataDir);start.ArgumentList.Add("--wait-for-pid");start.ArgumentList.Add(Environment.ProcessId.ToString());Process.Start(start);
            }
            else Process.Start(AppUpdates.SetupStartInfo(appUpdateFile,root,dataDir,Environment.ProcessId));
            if(Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime)lifetime.Shutdown();
            }
            catch(Exception ex){message.Text="Update konnte nicht gestartet werden: "+ex.Message;Log(ex);}
        }));
        panel.Children.Add(ActionButton("Downloadordner öffnen",()=>{var directory=Path.Combine(dataDir,"updates");Directory.CreateDirectory(directory);OpenPath(directory);}));
        panel.Children.Add(new TextBlock{Text="Stabile Windows-Versionen aus dem offiziellen EmuVora-Repository. Jeder Download wird mit SHA-256 geprüft. Das Setup wartet auf das Programmende, behält den Datenpfad und sichert die vorige App-Version. Bestehende Verknüpfungen bleiben erhalten. Bei einer portablen Version wählst du den Installationsordner im Setup. Ohne Internet kannst du weiterspielen.",TextWrapping=TextWrapping.Wrap});
        return panel;
    }
    private StackPanel LibraryOptions()
    {
        var panel=new StackPanel{Spacing=14,Margin=new(24)};panel.Children.Add(new TextBlock{Text="Deine Bibliothek",FontSize=25,FontWeight=FontWeight.Bold});panel.Children.Add(AsyncButton("Einrichtungsassistent öffnen",()=>ShowOnboardingAsync()));panel.Children.Add(DataDirectoryOptions());panel.Children.Add(RecognitionOptions());
        var automatic=new CheckBox{Content="Fehlende Cover beim Start und nach einem Import automatisch laden",IsChecked=settings.AutoDownloadCovers};automatic.IsCheckedChanged+=(_,_)=>Guard(()=>{settings.AutoDownloadCovers=automatic.IsChecked==true;settingsStore.Save(settings);});panel.Children.Add(automatic);
        panel.Children.Add(new TextBlock{Text="Quelle: libretro-thumbnails auf GitHub, ohne Konto. Vorhandene Cover bleiben erhalten. Titelvarianten werden erkannt; ähnliche Spielnamen und Fortsetzungen werden nicht geraten. Nicht jeder Titel ist verfügbar.",TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(new TextBlock{Text="Bevorzugte Cover-Region"});var region=new ComboBox{ItemsSource=new[]{"Europe","Germany","USA","Japan","World"},SelectedItem=settings.CoverRegion};region.SelectionChanged+=(_,_)=>Guard(()=>{settings.CoverRegion=region.SelectedItem as string??"Europe";settingsStore.Save(settings);});panel.Children.Add(region);
        panel.Children.Add(AsyncButton("Alle fehlenden Cover laden",()=>FetchAllCoversAsync()));panel.Children.Add(ActionButton("Cover-Bericht öffnen",()=>{var file=Path.Combine(dataDir,"cover-report.txt");if(File.Exists(file))OpenPath(file);else status.Text="Noch kein Cover-Bericht vorhanden.";}));
        panel.Children.Add(new Separator());panel.Children.Add(new TextBlock{Text="Covergröße"});var size=new Slider{Minimum=120,Maximum=220,Value=Math.Clamp(settings.CoverSize,120,220),TickFrequency=20,IsSnapToTickEnabled=true};size.PropertyChanged+=(_,e)=>{if(e.Property==Slider.ValueProperty)Guard(()=>{settings.CoverSize=(int)size.Value;settingsStore.Save(settings);Refresh();});};panel.Children.Add(size);
        var details=new CheckBox{Content="Spielinformationen rechts anzeigen (auf breiten Fenstern)",IsChecked=settings.ShowGameDetails};details.IsCheckedChanged+=(_,_)=>Guard(()=>{settings.ShowGameDetails=details.IsChecked==true;settingsStore.Save(settings);ApplyLayout();});panel.Children.Add(details);
        panel.Children.Add(new TextBlock{Text="Bibliothek und Spielstände",FontWeight=FontWeight.Bold});panel.Children.Add(new TextBlock{Text=dataDir,TextWrapping=TextWrapping.Wrap});panel.Children.Add(ActionButton("Datenordner öffnen",()=>OpenPath(dataDir)));panel.Children.Add(AsyncButton("Bibliothek sichern",()=>ExportAsync()));panel.Children.Add(AsyncButton("Sicherung wiederherstellen",()=>RestoreAsync()));return panel;
    }
    private StackPanel DisplayOptions()
    {
        var panel=new StackPanel{Spacing=14,Margin=new(24)};panel.Children.Add(new TextBlock{Text="Ein klares Bild auf deinem Monitor",FontSize=25,FontWeight=FontWeight.Bold,TextWrapping=TextWrapping.Wrap});
        var monitors=new TextBlock{TextWrapping=TextWrapping.Wrap};void Detect(){var found=DisplayDetection.All();monitors.Text=found.Count==0?"Kein Monitor erkannt.":string.Join("\n",found.Select(m=>m.ToString()));}Detect();panel.Children.Add(monitors);panel.Children.Add(ActionButton("Monitore neu erkennen",Detect));
        var automatic=new CheckBox{Content="Monitorauflösung und Bildprofil automatisch berücksichtigen",IsChecked=settings.AutomaticDisplay};automatic.IsCheckedChanged+=(_,_)=>Guard(()=>{settings.AutomaticDisplay=automatic.IsChecked==true;settingsStore.Save(settings);UpdateSelected();});panel.Children.Add(automatic);
        var mode=new ComboBox{ItemsSource=new[]{"Automatisch · scharf und im Originalformat","Pixelgenau · ganze Skalierungsstufen","Einpassen · scharf, mit originalem Seitenverhältnis","Weich · mit bilinearer Filterung"},SelectedIndex=Math.Max(0,Array.IndexOf(DisplayProfiles.Modes,settings.DisplayMode))};mode.SelectionChanged+=(_,_)=>Guard(()=>{settings.DisplayMode=DisplayProfiles.Modes[Math.Max(0,mode.SelectedIndex)];settingsStore.Save(settings);UpdateSelected();});panel.Children.Add(mode);
        panel.Children.Add(new TextBlock{Text="Maximale interne Auflösung für PS1, PS2, GameCube, Wii und PSP"});var scale=new ComboBox{ItemsSource=new[]{"1× · Original / sparsam","2×","3× · empfohlen","4×","5×","6×"},SelectedIndex=Math.Clamp(settings.MaximumRenderScale,1,6)-1};scale.SelectionChanged+=(_,_)=>Guard(()=>{settings.MaximumRenderScale=scale.SelectedIndex+1;settingsStore.Save(settings);UpdateSelected();});panel.Children.Add(scale);
        panel.Children.Add(new TextBlock{Text="Automatisch verwendet 2D-Pixel ohne Weichzeichner und ohne Abschneiden. 3D-Emulatoren skalieren intern passend zur Monitorhöhe bis zur gewählten Grenze. RetroArch erhält das Format vom jeweiligen Core; PS1/PS2/Dolphin erkennen ihr Spielformat, PSP bleibt im Originalformat. Schwarze Ränder sind bei unterschiedlichen Formaten gewollt. Die Desktopauflösung bleibt erhalten. Bei Ruckeln die maximale Auflösung reduzieren.",TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(new TextBlock{Text="Bildprofile gelten für von EmuVora verwaltete Emulatoren. Änderungen in deren Einstellungen bleiben nach der ersten Profil-Einrichtung erhalten. Beim nächsten Spielstart wird das Profil geprüft; auch im ES-DE-Modus.",TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(AsyncButton("Gewähltes Bildprofil erneut anwenden",async()=>{if(running.Count>0||esDeProcess!=null){status.Text="Laufende Spiele und ES-DE zuerst schließen.";return;}if(await ConfirmAsync("Bild-Einstellungen der verwalteten Emulatoren auf dieses Profil setzen? Andere Einstellungen bleiben erhalten; bisherige Konfigurationsdateien werden als .bak gesichert."))Guard(()=>{foreach(var definition in EmulatorCatalog.Known)if(settings.Packages.TryGetValue(definition.Id,out var package)&&settings.EmulatorPaths.GetValueOrDefault(definition.Id)==package.ExecutablePath&&File.Exists(package.ExecutablePath))DisplayProfiles.Apply(definition.Id,package.ExecutablePath,Selected?.System??definition.Systems[0],DisplayDetection.Current(TryGetPlatformHandle()?.Handle??IntPtr.Zero),settings,true);status.Text="Bildprofil angewendet.";});}));return panel;
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
        panel.Children.Add(new TextBlock{Text="Weitere Gamepads: SDL und die mitgelieferte Controller-Datenbank belegen die EmuVora-Oberfläche automatisch. A/B/X/Y bezeichnen die Positionen wie beim Xbox-Layout. Spielprofile für andere Controller müssen im jeweiligen Emulator eingerichtet werden; Xbox-Standardprofile bleiben erhalten.",TextWrapping=TextWrapping.Wrap,FontSize=12});
        panel.Children.Add(AsyncButton("Controller prüfen",ShowControllerDiagnosticsAsync));
        panel.Children.Add(AsyncButton("Eigene BIOS-/Firmware-Dateien importieren",async()=>{var files=await new ControllerPicker(this).OpenFilePathsAsync("Eigene BIOS-/Firmware-Dateien");if(files.Length==0)return;await ImportFirmwareFilesAsync(files);}));
        panel.Children.Add(AsyncButton("Firmware-Ordner importieren (z. B. MSX)",async()=>
        {
            var folders=await new ControllerPicker(this).OpenFolderPickerAsync(new(){Title="Ordner mit eigenen Firmware-Dateien"});if(folders.Count==0||folders[0].TryGetLocalPath() is not{} folder)return;
            if(await BusyAsync(async token=>await Task.Run(()=>EmulatorSetup.ImportFirmwareFolder(folder,dataDir,token),token)))status.Text="Firmware-Ordner importiert.";
        }));
        panel.Children.Add(ActionButton("BIOS-/Firmware-Ordner öffnen",()=>{Directory.CreateDirectory(EmulatorSetup.FirmwareDirectory(dataDir));OpenPath(EmulatorSetup.FirmwareDirectory(dataDir));}));
        panel.Children.Add(new TextBlock {Text="Systemhinweise",FontWeight=FontWeight.Bold});
        var hints=new ComboBox {ItemsSource=SystemCatalog.RetroSystems.Select(s=>s.Name).ToArray(),SelectedIndex=0};var hintText=new TextBlock {TextWrapping=TextWrapping.Wrap};
        if(Selected is{} selectedGame&&SystemCatalog.Find(selectedGame.System)!=null)hints.SelectedItem=selectedGame.System;
        void LoadHint(){var system=SystemCatalog.Find((string)hints.SelectedItem!)!;hintText.Text=(EmulatorSetup.FirmwareHint(system.Name,dataDir)??"Firmware-Prüfung: keine fehlende Pflichtdatei erkannt.")+"\n"+system.Note+"\nDateien: "+system.Extensions+" · Ordner: "+system.Aliases.Split('|')[0];}
        hints.SelectionChanged+=(_,_)=>LoadHint();LoadHint();panel.Children.Add(hints);panel.Children.Add(hintText);
    }
    private void ConfigureCores(string executable,string? oldDirectory=null)
    {
        var directory=Path.GetDirectoryName(executable)!;
        foreach(var (system,id) in PackageManager.CoreIds)
        {
            var current=settings.CorePaths.GetValueOrDefault(system);
            if(oldDirectory==null&&current!=null&&File.Exists(current))continue;
            if(oldDirectory!=null&&current!=null&&!Path.GetFullPath(current).StartsWith(Path.GetFullPath(oldDirectory)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))continue;
            var file=Directory.EnumerateFiles(directory,id+"_libretro.dll",SearchOption.AllDirectories).FirstOrDefault();
            if(file!=null)settings.CorePaths[system]=file;
        }
    }
    private static void PreserveConfiguration(string previous,string next)
    {
        var source=Path.GetDirectoryName(previous)!;var destination=Path.GetDirectoryName(next)!;
        foreach(var name in new[]{"retroarch.cfg","retroarch-core-options.cfg","retroarch-core-options.cfg.emuvora-defaults.json","portable.ini","portable.txt","settings.ini","retroarch.cfg.emuvora-defaults.json","settings.ini.emuvora-defaults.json",".emuvora-configured",".emuvora-video-profile","retroarch.cfg.emuvora-video-original.bak","settings.ini.emuvora-video-original.bak"})if(File.Exists(Path.Combine(source,name))&&(File.GetAttributes(Path.Combine(source,name))&FileAttributes.ReparsePoint)==0)File.Copy(Path.Combine(source,name),Path.Combine(destination,name),true);
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







