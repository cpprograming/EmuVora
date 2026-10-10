using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media.Imaging;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using EmuVora.Core;
using System.Text.Json;

namespace EmuVora.App;

public sealed class MainWindow : Window
{
    private readonly LibraryStore store;
    private readonly ListBox gamesList = new() { MinWidth=380 };
    private readonly TextBlock status = new() { Text="Ready", TextWrapping=TextWrapping.Wrap };
    private readonly ComboBox themes = new();
    private readonly TextBox emulatorPath = new() { Watermark="Full path to emulator executable" };
    private readonly TextBox corePath = new() { Watermark="RetroArch core path (when needed)" };
    private readonly ComboBox emulators = new();
    private readonly TextBox search = new() { Watermark="Search games..." };
    private readonly ComboBox systems = new() { MinWidth=160 };
    private readonly TextBlock emptyLibrary = new() { TextWrapping=TextWrapping.Wrap, Margin=new Thickness(12) };
    private readonly Dictionary<string,Bitmap> coverImages = new();
    private IReadOnlyList<Game> allGames = [];
    private readonly string settingsFile;
    private readonly Border root = new();
    private readonly StackPanel sidebar = new() { Spacing=12, Margin=new Thickness(20) };
    private readonly Grid main = new() { RowDefinitions=new RowDefinitions("Auto,Auto,*,Auto,Auto,Auto,Auto"), RowSpacing=12, Margin=new Thickness(22) };
    private Settings settings;
    private record Settings(string Theme="Midnight", Dictionary<string,string>? EmulatorPaths=null, Dictionary<string,string>? CorePaths=null, Dictionary<string,string>? Covers=null);
    private static readonly Dictionary<string,(string Background,string Foreground,string Accent)> Palette = new()
    {
        ["Midnight"]=("#101722","#EAF0FA","#6AA8FF"),
        ["Classic Light"]=("#F3F5F9","#1E2736","#4478DB"),
        ["Retro Green"]=("#101A13","#D9FFE1","#69E991"),
        ["Neon Purple"]=("#1A1026","#F5E6FF","#CC8CFF")
    };
    public MainWindow()
    {
        Title="EmuVora · v0.1 Preview"; Width=1150; Height=750; MinWidth=780; MinHeight=530;
        var dataDir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EmuVora");
        Directory.CreateDirectory(dataDir); settingsFile=Path.Combine(dataDir,"settings.json");
        try { settings=JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsFile)) ?? new(); } catch { settings=new(); }
        store=new LibraryStore(Path.Combine(dataDir,"library.db"));
        allGames=store.GetAll();
        var layout = new Grid { ColumnDefinitions=new ColumnDefinitions("250,*") };
        var sidebarScroll=new ScrollViewer {Content=sidebar};
        Grid.SetColumn(sidebarScroll,0); Grid.SetColumn(main,1); layout.Children.Add(sidebarScroll); layout.Children.Add(main);
        root.Child=layout; Content=root;
        sidebar.Children.Add(new TextBlock { Text="◈ EmuVora", FontSize=27, FontWeight=FontWeight.Bold });
        sidebar.Children.Add(new TextBlock { Text="YOUR LIBRARY", FontSize=12 });
        var import = new Button { Content="+ Import ROM folder" }; import.Click += async (_,_)=>await ImportAsync(); sidebar.Children.Add(import);
        sidebar.Children.Add(new TextBlock { Text="THEME" });
        foreach (var theme in Palette.Keys) themes.Items.Add(theme);
        themes.SelectedItem=Palette.ContainsKey(settings.Theme)?settings.Theme:"Midnight";
        themes.SelectionChanged += (_,_)=>{ if(themes.SelectedItem is string t){settings=settings with { Theme=t }; Save(); ApplyTheme(t);} }; sidebar.Children.Add(themes);
        sidebar.Children.Add(new TextBlock { Text="EMULATOR CONFIGURATION" });
        foreach (var def in EmulatorCatalog.Known) emulators.Items.Add(def.Name);
        emulators.SelectionChanged+=(_,_)=>LoadEmulatorSettings(); emulators.SelectedIndex=0;
        sidebar.Children.Add(emulators); sidebar.Children.Add(emulatorPath); sidebar.Children.Add(corePath);
        var chooseExe=new Button {Content="Browse executable..."}; chooseExe.Click+=async (_,_)=>await BrowseExecutableAsync(); sidebar.Children.Add(chooseExe);
        var chooseCore=new Button {Content="Browse RetroArch core..."}; chooseCore.Click+=async (_,_)=>await BrowseCoreAsync(); sidebar.Children.Add(chooseCore);
        var saveButton=new Button { Content="Save emulator settings" }; saveButton.Click+=(_,_)=>SaveEmulator(); sidebar.Children.Add(saveButton);
        var websiteButton=new Button { Content="Official emulator website ↗" }; websiteButton.Click+=(_,_)=>OpenOfficialWebsite(); sidebar.Children.Add(websiteButton);
        sidebar.Children.Add(new TextBlock { Text="Downloads / auto-updates: planned for v0.2. Only official verified releases.", TextWrapping=TextWrapping.Wrap, FontSize=12 });
        main.Children.Add(new TextBlock { Text="Your Games", FontSize=30, FontWeight=FontWeight.Bold });
        var filters = new Grid { ColumnDefinitions=new ColumnDefinitions("*,Auto"), ColumnSpacing=12 };
        filters.Children.Add(search); Grid.SetColumn(systems,1); filters.Children.Add(systems);
        main.Children.Add(filters); search.TextChanged+=(_,_)=>Refresh();
        UpdateSystems(); systems.SelectionChanged+=(_,_)=>Refresh();
        gamesList.ItemsPanel=new FuncTemplate<Panel>(()=>new WrapPanel());
        gamesList.ItemTemplate=new FuncDataTemplate<Game>((game,_)=>CreateGameCard(game));
        ScrollViewer.SetHorizontalScrollBarVisibility(gamesList,Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled);
        gamesList.SelectionChanged+=(_,_)=>UpdateSelected();
        main.Children.Add(new Border { Child=gamesList });
        main.Children.Add(emptyLibrary);
        var play=new Button {Content="▶ PLAY SELECTED GAME", HorizontalAlignment=HorizontalAlignment.Left}; play.Click+=async (_,_)=>await PlayAsync(); main.Children.Add(play);
        var coverButton=new Button {Content="Choose cover for selected game..."}; coverButton.Click+=async (_,_)=>await ChooseCoverAsync(); main.Children.Add(coverButton);
        main.Children.Add(status);
        for(var row=0;row<main.Children.Count;row++) Grid.SetRow(main.Children[row],row);
        ApplyTheme(themes.SelectedItem as string ?? "Midnight"); Refresh();
        Closed+=(_,_)=>{foreach(var bitmap in coverImages.Values) bitmap.Dispose();};
    }
    private void ApplyTheme(string theme)
    {
        var p=Palette[theme]; Background=Brush.Parse(p.Background); Foreground=Brush.Parse(p.Foreground);
        sidebar.Background=Brush.Parse(theme=="Classic Light"?"#E6EAF2":"#1A2433");
        main.Background=Brush.Parse(p.Background); status.Foreground=Brush.Parse(p.Accent);
    }
    private void Save() => File.WriteAllText(settingsFile,JsonSerializer.Serialize(settings,new JsonSerializerOptions{WriteIndented=true}));
    private void Refresh()
    {
        var selectedPath=SelectedGame()?.Path;
        var query=search.Text?.Trim()??"";
        var system=systems.SelectedItem as string;
        var filtered=allGames.Where(g=>(system is null or "All consoles" || g.System==system) && (g.Title.Contains(query,StringComparison.OrdinalIgnoreCase)||g.System.Contains(query,StringComparison.OrdinalIgnoreCase))).ToArray();
        gamesList.ItemsSource=filtered;
        gamesList.SelectedItem=filtered.FirstOrDefault(g=>g.Path==selectedPath);
        emptyLibrary.IsVisible=filtered.Length==0;
        emptyLibrary.Text=allGames.Count==0?"Your library is empty. Import a ROM folder to get started.":"No games match your search and console filter.";
        status.Text=$"{allGames.Count} games in library · {filtered.Length} shown";
    }
    private void UpdateSystems()
    {
        var selected=systems.SelectedItem as string;
        var options=new[]{"All consoles"}.Concat(allGames.Select(g=>g.System).Distinct().OrderBy(s=>s)).ToArray();
        systems.ItemsSource=options;
        systems.SelectedItem=options.Contains(selected)?selected:"All consoles";
    }
    private Game? SelectedGame() => gamesList.SelectedItem as Game;
    private Control CreateGameCard(Game game)
    {
        Control artwork=new TextBlock {Text="◈\n"+game.System,FontSize=24,TextAlignment=TextAlignment.Center,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center};
        var path=settings.Covers?.GetValueOrDefault(game.Path);
        if(path!=null && File.Exists(path))
        {
            try
            {
                if(!coverImages.TryGetValue(path,out var bitmap))
                {
                    using var stream=File.OpenRead(path);
                    bitmap=Bitmap.DecodeToWidth(stream,360); coverImages[path]=bitmap;
                }
                artwork=new Image {Source=bitmap,Stretch=Stretch.Uniform};
            }
            catch { /* A missing or invalid cover keeps the console placeholder visible. */ }
        }
        var card=new StackPanel {Width=160,Spacing=8,Margin=new Thickness(6)};
        card.Children.Add(new Border {Height=190,CornerRadius=new CornerRadius(8),Background=Brush.Parse("#25334A"),Child=artwork});
        card.Children.Add(new TextBlock {Text=game.Title,FontWeight=FontWeight.SemiBold,TextWrapping=TextWrapping.Wrap,MaxHeight=44});
        card.Children.Add(new TextBlock {Text=game.System,FontSize=12,Opacity=0.7});
        ToolTip.SetTip(card,game.Title+"\n"+game.Path);
        return card;
    }
    private async Task ChooseCoverAsync()
    {
        var game=SelectedGame(); if(game==null){status.Text="Select a game first";return;}
        var files=await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {Title="Choose game cover",AllowMultiple=false,FileTypeFilter=[new FilePickerFileType("Cover images") {Patterns=["*.png","*.jpg","*.jpeg","*.webp","*.bmp"]}]});
        if(files.Count==0)return;
        try
        {
            var source=files[0].TryGetLocalPath()??throw new InvalidOperationException("Choose a local image file.");
            using(var stream=File.OpenRead(source)) using(var validation=Bitmap.DecodeToWidth(stream,360)) { }
            var coverFolder=Path.Combine(Path.GetDirectoryName(settingsFile)!,"covers"); Directory.CreateDirectory(coverFolder);
            var destination=Path.Combine(coverFolder,Guid.NewGuid().ToString("N")+Path.GetExtension(source));
            File.Copy(source,destination);
            var covers=new Dictionary<string,string>(settings.Covers??new()); covers[game.Path]=destination;
            var updated=settings with {Covers=covers};
            File.WriteAllText(settingsFile,JsonSerializer.Serialize(updated,new JsonSerializerOptions{WriteIndented=true}));
            settings=updated; Refresh(); status.Text=$"Saved cover for {game.Title}";
        }
        catch(Exception ex){status.Text=$"Cover could not be saved: {ex.Message}";}
    }
    private void UpdateSelected() { var g=SelectedGame(); if(g!=null) status.Text=$"{g.System} · {g.Path}"; }
    private async Task ImportAsync()
    {
        var folders=await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions{Title="Choose ROM folder",AllowMultiple=false});
        if(folders.Count==0) return;
        try { var path=folders[0].TryGetLocalPath()??throw new InvalidOperationException("Only local folders supported");
            var found=await Task.Run(()=>RomScanner.Scan(path)); store.Upsert(found); allGames=store.GetAll(); UpdateSystems(); Refresh(); status.Text=$"Imported {found.Count} files. Disc images may need manual system classification.";
        } catch(Exception ex) {status.Text=$"Import failed: {ex.Message}";}
    }
    private EmulatorDefinition? CurrentEmulator() => emulators.SelectedIndex >= 0 && emulators.SelectedIndex < EmulatorCatalog.Known.Length ? EmulatorCatalog.Known[emulators.SelectedIndex] : null;
    private void LoadEmulatorSettings() { var def=CurrentEmulator(); if(def==null)return; emulatorPath.Text=settings.EmulatorPaths?.GetValueOrDefault(def.Id)??""; corePath.Text=settings.CorePaths?.GetValueOrDefault(def.Id)??""; }
    private void SaveEmulator()
    {
        var def=CurrentEmulator(); if(def==null)return;
        var paths=new Dictionary<string,string>(settings.EmulatorPaths??new()); paths[def.Id]=emulatorPath.Text??"";
        var cores=new Dictionary<string,string>(settings.CorePaths??new()); cores[def.Id]=corePath.Text??"";
        settings=settings with {EmulatorPaths=paths,CorePaths=cores}; Save(); status.Text=$"Saved {def.Name} configuration";
    }
    private async Task BrowseExecutableAsync()
    {
        var files=await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions{Title="Choose emulator executable",AllowMultiple=false});
        if(files.Count>0) emulatorPath.Text=files[0].TryGetLocalPath();
    }
    private async Task BrowseCoreAsync()
    {
        var files=await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions{Title="Choose RetroArch core",AllowMultiple=false});
        if(files.Count>0) corePath.Text=files[0].TryGetLocalPath();
    }
    private void OpenOfficialWebsite()
    {
        var def=CurrentEmulator(); if(def!=null) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(def.Homepage){UseShellExecute=true});
    }
    private async Task PlayAsync()
    {
        var game=SelectedGame(); if(game==null){status.Text="Select a game first";return;}
        var def=EmulatorCatalog.Known.FirstOrDefault(e=>e.Systems.Contains(game.System) && File.Exists(settings.EmulatorPaths?.GetValueOrDefault(e.Id)));
        if(def==null){status.Text=$"No configured emulator found for {game.System}. Configure one on the left.";return;}
        try { await new ExternalEmulatorAdapter(def).LaunchAsync(game,settings.EmulatorPaths![def.Id],settings.CorePaths?.GetValueOrDefault(def.Id));status.Text=$"Launched {game.Title} with {def.Name}"; }
        catch(Exception ex){status.Text=$"Launch failed: {ex.Message}";}
    }
}

