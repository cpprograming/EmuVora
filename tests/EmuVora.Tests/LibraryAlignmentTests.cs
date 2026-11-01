using System.Reflection;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;

public class LibraryAlignmentTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static T Field<T>(MainWindow w, string name) => (T)typeof(MainWindow).GetField(name, Private)!.GetValue(w)!;
    private static void Render(Window w) { w.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); w.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
    private static readonly string LongTitle = "A very long adventure " + new string('W', 160);
    private static readonly string LongSystem = "Unknown console " + new string('S', 80);
    private static MainWindow Open(TestDirectory dir, string theme, bool tv, string layout, int coverSize = 180)
    {
        var settings = new AppSettings { Theme = theme, TvMode = tv, LibraryLayout = layout, CoverSize = coverSize, OnboardingCompleted = true, PreferGerman = false, AutoDownloadCovers = false, CheckAppUpdatesOnStartup = false, CheckUpdatesOnStartup = false };
        var games = new[] { new Game("A short title", "SNES", "D:/fixture/a.rom", true), new Game(LongTitle, LongSystem, "D:/fixture/b.rom", true), new Game("Missing cover", "GBA", "D:/fixture/c.rom", true), new Game("Broken cover", "PS1", "D:/fixture/d.rom", true) };
        for (var i = 0; i < 2; i++)
        {
            var size = i == 0 ? new PixelSize(120, 240) : new PixelSize(240, 100); var surface = new Border { Width = size.Width, Height = size.Height, Background = i == 0 ? Brushes.Teal : Brushes.Coral };
            surface.Measure(new(size.Width, size.Height)); surface.Arrange(new(0, 0, size.Width, size.Height));
            using var bitmap = new RenderTargetBitmap(size); bitmap.Render(surface); var path = Path.Combine(dir.Path, $"fixture-{i}.png"); bitmap.Save(path); settings.Covers[games[i].Path] = path;
        }
        settings.Covers[games[3].Path] = dir.File("broken.png", "not an image");
        new SettingsStore(Path.Combine(dir.Path, "settings.json")).Save(settings); new LibraryStore(Path.Combine(dir.Path, "library.db")).Restore(games);
        var w = new MainWindow(dir.Path) { Width = tv ? 1400 : 1000, Height = tv ? 1000 : 900 }; w.Show(); Render(w); return w;
    }
    private static void Capture(Window w, string name)
    {
        var output = Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS"); if (output == null) return;
        Directory.CreateDirectory(output); using var frame = w.CaptureRenderedFrame(); frame!.Save(Path.Combine(output, name + ".png"));
    }
    [AvaloniaTheory]
    [InlineData("Midnight", false, "Grid", 120)] [InlineData("Classic Light", false, "Grid", 220)]
    [InlineData("Studio", true, "Grid", 180)] [InlineData("Aurora", false, "Carousel", 180)]
    public void CardsKeepEqualBoundsWithLongTextMissingAndMixedFormatCovers(string theme, bool tv, string layout, int size)
    {
        using var dir = new TestDirectory(); var w = Open(dir, theme, tv, layout, size); var store = new LibraryStore(Path.Combine(dir.Path, "library.db")); var before = store.GetAll(); var settings = File.ReadAllBytes(Path.Combine(dir.Path, "settings.json"));
        try
        {
            var list = Field<ListBox>(w, "gamesList"); var cards = list.GetVisualDescendants().OfType<StackPanel>().Where(c => c.Classes.Contains("library-game-card")).ToArray(); Assert.NotEmpty(cards);
            Assert.Single(cards.Select(c => c.Bounds.Size).Distinct());
            foreach (var card in cards)
            {
                var title = card.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Classes.Contains("game-title")); var caption = card.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Classes.Contains("game-caption"));
                Assert.True(title.Bounds.Width <= card.Bounds.Width); Assert.True(caption.Bounds.Width <= card.Bounds.Width); Assert.Equal(2, title.MaxLines); Assert.Equal(1, caption.MaxLines);
                foreach (var image in card.GetVisualDescendants().OfType<Image>()) Assert.Equal(Stretch.Uniform, image.Stretch);
            }
            list.SelectedIndex = 1; Render(w); var item = list.GetVisualDescendants().OfType<ListBoxItem>().Single(i => i.IsSelected); var bounds = item.Bounds.Size; var selected = list.SelectedItem;
            item.Focus(); Render(w); Assert.Equal(bounds, item.Bounds.Size); Assert.Equal(new Thickness(2), item.BorderThickness); Assert.Equal(w.Resources["EmuAccent"], item.BorderBrush); Assert.Equal(w.Resources["EmuSelection"], item.Background);
            var longCard = list.GetVisualDescendants().OfType<StackPanel>().Single(c => c.Classes.Contains("library-game-card") && (AutomationProperties.GetName(c)?.Contains(LongTitle) ?? false)); Assert.Contains(LongSystem, ToolTip.GetTip(longCard) as string);
            Capture(w, $"Aligned-{theme}-{layout}-{size}"); w.Width = 860; Render(w); Assert.Equal(selected, list.SelectedItem); Assert.Equal(4, list.ItemCount); Assert.Equal(before, store.GetAll()); Assert.Equal(settings, File.ReadAllBytes(Path.Combine(dir.Path, "settings.json")));
        }
        finally { w.Close(); }
    }
    [AvaloniaTheory][InlineData(false)][InlineData(true)]
    public void ListRowsKeepTextAndArtworkInsideTheSelectedRow(bool tv)
    {
        using var dir = new TestDirectory(); var w = Open(dir, "Classic Light", tv, "List");
        try
        {
            var list = Field<ListBox>(w, "gamesList"); var rows = list.GetVisualDescendants().OfType<Grid>().Where(c => c.Classes.Contains("library-list-row")).ToArray(); Assert.Equal(4, rows.Length); Assert.Single(rows.Select(c => c.Bounds.Height).Distinct());
            foreach (var row in rows) foreach (var text in row.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains("game-title") || t.Classes.Contains("game-caption")))
            { var point = text.TranslatePoint(default, row)!.Value; Assert.InRange(point.X + text.Bounds.Width, 1, row.Bounds.Width + 1); Assert.InRange(point.Y + text.Bounds.Height, 1, row.Bounds.Height + 1); }
            Capture(w, $"Aligned-list-{tv}");
        }
        finally { w.Close(); }
    }
    [AvaloniaTheory][InlineData(false)][InlineData(true)]
    public void HomeCardsUseTheSameTitleMetricsAndFocusDoesNotMoveTheirContents(bool tv)
    {
        using var dir = new TestDirectory(); var w = Open(dir, "Midnight", tv, "Grid");
        try
        {
            var title = Field<ListBox>(w, "gamesList").GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("game-title")); var metric = (title.FontSize, title.LineHeight, title.Height);
            typeof(MainWindow).GetMethod("ShowHome", Private)!.Invoke(w, null); Render(w); var cards = w.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("home-game")).ToArray(); Assert.Equal(4, cards.Length); Assert.Single(cards.Select(c => c.Bounds.Height).Distinct());
            foreach (var card in cards) { var text = card.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Classes.Contains("game-title")); Assert.Equal(metric, (text.FontSize, text.LineHeight, text.Height)); }
            var first = cards[0]; var content = (Control)first.Content!; var position = content.TranslatePoint(default, first); var size = first.Bounds.Size; first.Focus(); Render(w); Assert.Equal(position, content.TranslatePoint(default, first)); Assert.Equal(size, first.Bounds.Size); Assert.Equal(w.Resources["EmuAccent"], first.BorderBrush);
            Capture(w, $"Aligned-home-{tv}"); first.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Render(w); Assert.Equal(first.Tag, Assert.IsType<Game>(Field<ListBox>(w, "gamesList").SelectedItem).Path);
        }
        finally { w.Close(); }
    }
    [AvaloniaTheory][InlineData(false)][InlineData(true)]
    public void ConsolePlatesAndCountColumnsStayAlignedWithSixDigitCounts(bool tv)
    {
        using var logos = new ConsoleLogos(); var body = new StackPanel { Width = tv ? 260 : 200 }; var window = new Window { Content = body, Width = 420, Height = 500 }; WindowAppearance.Apply(window); WindowAppearance.SetTvMode(window, tv);
        foreach (var system in new[] { "SNES", "Neo Geo CD", "Arcade (FinalBurn Neo)", LongSystem }) { var button = new Button { Content = logos.CreateContent(system, 123456), HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<Control>((c, _) => c) }; button.Classes.Add("console-filter"); body.Children.Add(button); }
        window.Show(); Render(window);
        try
        {
            var plates = body.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("console-logo-plate")).ToArray(); Assert.Equal(4, plates.Length); Assert.Single(plates.Select(p => p.Bounds.Size).Distinct()); Assert.Equal(tv ? 44 : 36, plates[0].Bounds.Height);
            var counts = body.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains("console-count")).ToArray(); Assert.All(counts, count => { Assert.Equal("123456", count.Text); Assert.True(count.DesiredSize.Width <= count.Bounds.Width); });
            Assert.Contains(body.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == LongSystem); Capture(window, $"Aligned-logos-{tv}");
        }
        finally { window.Close(); }
    }
}
