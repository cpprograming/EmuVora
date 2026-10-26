using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using EmuVora.Core;
using Xunit;
namespace EmuVora.Tests;
public class InputGuideTests
{
    [Fact] public void EverySupportedSystemHasAGuideAndUnknownSystemsAreNotClaimedAsGamepadCompatible()
    {
        var all=InputGuide.All();Assert.Equal(47,all.Count);Assert.Equal(47,all.Select(g=>g.System).Distinct().Count());Assert.All(all,g=>{Assert.NotEmpty(g.Category);Assert.NotEmpty(g.Hint);});Assert.Equal(10,all.Count(g=>g.AdditionalInput));
        var unknown=InputGuide.ForSystem("Unassigned");Assert.Equal("Bedienung ungeklärt",unknown.Category);Assert.Contains("zuordnen",unknown.Hint);Assert.False(unknown.AdditionalInput);
    }
    [Fact] public void SpecialInputsAreExplainedWithoutGuaranteeingPerGameCompatibility()
    {
        Assert.Contains("Touchscreen",InputGuide.Describe("Nintendo DS"));Assert.Contains("Bewegung",InputGuide.Describe("Wii"));Assert.Contains("Tastatur",InputGuide.Describe("DOS"));Assert.Contains("Zifferntasten",InputGuide.Describe("Intellivision"));Assert.Contains("spielabhängig",InputGuide.Describe("NES"));Assert.False(InputGuide.ForSystem("NES").AdditionalInput);
    }
    [AvaloniaTheory][InlineData("Midnight",false)][InlineData("Classic Light",false)][InlineData("Studio",true)]
    public void GuideIsVisibleBeforeLaunchWithHiddenDetailsAndDoesNotChangeMetadata(string theme,bool tv)
    {
        using var dir=new TestDirectory();var settingsFile=Path.Combine(dir.Path,"settings.json");new SettingsStore(settingsFile).Save(new(){Theme=theme,TvMode=tv,ShowGameDetails=false,OnboardingCompleted=true,AutoDownloadCovers=false,CheckUpdatesOnStartup=false,CheckAppUpdatesOnStartup=false,PreferGerman=false});var store=new LibraryStore(Path.Combine(dir.Path,"library.db"));store.Upsert([new("Private DS title","Nintendo DS",dir.File("game.nds")),new("Private NES title","NES",dir.File("game.nes"))]);var before=File.ReadAllBytes(settingsFile);var window=new MainWindow(dir.Path);window.Show();
        void Render(Window w){w.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}Render(window);
        try
        {
            var games=window.GetVisualDescendants().OfType<ListBox>().Single();games.SelectedItem=games.Items.Cast<Game>().Single(g=>g.System=="Nintendo DS");Render(window);var button=window.GetVisualDescendants().OfType<Button>().Single(b=>(b.Content as string)?.StartsWith("Bedienung: ")==true);Assert.True(button.IsEffectivelyVisible);button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));var dialog=Assert.Single(window.OwnedWindows);Render(dialog);
            var list=dialog.GetVisualDescendants().OfType<ListBox>().Single();Assert.Equal(10,list.ItemCount);Assert.Equal("Nintendo DS",((SystemInputGuide)list.SelectedItem!).System);var search=dialog.GetVisualDescendants().OfType<TextBox>().Single();search.Text="Wii";Render(dialog);Assert.Single(list.Items.Cast<SystemInputGuide>());Assert.Contains(dialog.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text?.Contains("Bewegungsspiele")==true);
            var folder=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(folder!=null){Directory.CreateDirectory(folder);using var frame=dialog.CaptureRenderedFrame();frame!.Save(Path.Combine(folder,$"Input-guide-{theme}.png"));}
            search.Text="";dialog.GetVisualDescendants().OfType<CheckBox>().Single().IsChecked=false;Render(dialog);Assert.Equal(47,list.ItemCount);ControllerUi.Handle(dialog,0x2000);Assert.Empty(window.OwnedWindows);games.SelectedItem=games.Items.Cast<Game>().Single(g=>g.System=="NES");Render(window);Assert.False(button.IsVisible);Assert.Equal(before,File.ReadAllBytes(settingsFile));Assert.Equal(2,store.GetAll().Count);Assert.Contains(store.GetAll(),g=>g.Title=="Private DS title");
        }
        finally{foreach(var child in window.OwnedWindows.ToArray())child.Close();window.Close();}
    }
}
