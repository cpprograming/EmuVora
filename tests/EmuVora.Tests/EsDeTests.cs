using EmuVora.Core;
using System.Xml.Linq;
using System.Text.Json;
using System.Diagnostics;
using Xunit;
namespace EmuVora.Tests;

public class EsDeTests
{
    [Fact] public void ExportUsesSafeIdsPreservesRomLocationsAndCopiesCoverMedia()
    {
        using var dir=new TestDirectory();var launcher=dir.File("Program with spaces/EmuVora.App.exe");var rom=dir.File("Original ROMs/Game & Friends.chd");var game=new Game("Game & Friends","PS1",rom,true,null,3);var cover=dir.File("original-cover.png","image");var settings=new AppSettings{Covers=new(){{rom,cover}}};
        EsDeFrontend.Export(dir.Path,launcher,[game],settings);
        var app=EsDeFrontend.AppData(dir.Path);var systems=XDocument.Load(System.IO.Path.Combine(app,"custom_systems/es_systems.xml"));var system=Assert.Single(systems.Root!.Elements());
        Assert.Equal("psx",system.Element("theme")!.Value);var command=system.Element("command")!.Value;Assert.Contains("\""+launcher+"\" --esde-launch \"%BASENAME%\"",command);Assert.DoesNotContain("Game & Friends",command);
        var id=EsDeFrontend.GameId(game);Assert.Equal(64,id.Length);Assert.True(System.IO.File.Exists(System.IO.Path.Combine(EsDeFrontend.Home(dir.Path),"ROMs/emuvora-psx",id+".emuvora")));
        Assert.Equal("image",System.IO.File.ReadAllText(System.IO.Path.Combine(app,"downloaded_media/emuvora-psx/covers",id+".png")));Assert.True(System.IO.File.Exists(rom));
        var list=XDocument.Load(System.IO.Path.Combine(app,"gamelists/emuvora-psx/gamelist.xml"));Assert.Equal(game.Title,list.Root!.Element("game")!.Element("name")!.Value);
        var config=XDocument.Load(System.IO.Path.Combine(app,"settings/es_settings.xml"));Assert.Contains(config.Root!.Elements("string"),e=>(string?)e.Attribute("name")=="Theme"&&(string?)e.Attribute("value")==EsDeFrontend.ThemeName);
    }
    [Fact] public void NativeFavoritesCountsMetadataAndThemeOptionsSurviveRoundTrip()
    {
        using var dir=new TestDirectory();var launcher=dir.File("app.exe");var game=new Game("Game","NES",dir.File("Game.nes"),false,null,2);var settings=new AppSettings();var store=new LibraryStore(dir.File("library.db",""));store.Restore([game]);
        EsDeFrontend.Export(dir.Path,launcher,[game],settings);var app=EsDeFrontend.AppData(dir.Path);var listPath=System.IO.Path.Combine(app,"gamelists/emuvora-nes/gamelist.xml");var list=XDocument.Load(listPath);var entry=list.Root!.Element("game")!;entry.SetElementValue("favorite","true");entry.SetElementValue("playcount",3);entry.SetElementValue("desc","Native scraped description");list.Save(listPath);
        var settingsPath=System.IO.Path.Combine(app,"settings/es_settings.xml");var native=XDocument.Load(settingsPath);native.Root!.Elements("string").First(e=>(string?)e.Attribute("name")=="ThemeColorScheme").SetAttributeValue("value","light-original");native.Save(settingsPath);
        store.MarkPlayed(game);EsDeFrontend.ImportChanges(dir.Path,store,settings);var updated=Assert.Single(store.GetAll());Assert.True(updated.Favorite);Assert.Equal(3,updated.PlayCount);
        EsDeFrontend.Export(dir.Path,launcher,store.GetAll(),settings);Assert.Equal("Native scraped description",XDocument.Load(listPath).Root!.Element("game")!.Element("desc")!.Value);Assert.Equal("light-original",(string?)XDocument.Load(settingsPath).Root!.Elements("string").First(e=>(string?)e.Attribute("name")=="ThemeColorScheme").Attribute("value"));
    }
    [Fact] public void EveryLibrarySystemHasAUniqueNativeIdAndCanonicalTheme()
    {
        Assert.Equal(47,EmulatorCatalog.Systems.Select(EsDeFrontend.NativeSystem).Distinct().Count());Assert.Equal("sega32x",EsDeFrontend.ThemeId("32X"));Assert.Equal("atarilynx",EsDeFrontend.ThemeId("Atari Lynx"));Assert.Equal("wonderswancolor",EsDeFrontend.ThemeId("WonderSwan Color"));
        Assert.Equal("arcade",EsDeFrontend.ThemeId("Arcade (FinalBurn Neo)"));Assert.Equal("arcade",EsDeFrontend.ThemeId("Arcade (MAME)"));
    }
    [Fact] public void LaunchReferencesRejectCommandsAndRemovedGames()
    {
        using var dir=new TestDirectory();var store=new LibraryStore(dir.File("library.db",""));Assert.Throws<IOException>(()=>EsDeLaunchRequest.Parse(["--esde-launch","game & command","--data-dir",dir.Path]));
        Assert.Throws<IOException>(()=>EsDeLaunchRequest.Parse(["--esde-launch",new string('a',64),"--data-dir","relative"]));Assert.Throws<IOException>(()=>ManagedGameLauncher.FindGame(new(new string('a',64),dir.Path),store));
    }
    [Fact] public async Task BridgeRunsARealChildProcessAndRetainsRomAsOneArgument()
    {
        if(!OperatingSystem.IsWindows())return;
        var probe=Environment.GetEnvironmentVariable("EMUVORA_LAUNCH_PROBE");if(probe==null)return;
        using var dir=new TestDirectory();var rom=dir.File("My ROMs/Game & Friends.chd");var game=new Game("Game","PS1",rom);var capture=System.IO.Path.Combine(dir.Path,"capture.json");new LibraryStore(dir.File("library.db","")).Restore([game]);new SettingsStore(System.IO.Path.Combine(dir.Path,"settings.json")).Save(new(){AutomaticSetup=false,EmulatorPaths=new(){{"duckstation",probe}},Fullscreen=true});
        var old=Environment.GetEnvironmentVariable("EMUVORA_LAUNCH_CAPTURE");Environment.SetEnvironmentVariable("EMUVORA_LAUNCH_CAPTURE",capture);var started=false;
        try{await ManagedGameLauncher.RunAsync(new(EsDeFrontend.GameId(game),dir.Path),0,()=>started=true);Assert.True(started);var args=JsonSerializer.Deserialize<string[]>(System.IO.File.ReadAllText(capture))!;Assert.Equal(new[]{"-batch","-fullscreen",rom},args);Assert.Equal(1,Assert.Single(new LibraryStore(System.IO.Path.Combine(dir.Path,"library.db")).GetAll()).PlayCount);}
        finally{Environment.SetEnvironmentVariable("EMUVORA_LAUNCH_CAPTURE",old);}
    }
    [Fact][Trait("Category","Network")]
    public async Task OfficialEsDeAndPinnedAlekfullInstallVerifyAndContainTheCompleteTheme()
    {
        using var dir=new TestDirectory();using var manager=new PackageManager();var settings=new AppSettings();await EsDeFrontend.InstallAsync(dir.Path,settings,manager);
        Assert.True(System.IO.File.Exists(settings.EsDeExecutable));var theme=System.IO.Path.Combine(EsDeFrontend.AppData(dir.Path),"themes",EsDeFrontend.ThemeName);var capabilities=XDocument.Load(System.IO.Path.Combine(theme,"capabilities.xml"));Assert.Contains(capabilities.Root!.Elements("variant"),e=>(string?)e.Attribute("name")=="gamelist-metadata-on-boxart");Assert.True(System.IO.File.Exists(System.IO.Path.Combine(theme,"_inc/fonts/Oxygen-Regular.ttf")));Assert.True(System.IO.File.Exists(System.IO.Path.Combine(theme,"README.md")));
        var start=new ProcessStartInfo(settings.EsDeExecutable!){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=System.IO.Path.GetDirectoryName(settings.EsDeExecutable)!};start.ArgumentList.Add("--version");using var process=Process.Start(start)!;await process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token);Assert.Equal(0,process.ExitCode);
    }
}
