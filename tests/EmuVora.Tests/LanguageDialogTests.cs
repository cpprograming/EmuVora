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
public class LanguageDialogTests
{
    [Theory][InlineData("retroarch","retroarch.cfg","user_language = 4")][InlineData("duckstation","settings.ini","Language = de")][InlineData("pcsx2","inis/PCSX2.ini","Language = de-DE")][InlineData("dolphin","User/Config/Dolphin.ini","LanguageCode = de")][InlineData("ppsspp","memstick/PSP/SYSTEM/ppsspp.ini","Language = de_DE")]
    public void GermanProfileChangesOnlyLanguageAndRetainsOriginalBackup(string id,string relative,string expected)
    {
        using var dir=new TestDirectory();var exe=dir.File("emulator/program.exe");var file=dir.File("emulator/"+relative,"; original\n[Unrelated]\nCustom = keep\n");
        EmulatorLanguage.Apply(id,exe);var first=File.ReadAllText(file);Assert.Contains(expected,first);Assert.Contains("Custom = keep",first);Assert.Equal("; original\n[Unrelated]\nCustom = keep\n",File.ReadAllText(file+".before-german.bak"));
        EmulatorLanguage.Apply(id,exe);Assert.Equal(first,File.ReadAllText(file));
        if(id=="retroarch"){var cores=File.ReadAllText(Path.Combine(dir.Path,"emulator/retroarch-core-options.cfg"));Assert.Contains("flycast_language = \"German\"",cores);Assert.Contains("desmume_firmware_language = \"German\"",cores);}
        if(id=="dolphin")Assert.Contains("SelectedLanguage = 1",first);
        if(id=="ppsspp")Assert.Contains("GameLanguage = 4",first);
        if(id=="pcsx2")Assert.DoesNotContain("BIOS =",first);
    }
    [Fact] public void PreferenceDefaultsToGermanAndCanBeDisabledWithoutResettingOtherSettings()
    {
        using var dir=new TestDirectory();var store=new SettingsStore(Path.Combine(dir.Path,"settings.json"));Assert.True(store.Load().PreferGerman);store.Save(new(){PreferGerman=false});Assert.False(store.Load().PreferGerman);
        var exe=dir.File("emulator/program.exe");var cfg=dir.File("emulator/settings.ini","[Main]\nLanguage = en\n");EmulatorSetup.Configure("duckstation",exe,dir.Path,preferGerman:false);Assert.Contains("Language = en",File.ReadAllText(cfg));
        EmulatorLanguage.Apply("duckstation",exe);Assert.Contains("Language = de",File.ReadAllText(cfg));Assert.DoesNotContain("Language = en",File.ReadAllText(cfg));
    }
    [Fact] public void WiiUsesDocumentedTemporaryLanguageOverrideWithoutModifyingNand()
    {
        using var dir=new TestDirectory();var exe=dir.File("Dolphin.exe");var game=new Game("Game","Wii",dir.File("game with spaces.iso"));var adapter=new ExternalEmulatorAdapter(EmulatorCatalog.Get("dolphin"));
        var args=adapter.CreateStartInfo(game,exe,preferGerman:true).ArgumentList;Assert.Equal(new[]{"-C","SYSCONF.IPL.LNG=2","-b","-e",game.Path},args);
        Assert.DoesNotContain("-C",adapter.CreateStartInfo(game,exe).ArgumentList);Assert.DoesNotContain("-C",adapter.CreateStartInfo(game with{System="GameCube"},exe,preferGerman:true).ArgumentList);
    }
    private static object? Call(MainWindow window,string method,params object?[] args)=>typeof(MainWindow).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(window,args);
    private static void Render(Window w){w.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();}
    [AvaloniaTheory][InlineData("bios")][InlineData("missing-rom")][InlineData("error")]
    public async Task BlockingLaunchProblemsShowVisibleActionableDialogs(string kind)
    {
        using var dir=new TestDirectory();new SettingsStore(Path.Combine(dir.Path,"settings.json")).Save(new(){OnboardingCompleted=true,AutoDownloadCovers=false,CheckUpdatesOnStartup=false,CheckAppUpdatesOnStartup=false});
        if(kind=="missing-rom")new LibraryStore(Path.Combine(dir.Path,"library.db")).Upsert([new("Missing","PS2",Path.Combine(dir.Path,"missing.iso"))]);
        var window=new MainWindow(dir.Path);window.Show();Render(window);Task? task=null;
        try
        {
            if(kind=="bios")task=(Task)Call(window,"ShowFirmwareRequiredAsync","PS2","PS2: gültiges Haupt-BIOS importieren.","pcsx2")!;
            else if(kind=="missing-rom"){var list=window.GetVisualDescendants().OfType<ListBox>().Single();list.SelectedItem=list.Items.Cast<Game>().Single();await (Task)Call(window,"PlayAsync")!;}
            else{Call(window,"ShowError",new IOException("Emulatorstart fehlgeschlagen"));Call(window,"ShowError",new IOException("Aktualisierte Fehlerdetails"));}
            var dialog=Assert.Single(window.OwnedWindows);Render(dialog);Assert.True(dialog.IsVisible);
            if(kind=="bios"){Assert.Contains(dialog.GetVisualDescendants().OfType<Button>(),b=>b.Content as string=="BIOS-Dateien importieren …");Assert.Contains(dialog.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text?.Contains("nicht gestartet")==true);}
            else Assert.Contains(dialog.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text?.Contains(kind=="error"?"Aktualisierte Fehlerdetails":"ROM-Datei nicht gefunden")==true);
            var output=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(output!=null){Directory.CreateDirectory(output);using var frame=dialog.CaptureRenderedFrame();frame!.Save(Path.Combine(output,"Launch-"+kind+".png"));}
            dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Schließen").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));if(task!=null)await task;Assert.Empty(window.OwnedWindows);
        }
        finally{foreach(var child in window.OwnedWindows.ToArray())child.Close();window.Close();}
    }
}
