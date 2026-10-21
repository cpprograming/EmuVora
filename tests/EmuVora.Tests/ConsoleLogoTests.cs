using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;

public class ConsoleLogoTests
{
    private static void Render(Window window){window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
    [AvaloniaFact] public void AllSupportedSystemsHaveOfflineLogosAndUnknownSystemsStayDistinct()
    {
        using var logos=new ConsoleLogos();
        foreach(var system in EmulatorCatalog.Systems)
        {
            Assert.NotNull(ConsoleLogos.AssetId(system));
            var content=logos.CreateContent(system,42);
            var image=Assert.Single(content.GetLogicalDescendants().OfType<Image>());Assert.NotNull(image.Source);
            Assert.Equal(new Size(400,96),image.Source.Size);
        }
        Assert.Null(ConsoleLogos.AssetId("Unknown disc"));
        Assert.DoesNotContain(logos.CreateContent("Unknown disc",1).GetLogicalDescendants(),c=>c is Image);
    }
    [AvaloniaTheory][InlineData("Midnight",false)][InlineData("Classic Light",false)][InlineData("Midnight",true)]
    public async Task LogoButtonsKeepCountsAccessibleNamesFilteringAndControllerNavigation(string theme,bool tv)
    {
        using var dir=new TestDirectory();
        new SettingsStore(Path.Combine(dir.Path,"settings.json")).Save(new(){Theme=theme,TvMode=tv,OnboardingCompleted=true,AutoDownloadCovers=false,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false});
        new LibraryStore(Path.Combine(dir.Path,"library.db")).Upsert([new("Fixture A","SNES","D:/fixture/a.sfc"),new("Fixture B","SNES","D:/fixture/b.sfc"),new("Fixture C","PS1","D:/fixture/c.cue")]);
        var window=new MainWindow(dir.Path){Width=1320,Height=820};window.Show();Render(window);
        try
        {
            var buttons=window.GetVisualDescendants().OfType<Button>().Where(b=>b.Classes.Contains("console-filter")).ToArray();Assert.Equal(2,buttons.Length);
            var snes=buttons.Single(b=>b.Tag as string=="SNES");Assert.Equal("SNES - 2 Spiele",AutomationProperties.GetName(snes));Assert.Equal("SNES - 2 Spiele",ToolTip.GetTip(snes));
            Assert.DoesNotContain(snes.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text=="SNES");
            Assert.Contains(snes.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text=="2");
            var image=snes.GetVisualDescendants().OfType<Image>().Single();Assert.Equal(tv?32:24,image.Height);
            snes.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Render(window);var list=window.GetVisualDescendants().OfType<ListBox>().Single();Assert.Equal(2,list.ItemCount);Assert.Contains("selected-console",snes.Classes);
            await window.HandleLibraryControllerAsync(0x100);Render(window);Assert.Equal(1,list.ItemCount);Assert.DoesNotContain("selected-console",snes.Classes);
            var folder=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(folder!=null){Directory.CreateDirectory(folder);using var frame=window.CaptureRenderedFrame();Assert.NotNull(frame);frame.Save(Path.Combine(folder,$"Console-logos-{theme.Replace(' ','-')}-{tv}.png"));}
            window.Width=820;window.Height=600;Render(window);Assert.True(image.Bounds.Width>0);Assert.True(snes.Bounds.Width>0);
        }
        finally{window.Close();}
    }
}
