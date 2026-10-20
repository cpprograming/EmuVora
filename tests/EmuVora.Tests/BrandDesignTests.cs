using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;

public class BrandDesignTests
{
    // Original geometric demo artwork, created only for rendering checks, never shipped as game covers.
    private static MainWindow Demo(TestDirectory dir,string theme)
    {
        var titles=new[]{"Astral Drift","Forest of Echoes","Neon Rally","Lunar Atlas","Pocket Odyssey","Skybound","Crystal Coast","Afterlight","Last Horizon","Velocity"};
        var consoles=new[]{"SNES","GBA","PS1","NES","GB","Mega Drive","SNES","GBA","PS1","NES"};
        var colors=new[]{"#E6B380","#7CB7A0","#D58C9B","#959CCB","#B8C589","#8CCAD3","#D6A4C8","#DA9E80","#9CB7DD","#BBC893"};
        var settings=new AppSettings{Theme=theme,OnboardingCompleted=true,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false,AutoDownloadCovers=false,CoverSize=180};
        var games=new List<Game>();
        for(var i=0;i<titles.Length;i++)
        {
            var rom=dir.File($"roms/demo{i}.sfc");games.Add(new(titles[i],consoles[i],rom,i==1));
            var svg=dir.File($"art/demo{i}.svg",$"""
            <svg xmlns="http://www.w3.org/2000/svg" width="400" height="536" viewBox="0 0 400 536">
              <rect width="400" height="536" fill="{colors[i]}"/>
              <circle cx="{120+i*13}" cy="215" r="100" fill="#F5EFDC" opacity=".8"/>
              <path d="M0 400 L155 180 L330 400 L400 270 L400 536 L0 536Z" fill="#263C48"/>
              <path d="M0 445 L155 290 L325 445 L400 355 L400 536 L0 536Z" fill="#152731" opacity=".9"/>
              <text x="28" y="42" font-family="Segoe UI" font-size="12" letter-spacing="4" fill="#182E39">EMUVORA / DEMO</text>
              <text x="28" y="468" font-family="Segoe UI" font-weight="bold" font-size="29" fill="#F9F5E9">{titles[i]}</text>
              <text x="28" y="501" font-family="Segoe UI" font-size="13" letter-spacing="3" fill="{colors[i]}">{consoles[i]}</text>
            </svg>
            """);
            var png=System.IO.Path.ChangeExtension(svg,".png");using(var artwork=ThemeArtwork.Load(svg))artwork.Save(png);settings.Covers[rom]=png;
        }
        new SettingsStore(System.IO.Path.Combine(dir.Path,"settings.json")).Save(settings);new LibraryStore(System.IO.Path.Combine(dir.Path,"library.db")).Upsert(games);
        var window=new MainWindow(dir.Path){Width=1600,Height=1000};window.Show();Render(window);window.GetVisualDescendants().OfType<ListBox>().Single().SelectedIndex=0;return window;
    }
    private static void Render(Window window){window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
    private static void Capture(Window window,string name)
    {
        Render(window);Thread.Sleep(120);Render(window);using var frame=window.CaptureRenderedFrame();Assert.NotNull(frame);var folder=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(folder!=null){Directory.CreateDirectory(folder);frame.Save(System.IO.Path.Combine(folder,name));}
    }
    [AvaloniaTheory][InlineData("Midnight")][InlineData("Classic Light")]
    public void CoversAndPrimaryActionsFitDesktopAndCompactLibrary(string theme)
    {
        using var dir=new TestDirectory();var window=Demo(dir,theme);Assert.NotNull(window.Icon);
        var play=window.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="▶ Spielen");Assert.True(play.IsEnabled);Assert.True(play.Bounds.Height>=46);
        Assert.Contains(window.GetVisualDescendants().OfType<Image>(),i=>i.Width==40&&i.Source!=null);
        var list=window.GetVisualDescendants().OfType<ListBox>().Single();Assert.Equal(10,list.ItemCount);
        Capture(window,"Brand-library-"+theme.Replace(' ','-')+".png");
        window.MouseMove(play.TranslatePoint(new Point(play.Bounds.Width/2,play.Bounds.Height/2),window)!.Value);Render(window);Thread.Sleep(120);Render(window);
        var presenter=play.GetVisualDescendants().OfType<ContentPresenter>().Single();
        var background=Assert.IsAssignableFrom<ISolidColorBrush>(presenter.Background).Color;var foreground=Assert.IsAssignableFrom<ISolidColorBrush>(presenter.Foreground).Color;
        static double Luminance(Color c){static double Channel(byte v){var s=v/255d;return s<=0.04045?s/12.92:Math.Pow((s+0.055)/1.055,2.4);}return 0.2126*Channel(c.R)+0.7152*Channel(c.G)+0.0722*Channel(c.B);}
        var light=Math.Max(Luminance(background),Luminance(foreground));var dark=Math.Min(Luminance(background),Luminance(foreground));Assert.True((light+0.05)/(dark+0.05)>=4.5,"Play text needs readable hover contrast.");
        window.Width=820;window.Height=600;Render(window);var search=window.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Watermark as string=="Spiele suchen …");
        Assert.True(search.Bounds.Width>250);Assert.True(list.Bounds.Width>400);Capture(window,"Brand-compact-"+theme.Replace(' ','-')+".png");window.Close();
    }
    [AvaloniaFact] public void FilterResetRestoresAllGamesAndManagementStillOpensWizard()
    {
        using var dir=new TestDirectory();var window=Demo(dir,"Midnight");var search=window.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Watermark as string=="Spiele suchen …");search.Text="no matching title";Render(window);
        var list=window.GetVisualDescendants().OfType<ListBox>().Single();Assert.Equal(0,list.ItemCount);
        window.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Filter zurücksetzen").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Assert.Equal(10,list.ItemCount);Assert.Equal("",search.Text);
        window.GetVisualDescendants().OfType<Expander>().Single(e=>e.Header as string=="Verwaltung").IsExpanded=true;Render(window);
        window.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Einrichtungsassistent").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();var wizard=Assert.Single(window.OwnedWindows);Assert.Contains("Einrichtung",wizard.Title);wizard.Close();window.Close();
    }
}
