using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using EmuVora.Core;
using FluentAvalonia.UI.Controls;
using Xunit;

namespace EmuVora.Tests;

public class AppearanceTests
{
    private static MainWindow Library(TestDirectory dir,string theme,bool tv)
    {
        new SettingsStore(System.IO.Path.Combine(dir.Path,"settings.json")).Save(new(){Theme=theme,TvMode=tv,OnboardingCompleted=true,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false,AutoDownloadCovers=false});
        var library=new LibraryStore(System.IO.Path.Combine(dir.Path,"library.db"));library.Upsert(Enumerable.Range(0,18).Select(i=>new Game($"Spiel {i:00}","SNES",$"D:/fixture/game{i:00}.sfc")));
        var window=new MainWindow(dir.Path){Width=1440,Height=900};window.Show();Render(window);return window;
    }
    private static void Render(Window window){window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
    private static void Capture(Window window,string name)
    {
        Render(window);using var frame=window.CaptureRenderedFrame();Assert.NotNull(frame);var folder=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(folder!=null){Directory.CreateDirectory(folder);frame.Save(System.IO.Path.Combine(folder,name));}
    }
    [AvaloniaFact] public void AllPalettesRenderWithFluentAvaloniaAndTvControls()
    {
        foreach(var theme in new[]{"Midnight","Classic Light","Retro Green","Neon Purple"})
        {
            using var dir=new TestDirectory();var window=Library(dir,theme,true);Assert.Contains("tv-mode",window.Classes);
            var button=window.GetVisualDescendants().OfType<Button>().First(b=>b.Content as string=="Einstellungen");Assert.Equal(18,button.FontSize);Assert.True(button.Bounds.Height>=48);
            Assert.Contains(window.GetVisualDescendants().OfType<StackPanel>(),p=>p.Width==260);Capture(window,"TV-"+theme.Replace(' ','-')+".png");
            if(theme=="Midnight"){window.Width=820;window.Height=600;Capture(window,"TV-compact.png");}
            window.Close();
        }
    }
    [AvaloniaFact] public void TvPreferencePersistsAndPropagatesToDialogsThenReturnsToDesktop()
    {
        using var dir=new TestDirectory();var window=Library(dir,"Midnight",false);window.GetVisualDescendants().OfType<Button>().First(b=>b.Content as string=="Einstellungen").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();var dialog=Assert.Single(window.OwnedWindows);dialog.GetVisualDescendants().OfType<TabControl>().Single().SelectedIndex=2;Render(dialog);
        var tv=dialog.GetVisualDescendants().OfType<CheckBox>().Single(c=>c.Content as string=="TV-Modus · größere Cover und Bedienelemente");tv.IsChecked=true;Render(window);Render(dialog);Assert.True(new SettingsStore(System.IO.Path.Combine(dir.Path,"settings.json")).Load().TvMode);Assert.Contains("tv-mode",dialog.Classes);
        tv.IsChecked=false;Render(window);Assert.DoesNotContain("tv-mode",window.Classes);Assert.DoesNotContain("tv-mode",dialog.Classes);Assert.Contains(window.GetVisualDescendants().OfType<StackPanel>(),p=>p.Width==164);dialog.Close();Capture(window,"Desktop-Fluent.png");window.Close();
    }
    [AvaloniaFact] public async Task ControllerMovesByTheActualNumberOfColumnsForEachCoverSize()
    {
        foreach(var tv in new[]{false,true})
        {
            using var dir=new TestDirectory();var window=Library(dir,"Midnight",tv);var list=window.GetVisualDescendants().OfType<ListBox>().Single(l=>l.ItemCount==18);var items=list.GetVisualDescendants().OfType<ListBoxItem>().ToArray();Assert.True(items.Length>1);var top=items.Min(c=>c.Bounds.Y);var columns=items.Count(c=>Math.Abs(c.Bounds.Y-top)<2);Assert.True(columns>1);list.SelectedIndex=0;await window.HandleLibraryControllerAsync(2);Assert.Equal(columns,list.SelectedIndex);window.Close();
        }
    }
    [AvaloniaFact] public void EmptyAndFilteredLibrariesShowDifferentFluentMessages()
    {
        using var dir=new TestDirectory();new SettingsStore(System.IO.Path.Combine(dir.Path,"settings.json")).Save(new(){OnboardingCompleted=true,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false,AutoDownloadCovers=false});var window=new MainWindow(dir.Path);window.Show();Render(window);var empty=window.GetVisualDescendants().OfType<InfoBar>().Single();Assert.True(empty.IsVisible);Assert.Equal("Deine Bibliothek ist leer",empty.Title);window.Close();
        using var populated=new TestDirectory();window=Library(populated,"Classic Light",false);window.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Watermark as string=="Spiele suchen …").Text="kein passendes Spiel";Render(window);empty=window.GetVisualDescendants().OfType<InfoBar>().Single();Assert.Equal("Keine passenden Spiele",empty.Title);Capture(window,"Library-filter-empty.png");window.Close();
    }
}

