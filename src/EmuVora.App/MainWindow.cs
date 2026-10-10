using Avalonia;
using Avalonia.Controls;
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
    private IReadOnlyList<Game> allGames = [];
    private readonly string settingsFile;
    private readonly Border root = new();
    private readonly StackPanel sidebar = new() { Spacing=12, Margin=new Thickness(20) };
    private readonly StackPanel main = new() { Spacing=12, Margin=new Thickness(22) };
    private Settings settings;
    private record Settings(string Theme="Midnight", Dictionary<string,string>? EmulatorPaths=null, Dictionary<string,string>? CorePaths=null);
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
        Grid.SetColumn(sidebar,0); Grid.SetColumn(main,1); layout.Children.Add(sidebar); layout.Children.Add(main);
        root.Child=layout; Content=root;
        sidebar.Children.Add(new TextBlock { Text="◈ GAMEHUB", FontSize=27, FontWeight=FontWeight.Bold });
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
        main.Children.Add(search); search.TextChanged+=(_,_)=>Refresh();
        gamesList.SelectionChanged+=(_,_)=>UpdateSelected();
        main.Children.Add(new Border { Child=gamesList, Height=460 });
        var play=new Button {Content="▶ PLAY SELECTED GAME", HorizontalAlignment=HorizontalAlignment.Left}; play.Click+=async (_,_)=>await PlayAsync(); main.Children.Add(play);
        main.Children.Add(status);
        ApplyTheme(themes.SelectedItem as string ?? "Midnight"); Refresh();
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
        var query=search.Text?.Trim()??"";
        gamesList.ItemsSource=allGames.Where(g=>g.Title.Contains(query,StringComparison.OrdinalIgnoreCase)||g.System.Contains(query,StringComparison.OrdinalIgnoreCase)).Select(g=>$"[{g.System}]  {g.Title}").ToArray();
        status.Text=$"{allGames.Count} games in library · {(gamesList.ItemsSource as string[])?.Length ?? 0} shown";
    }
    private Game? SelectedGame()
    {
        var query=search.Text?.Trim()??"";
        var filtered=allGames.Where(g=>g.Title.Contains(query,StringComparison.OrdinalIgnoreCase)||g.System.Contains(query,StringComparison.OrdinalIgnoreCase)).ToArray();
        return gamesList.SelectedIndex>=0 && gamesList.SelectedIndex<filtered.Length?filtered[gamesList.SelectedIndex]:null;
    }
    private void UpdateSelected() { var g=SelectedGame(); if(g!=null) status.Text=$"{g.System} · {g.Path}"; }
    private async Task ImportAsync()
    {
        var folders=await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions{Title="Choose ROM folder",AllowMultiple=false});
        if(folders.Count==0) return;
        try { var path=folders[0].TryGetLocalPath()??throw new InvalidOperationException("Only local folders supported");
            var found=await Task.Run(()=>RomScanner.Scan(path)); store.Upsert(found); allGames=store.GetAll(); Refresh(); status.Text=$"Imported {found.Count} files. Disc images may need manual system classification.";
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

