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
    [AvaloniaFact] public void NativeGameLaunchFailureShowsSetupAndReturnActions()
    {
        using var dir=new TestDirectory();var game=new Game("Unavailable","PS1",System.IO.Path.Combine(dir.Path,"missing.chd"));new LibraryStore(dir.File("library.db","")).Restore([game]);
        var window=new GameLaunchWindow(new EsDeLaunchRequest(EsDeFrontend.GameId(game),dir.Path));window.Show();Dispatcher.UIThread.RunJobs();
        Assert.Contains(window.GetVisualDescendants().OfType<Button>(),b=>b.Content as string=="EmuVora-Einrichtung öffnen"&&b.IsVisible);
        Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text?.Contains("ROM-Datei ist nicht erreichbar")==true);
        using var frame=window.CaptureRenderedFrame();Assert.NotNull(frame);var screenshots=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(screenshots!=null){Directory.CreateDirectory(screenshots);frame.Save(System.IO.Path.Combine(screenshots,"ES-DE-launch-error.png"));}window.Close();
    }
    [AvaloniaFact] public void FirstStartShowsFolderChoiceAndRenders()
    {
        using var dir=new TestDirectory();var window=new LocationWindow(new DataLocation(System.IO.Path.Combine(dir.Path,"bootstrap.json")),_=>{},dir.Path);window.Show();
        Assert.Contains(window.GetVisualDescendants().OfType<Button>(),b=>b.Content as string=="Festplatte / Ordner auswählen …");
        Assert.Contains(window.GetVisualDescendants().OfType<TextBox>(),t=>t.Text==dir.Path);
        using var frame=window.CaptureRenderedFrame();Assert.NotNull(frame);var screenshots=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(screenshots!=null){Directory.CreateDirectory(screenshots);frame.Save(System.IO.Path.Combine(screenshots,"Storage-choice.png"));}window.Close();
    }
    [AvaloniaFact] public void ImportedThemeChangesColorAndSvgLogoWithConsoleFilter()
    {
        using var dir=new TestDirectory();var theme=System.IO.Path.Combine(dir.Path,"theme");
        dir.File("theme/nes/theme.xml","<theme><formatVersion>3</formatVersion><view name='detailed'><textlist name='gamelist'><primaryColor>FFCC00</primaryColor></textlist><image name='logo'><path>logo.svg</path></image></view></theme>");
        dir.File("theme/nes/logo.svg","<svg xmlns='http://www.w3.org/2000/svg' width='160' height='50'><rect width='160' height='50' rx='8' fill='#e43131'/><text x='30' y='35' fill='white' font-size='30'>NES</text></svg>");
        dir.File("theme/snes/theme.xml","<theme><formatVersion>3</formatVersion><view name='detailed'><textlist name='gamelist'><primaryColor>00FF00</primaryColor></textlist></view></theme>");
        new SettingsStore(System.IO.Path.Combine(dir.Path,"settings.json")).Save(new(){EmulationStationTheme=theme,CheckUpdatesOnStartup=false});
        new LibraryStore(System.IO.Path.Combine(dir.Path,"library.db")).Restore([new("Mario","NES",dir.File("Mario.nes")),new("Zelda","SNES",dir.File("Zelda.sfc"))]);
        var window=new MainWindow(dir.Path);window.Show();var list=window.GetVisualDescendants().OfType<ListBox>().Single();var console=window.GetVisualDescendants().OfType<ComboBox>().First(c=>c.Items.Contains("Alle Konsolen"));console.SelectedItem="NES";
        Assert.Equal(Avalonia.Media.Color.Parse("#FFCC00"),Assert.IsType<Avalonia.Media.SolidColorBrush>(list.Foreground).Color);Assert.Contains(window.GetVisualDescendants().OfType<Image>(),i=>i.Source!=null&&i.Height==70);
        using var frame=window.CaptureRenderedFrame();Assert.NotNull(frame);var screenshots=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(screenshots!=null){Directory.CreateDirectory(screenshots);frame.Save(System.IO.Path.Combine(screenshots,"ES-theme.png"));}
        console.SelectedItem="SNES";Assert.Equal(Avalonia.Media.Colors.Lime,Assert.IsType<Avalonia.Media.SolidColorBrush>(list.Foreground).Color);Assert.DoesNotContain(window.GetVisualDescendants().OfType<Image>(),i=>i.Source!=null&&i.Height==70);window.Close();
    }
    [AvaloniaFact] public void AutomaticSetupAndXboxOptionsAreVisibleInSettings()
    {
        using var dir=new TestDirectory();new SettingsStore(System.IO.Path.Combine(dir.Path,"settings.json")).Save(new(){CheckUpdatesOnStartup=false});
        var window=new MainWindow(dir.Path);window.Show();var settings=window.GetVisualDescendants().OfType<Button>().First(b=>b.Content as string=="Emulatoren einrichten");settings.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();
        var dialog=Assert.Single(window.OwnedWindows);var controls=dialog.GetVisualDescendants().OfType<CheckBox>().ToArray();Assert.Contains(controls,c=>c.Content as string=="Beim Spielen fehlende Emulatoren und Cores automatisch einrichten"&&c.IsChecked==true);Assert.Contains(controls,c=>c.Content as string=="Xbox-Standardbelegung bei neuer Einrichtung verwenden"&&c.IsChecked==true);
        using var frame=dialog.CaptureRenderedFrame();Assert.NotNull(frame);var screenshots=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(screenshots!=null){Directory.CreateDirectory(screenshots);frame.Save(System.IO.Path.Combine(screenshots,"Automatic-setup.png"));}
        dialog.Close();window.Close();
    }
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
