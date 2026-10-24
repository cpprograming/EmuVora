using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using EmuVora.Core;
using System.Buffers.Binary;
using System.Reflection;
using Xunit;

namespace EmuVora.Tests;
public class NativeSurfaceTests
{
    public static byte[] BiosFixture()
    {
        var bytes=new byte[4*1024*1024];void Entry(int offset,string name,uint size){System.Text.Encoding.ASCII.GetBytes(name).CopyTo(bytes,offset);BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset+12),size);}
        Entry(0x100,"RESET",256);Entry(0x110,"ROMDIR",64);Entry(0x120,"ROMVER",14);System.Text.Encoding.ASCII.GetBytes("0220EC20060210").CopyTo(bytes,320);return bytes;
    }
    [Fact] public void Ps2BiosRejectsSameSizedCompanionAndSelectsValidatedImage()
    {
        using var dir=new TestDirectory();var root=EmulatorSetup.FirmwareDirectory(dir.Path);Directory.CreateDirectory(root);var bad=Path.Combine(root,"a.DIFF");File.WriteAllBytes(bad,new byte[4*1024*1024]);Assert.False(Ps2Bios.IsImage(bad));Assert.NotNull(EmulatorSetup.FirmwareHint("PS2",dir.Path));
        var good=Path.Combine(root,"b.ROM0");File.WriteAllBytes(good,BiosFixture());Assert.True(Ps2Bios.IsImage(good));Assert.Null(EmulatorSetup.FirmwareHint("PS2",dir.Path));var exe=dir.File("pcsx2/program.exe");EmulatorSetup.Configure("pcsx2",exe,dir.Path);Assert.Contains("BIOS = b.ROM0",File.ReadAllText(Path.Combine(dir.Path,"pcsx2/inis/PCSX2.ini")));
    }
    [Fact] public void GroupingKeepsDistinctRegionsSystemsAndActualMetadata()
    {
        var a=new Game("Same","GBA","a.gba",true);var b=new Game("same","GBA","b.zip",false,null,7);var c=new Game("Same (USA)","GBA","c.gba");var d=new Game("Same","GBC","d.gbc");
        var groups=GameVariants.Group([a,b,c,d],b.Path);Assert.Equal(3,groups.Count);var family=Assert.Single(groups,g=>g.Versions.Count==2);Assert.Equal(b,family.Representative);Assert.Contains(a,family.Versions);Assert.Contains(b,family.Versions);
    }
    private static void Render(Window window){window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
    [AvaloniaTheory][InlineData("Aurora",false)][InlineData("Studio",true)]
    public async Task NativeLibraryGroupsVersionsAndKeepsEqualConsoleRows(string theme,bool tv)
    {
        using var dir=new TestDirectory();new SettingsStore(Path.Combine(dir.Path,"settings.json")).Save(new(){Theme=theme,TvMode=tv,OnboardingCompleted=true,AutoDownloadCovers=false,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false,StartInEsDe=true});var store=new LibraryStore(Path.Combine(dir.Path,"library.db"));store.Upsert([new("Same","GBC","a.gbc"),new("Same","GBC","b.zip"),new("Other","GBA","c.gba"),new("Third","GameCube","d.iso")]);var window=new MainWindow(dir.Path);window.Show();Render(window);
        try
        {
            var list=window.GetVisualDescendants().OfType<ListBox>().Single();Assert.Equal(3,list.ItemCount);list.SelectedItem=list.Items.Cast<Game>().Single(g=>g.Title=="Same");Render(window);
            var task=(Task<Game?>)typeof(MainWindow).GetMethod("ChooseVersionAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(window,[list.SelectedItem])!;var dialog=Assert.Single(window.OwnedWindows);Render(dialog);Assert.Equal(2,dialog.GetVisualDescendants().OfType<ListBox>().Single().ItemCount);dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Abbrechen").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Assert.Null(await task);Assert.Equal(4,store.GetAll().Count);
            var buttons=window.GetVisualDescendants().OfType<Button>().Where(b=>b.Classes.Contains("console-filter")).ToArray();Assert.Single(buttons.Select(b=>b.Bounds.Height).Distinct());var badges=buttons.Select(b=>b.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.Classes.Contains("console-count"))).ToArray();Assert.Single(badges.Select(b=>b.Bounds.Width).Distinct());
            Assert.DoesNotContain(window.GetVisualDescendants().OfType<Button>(),b=>(b.Content as string)?.Contains("ES-DE öffnen")==true);
            var folder=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(folder!=null){Directory.CreateDirectory(folder);using var frame=window.CaptureRenderedFrame();frame!.Save(Path.Combine(folder,$"Native-library-{theme}.png"));}
        }
        finally{window.Close();}
    }
    [Fact] public void EsDeAdapterSelectsOneVariantAndDarkSchemeAndPreservesSource()
    {
        using var dir=new TestDirectory();dir.File("theme/capabilities.xml","<themeCapabilities><colorScheme name='light'/><colorScheme name='dark'/><variant name='first'/><variant name='second'/><aspectRatio>16:9</aspectRatio></themeCapabilities>");
        var xml="<theme><colorScheme name='dark'><variables><fg>EEEEEE</fg></variables></colorScheme><colorScheme name='light'><variables><fg>111111</fg></variables></colorScheme><variant name='first'><view name='gamelist'><textlist name='game-list'><primaryColor>${fg}</primaryColor></textlist><image name='system-logo'><path>logo.svg</path></image><video name='unsupported'/></view></variant><variant name='second'><view name='gamelist'><textlist name='game-list'><primaryColor>FF0000</primaryColor></textlist></view></variant></theme>";
        dir.File("theme/theme.xml",xml);dir.File("theme/logo.svg","<svg xmlns='http://www.w3.org/2000/svg' width='10' height='10'/>");var imported=EmulationStationTheme.Import(Path.Combine(dir.Path,"theme"),Path.Combine(dir.Path,"data"));var loaded=EmulationStationTheme.Load(imported,"GBA");Assert.Equal("EEEEEE",loaded.Elements.Single(e=>e.Name=="gamelist").Properties["primaryColor"]);Assert.Contains(loaded.Elements,e=>e.Name=="logo");Assert.Equal(xml,File.ReadAllText(Path.Combine(dir.Path,"theme/theme.xml")));Assert.Contains("ES-DE",File.ReadAllText(Path.Combine(imported,"emuvora-import-report.txt")));
    }
    [Theory][InlineData("<include>../outside.xml</include>")][InlineData("<view name='gamelist'><image name='background'><path>https://example.com/a.png</path></image></view>")]
    public void EsDeAdapterStillRejectsUnsafePaths(string body)
    {
        using var dir=new TestDirectory();dir.File("theme/capabilities.xml","<themeCapabilities/>");dir.File("theme/theme.xml","<theme>"+body+"</theme>");Assert.Throws<IOException>(()=>EmulationStationTheme.Load(Path.Combine(dir.Path,"theme"),"NES"));
    }
    [AvaloniaFact] public void ConsolePlateWidthsStayEqualAcrossCountsAndSelection()
    {
        using var logos=new ConsoleLogos();var panel=new StackPanel();var window=new Window{Width=240,Height=400,Content=panel};WindowAppearance.Apply(window);
        foreach(var (system,count) in new[]{("GB",1),("GBC",19),("GameCube",423),("SNES",1000)}){var b=new Button{Content=logos.CreateContent(system,count),HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Stretch,HorizontalContentAlignment=Avalonia.Layout.HorizontalAlignment.Stretch};b.Classes.Add("console-filter");panel.Children.Add(b);}
        window.Show();Render(window);
        try{var widths=panel.Children.Cast<Button>().Select(b=>((Border)((Grid)b.Content!).Children[0]).Bounds.Width).ToArray();Assert.Single(widths.Distinct());panel.Children.Cast<Button>().First().Classes.Add("selected-console");Render(window);Assert.Equal(widths[0],((Border)((Grid)panel.Children.Cast<Button>().First().Content!).Children[0]).Bounds.Width);}
        finally{window.Close();}
    }
}
