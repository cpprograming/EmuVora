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

public class LibraryStabilityTests
{
    private static void Render(Window window)
    {
        window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    }

    [AvaloniaFact]
    public async Task SixThousandGamesRemainBoundedAndReachableByControllerAndSearch()
    {
        using var dir=new TestDirectory();
        var settings=new AppSettings{OnboardingCompleted=true,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false,AutoDownloadCovers=false};
        var games=Enumerable.Range(0,6000).Select(i=>new Game($"Fixture {i:0000}","SNES",$"D:/fixture/{i:0000}.sfc")).ToArray();
        using(var surface=SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(32,48)))
        {
            surface.Canvas.Clear(SkiaSharp.SKColors.Teal);using var picture=surface.Snapshot();using var png=picture.Encode(SkiaSharp.SKEncodedImageFormat.Png,100);
            for(var i=0;i<96;i++)File.WriteAllBytes(Path.Combine(dir.Path,$"cover{i}.png"),png.ToArray());
        }
        for(var i=0;i<games.Length;i++)settings.Covers[games[i].Path]=Path.Combine(dir.Path,$"cover{i%96}.png");
        new SettingsStore(Path.Combine(dir.Path,"settings.json")).Save(settings);
        new LibraryStore(Path.Combine(dir.Path,"library.db")).Upsert(games);
        var window=new MainWindow(dir.Path);window.Show();Render(window);
        try
        {
            var list=window.GetVisualDescendants().OfType<ListBox>().Single();
            Assert.Equal(48,list.ItemCount);Assert.InRange(list.GetVisualDescendants().OfType<ListBoxItem>().Count(),1,48);
            list.SelectedIndex=47;await window.HandleLibraryControllerAsync(8);Render(window);
            Assert.Equal(games[48].Path,Assert.IsType<Game>(list.SelectedItem).Path);
            await window.HandleLibraryControllerAsync(4);Render(window);
            Assert.Equal(games[47].Path,Assert.IsType<Game>(list.SelectedItem).Path);
            var next=window.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Nächste Seite");
            for(var i=0;i<124;i++)next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Render(window);Assert.Equal(48,list.ItemCount);Assert.False(next.IsEnabled);
            var cache=(System.Collections.IDictionary)typeof(MainWindow).GetField("coverImages",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(window)!;
            Assert.InRange(cache.Count,1,48);
            list.SelectedIndex=47;Assert.Equal(games[5999].Path,Assert.IsType<Game>(list.SelectedItem).Path);
            window.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Watermark as string=="Spiele suchen …").Text="Fixture 0000";
            Render(window);Assert.Single(list.Items);Assert.Equal(games[0].Path,Assert.IsType<Game>(list.Items[0]).Path);
        }
        finally{window.Close();}
    }
}
