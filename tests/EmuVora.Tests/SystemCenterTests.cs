using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using EmuVora.Core;
using System.Reflection;
using Xunit;
namespace EmuVora.Tests;
public class SystemCenterTests
{
    [Fact] public void InventoryKeepsAllSystemsAndUnknownAssignmentsWithoutChangingFiles()
    {
        using var dir=new TestDirectory();var rom=dir.File("game.gba");var cover=dir.File("cover.png");var exe=dir.File("custom/retroarch.exe");var core=dir.File("custom/mgba.dll");var settings=new AppSettings();settings.EmulatorPaths["retroarch"]=exe;settings.CorePaths["GBA"]=core;settings.Covers[rom]=cover;
        var games=new Game[]{new("Same","GBA",rom),new("Same","GBA",Path.Combine(dir.Path,"missing.zip")),new("Unknown","Custom",Path.Combine(dir.Path,"custom.rom"))};var before=Directory.GetFiles(dir.Path,"*",SearchOption.AllDirectories).ToDictionary(f=>f,File.ReadAllBytes);
        var rows=SystemReadiness.Inspect(dir.Path,settings,games);Assert.Equal(48,rows.Count);var gba=Assert.Single(rows,r=>r.System=="GBA");Assert.Equal(2,gba.Files);Assert.Equal(1,gba.Titles);Assert.Equal(1,gba.MissingFiles);Assert.Equal(1,gba.MissingCovers);Assert.False(gba.NeedsSetup);Assert.Equal("Spieldateien prüfen",gba.Status);Assert.False(Assert.Single(rows,r=>r.System=="Custom").Known);Assert.Empty(SystemReadiness.MissingPackages(rows.Where(r=>r.Files>0)));
        Assert.Equal(before.Count,Directory.GetFiles(dir.Path,"*",SearchOption.AllDirectories).Length);foreach(var(f,bytes)in before)Assert.Equal(bytes,File.ReadAllBytes(f));Assert.Equal(exe,settings.EmulatorPaths["retroarch"]);Assert.Equal(core,settings.CorePaths["GBA"]);
    }
    [Fact] public void PlanDeduplicatesSharedPackagesAndNeverInstallsForUnselectedSystems()
    {
        using var dir=new TestDirectory();var settings=new AppSettings();settings.EmulatorPaths["duckstation"]=dir.File("custom/ps1.exe");var rows=SystemReadiness.Inspect(dir.Path,settings,[new("N","NES","missing.nes"),new("S","SNES","missing.sfc"),new("P","PS1","missing.chd"),new("W","Wii","missing.wbfs")]);
        Assert.Equal(new[]{"retroarch","dolphin","retroarch-cores"},SystemReadiness.MissingPackages(rows.Where(r=>r.Files>0)));Assert.Equal(new[]{"retroarch","retroarch-cores"},SystemReadiness.MissingPackages(rows.Where(r=>r.System=="NES")));Assert.Empty(SystemReadiness.MissingPackages(rows.Where(r=>r.System=="PS1")));Assert.DoesNotContain("pcsx2",SystemReadiness.MissingPackages(rows.Where(r=>r.Files>0)));
        settings.EmulatorPaths["retroarch"]=dir.File("custom/retro.exe");rows=SystemReadiness.Inspect(dir.Path,settings,[new("N","NES","missing.nes")]);Assert.Equal(new[]{"retroarch-cores"},SystemReadiness.MissingPackages(rows.Where(r=>r.Files>0)));
    }
    [Fact] public void FirmwareIsSeparateFromDownloadsAndPresentFilesAreNotClaimedAsGameTests()
    {
        using var dir=new TestDirectory();var game=dir.File("game.iso");var settings=new AppSettings();settings.EmulatorPaths["pcsx2"]=dir.File("pcsx2/program.exe");settings.Covers[game]=dir.File("cover.png");var games=new[]{new Game("Game","PS2",game)};
        var row=SystemReadiness.Inspect(dir.Path,settings,games).Single(r=>r.System=="PS2");Assert.Equal("Firmware prüfen",row.Status);Assert.NotNull(row.FirmwareHint);Assert.Empty(SystemReadiness.MissingPackages([row]));
        var firmware=EmulatorSetup.FirmwareDirectory(dir.Path);Directory.CreateDirectory(firmware);File.WriteAllBytes(Path.Combine(firmware,"bios.ROM0"),NativeSurfaceTests.BiosFixture());row=SystemReadiness.Inspect(dir.Path,settings,games).Single(r=>r.System=="PS2");Assert.Null(row.FirmwareHint);Assert.Equal("Voraussetzungen erkannt",row.Status);Assert.False(row.NeedsAttention);
    }
    [Fact] public void CancelledInventoryDoesNotCreateConfigurationOrFirmwareDirectories()
    {
        using var dir=new TestDirectory();using var source=new CancellationTokenSource();source.Cancel();Assert.Throws<OperationCanceledException>(()=>SystemReadiness.Inspect(dir.Path,new(),[],source.Token));Assert.Empty(Directory.GetFileSystemEntries(dir.Path));
    }
    private static void Render(Window window){window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
    private static void Click(Window window,string label)=>window.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string==label).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static async Task Pump(Func<bool> done){await Task.Delay(10);Dispatcher.UIThread.RunJobs();for(var i=0;i<400&&!done();i++){await Task.Delay(10);Dispatcher.UIThread.RunJobs();}Assert.True(done());}
    private static Task Show(MainWindow window)=>(Task)typeof(MainWindow).GetMethod("ShowSystemCenterAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(window,null)!;
    private static MainWindow Open(TestDirectory dir,string theme="Midnight",bool tv=false)
    {
        new SettingsStore(Path.Combine(dir.Path,"settings.json")).Save(new(){Theme=theme,TvMode=tv,OnboardingCompleted=true,AutoDownloadCovers=false,CheckUpdatesOnStartup=false,CheckAppUpdatesOnStartup=false,PreferGerman=false});new LibraryStore(Path.Combine(dir.Path,"library.db")).Upsert([new("Game","GBA",dir.File("game.gba")),new("Disc","PS2",dir.File("disc.iso")),new("Unknown","Custom",dir.File("custom.rom"))]);var window=new MainWindow(dir.Path);window.Show();Render(window);return window;
    }
    [AvaloniaTheory][InlineData("Midnight",1000,false)][InlineData("Classic Light",640,false)][InlineData("Studio",1100,true)]
    public async Task SearchFiltersAndCancelledBatchAreUsableAcrossNativeLayouts(string theme,int width,bool tv)
    {
        using var dir=new TestDirectory();var window=Open(dir,theme,tv);var before=File.ReadAllBytes(Path.Combine(dir.Path,"settings.json"));var shown=Show(window);await Pump(()=>window.OwnedWindows.Any());var dialog=window.OwnedWindows.Single();dialog.Width=width;Render(dialog);
        try
        {
            var list=dialog.GetVisualDescendants().OfType<ListBox>().Single();Assert.Equal(3,list.ItemCount);var query=dialog.GetVisualDescendants().OfType<TextBox>().Single();query.Text="PS2";Render(dialog);Assert.Equal("PS2",Assert.Single(list.Items.Cast<SystemReadinessInfo>()).System);
            var filter=dialog.GetVisualDescendants().OfType<ComboBox>().Single();filter.SelectedIndex=1;query.Text="";Render(dialog);Assert.Equal(48,list.ItemCount);filter.SelectedIndex=0;Render(dialog);list.SelectedItem=list.Items.Cast<SystemReadinessInfo>().Single(r=>r.System=="GBA");Render(dialog);
            Click(dialog,"Ausgewählte Konsole einrichten");await Pump(()=>dialog.OwnedWindows.Any());var preview=dialog.OwnedWindows.Single();Render(preview);Assert.Contains(preview.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text?.Contains("RetroArch-Core-Paket")==true);ControllerUi.Handle(preview,0x2000);await Pump(()=>!dialog.OwnedWindows.Any());Assert.False(Directory.Exists(Path.Combine(dir.Path,"emulators")));Assert.Equal(before,File.ReadAllBytes(Path.Combine(dir.Path,"settings.json")));
            var busy=(Task<bool>)typeof(MainWindow).GetMethod("BusyAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(window,[new Func<CancellationToken,Task>(token=>Task.Delay(Timeout.Infinite,token))])!;Assert.False(dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Ausgewählte Konsole einrichten").IsEnabled);ControllerUi.Handle(dialog,0x2000);Assert.False(await busy);Assert.True(dialog.IsVisible);
            var folder=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(folder!=null){Directory.CreateDirectory(folder);Render(dialog);using var frame=dialog.CaptureRenderedFrame();frame!.Save(Path.Combine(folder,$"System-center-{theme}-{width}.png"));}
            list.Focus();ControllerUi.Handle(dialog,2);Assert.True(list.SelectedIndex>=1);ControllerUi.Handle(dialog,0x2000);await shown.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally{foreach(var child in window.OwnedWindows.ToArray())child.Close();window.Close();}
    }
    [AvaloniaFact] public async Task NestedSettingsReturnToCenterAndLibraryActionUsesChosenSystem()
    {
        using var dir=new TestDirectory();var window=Open(dir);var shown=Show(window);await Pump(()=>window.OwnedWindows.Any());var dialog=window.OwnedWindows.Single();Render(dialog);
        try
        {
            var list=dialog.GetVisualDescendants().OfType<ListBox>().Single();list.SelectedItem=list.Items.Cast<SystemReadinessInfo>().Single(r=>r.System=="GBA");Render(dialog);Click(dialog,"Emulator-Einstellungen öffnen");await Pump(()=>dialog.OwnedWindows.Any());var settings=dialog.OwnedWindows.Single();Assert.Equal(dialog,settings.Owner);settings.Close();await Pump(()=>!dialog.OwnedWindows.Any());await Pump(()=>dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Emulator-Einstellungen öffnen").IsEnabled);
            Click(dialog,"Fehlende Cover ansehen");await shown.WaitAsync(TimeSpan.FromSeconds(5));var systems=(ComboBox)typeof(MainWindow).GetField("systems",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(window)!;Assert.Equal("GBA",systems.SelectedItem);Assert.Single(window.GetVisualDescendants().OfType<ListBox>().Single().Items.Cast<Game>());
        }
        finally{foreach(var child in window.OwnedWindows.ToArray())child.Close();window.Close();}
    }
}
