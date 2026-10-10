using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Avalonia.Threading;
using EmuVora.App;
using EmuVora.Core;
using Xunit;
[assembly: AvaloniaTestApplication(typeof(EmuVora.Tests.TestAppBuilder))]
[assembly: CollectionBehavior(DisableTestParallelization=true)]
namespace EmuVora.Tests;
public sealed class TestApplication:Application
{public override void Initialize()=>Styles.Add(new FluentTheme());}
public class TestAppBuilder
{public static AppBuilder BuildAvaloniaApp()=>AppBuilder.Configure<TestApplication>().UseSkia().UseHeadless(new(){UseHeadlessDrawing=false});}
public class InterfaceTests
{
    [AvaloniaFact] public void SearchConsoleAndFavoritesWorkTogetherAndSelectionIsStable()
    {
        using var dir=new TestDirectory();new SettingsStore(System.IO.Path.Combine(dir.Path,"settings.json")).Save(new(){CheckUpdatesOnStartup=false});
        var store=new LibraryStore(System.IO.Path.Combine(dir.Path,"library.db"));store.Restore([new("Mario","NES",dir.File("Mario.nes"),true),new("Zelda","SNES",dir.File("Zelda.sfc")),new("Racing","PS2",dir.File("Racing.iso"))]);
        var window=new MainWindow(dir.Path);window.Show();var list=window.GetVisualDescendants().OfType<ListBox>().Single();Assert.Equal(3,list.ItemCount);
        var console=window.GetVisualDescendants().OfType<ComboBox>().First(c=>c.Items.Contains("Alle Konsolen"));console.SelectedItem="NES";Assert.Equal("Mario",Assert.IsType<Game>(Assert.Single(list.Items)).Title);
        list.SelectedIndex=0;var search=window.GetVisualDescendants().OfType<TextBox>().First(t=>t.Watermark=="Spiele suchen …");search.Text="Mario";Dispatcher.UIThread.RunJobs();Assert.Equal("Mario",Assert.IsType<Game>(list.SelectedItem).Title);
        search.Text="Zelda";Dispatcher.UIThread.RunJobs();Assert.Empty(list.Items);Assert.Null(list.SelectedItem);search.Text="";Dispatcher.UIThread.RunJobs();console.SelectedItem="Alle Konsolen";
        var favorites=window.GetVisualDescendants().OfType<Button>().First(b=>b.Content as string=="Favoriten");favorites.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Assert.Single(list.Items);
        window.Close();
    }
    [AvaloniaTheory][InlineData("Midnight")][InlineData("Classic Light")][InlineData("Retro Green")][InlineData("Neon Purple")]
    public void RendersLibraryAndDetailsWithoutOverflow(string theme)
    {
        using var dir=new TestDirectory();new SettingsStore(System.IO.Path.Combine(dir.Path,"settings.json")).Save(new(){Theme=theme,CheckUpdatesOnStartup=false});
        new LibraryStore(System.IO.Path.Combine(dir.Path,"library.db")).Restore([new("Super Mario World","SNES",dir.File("Mario.sfc"),true),new("The Legend of Zelda","NES",dir.File("Zelda.nes")),new("Gran Turismo","PS1",dir.File("GT.chd"))]);
        var window=new MainWindow(dir.Path);window.Show();var list=window.GetVisualDescendants().OfType<ListBox>().Single();list.SelectedIndex=0;
        using var frame=window.CaptureRenderedFrame();Assert.NotNull(frame);Assert.True(frame.PixelSize.Width>=1000);
        var screenshots=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(screenshots!=null){Directory.CreateDirectory(screenshots);frame.Save(System.IO.Path.Combine(screenshots,theme.Replace(' ','-')+".png"));}
        var play=window.GetVisualDescendants().OfType<Button>().First(b=>b.Content as string=="▶ Spielen");Assert.True(play.IsEnabled);window.Close();
    }
}
