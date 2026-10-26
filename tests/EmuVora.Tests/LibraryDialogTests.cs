using Avalonia;
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

public class LibraryDialogTests
{
    private static void Render(Window window)
    {
        window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs(); AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    }
    private static Button Button(Window w, string label) => w.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == label);
    private static void Click(Window w, string label) => Button(w, label).RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
    private static async Task<Window> Next(Window owner)
    {
        for (var i = 0; i < 300 && !owner.OwnedWindows.Any(); i++) { Dispatcher.UIThread.RunJobs(); await Task.Delay(10); }
        return Assert.Single(owner.OwnedWindows);
    }
    private static void CheckFooter(Window dialog, string label, int width, int height)
    {
        dialog.Width = width; dialog.Height = height; Render(dialog);
        var button = Button(dialog, label);
        Assert.DoesNotContain(button.GetVisualAncestors(), a => a is ScrollViewer);
        var point = button.TranslatePoint(new(0, 0), dialog)!.Value;
        Assert.True(point.Y >= 0 && point.Y + button.Bounds.Height <= dialog.ClientSize.Height, $"{label}: {point.Y} + {button.Bounds.Height} / {dialog.ClientSize.Height}");
        // The preview can move without moving the decision buttons.
        var body = dialog.GetVisualDescendants().OfType<ScrollViewer>().First(s => s.Parent is Grid g && g.RowDefinitions.Count == 2 && Grid.GetRow(s) == 0);
        body.Offset = new(0, 10000); Render(dialog);
        Assert.Equal(point, button.TranslatePoint(new(0, 0), dialog)!.Value);
        var folder = Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");
        if (folder != null) { Directory.CreateDirectory(folder); using var frame = dialog.CaptureRenderedFrame(); frame!.Save(Path.Combine(folder, $"Library-{label}-{width}-{height}.png")); }
    }
    [AvaloniaTheory]
    [InlineData("Midnight", 760, 640, false)]
    [InlineData("Classic Light", 520, 480, false)]
    [InlineData("Studio", 900, 600, true)]
    public async Task LibraryReviewsKeepDecisionsVisibleAndCancellationPreservesData(string theme, int width, int height, bool tv)
    {
        using var dir = new TestDirectory();
        new SettingsStore(Path.Combine(dir.Path, "settings.json")).Save(new() { Theme = theme, TvMode = tv, OnboardingCompleted = true, AutoDownloadCovers = false, CheckUpdatesOnStartup = false, CheckAppUpdatesOnStartup = false, PreferGerman = false });
        var games = new[] { new Game("Game (Disc 1)", "PS1", dir.File("old/Game (Disc 1).chd", "same")), new Game("Game (Disc 2)", "PS1", dir.File("old/Game (Disc 2).chd", "same")) };
        var store = new LibraryStore(Path.Combine(dir.Path, "library.db")); store.Upsert(games); var before = store.GetAll();
        var window = new MainWindow(dir.Path); window.Show(); Render(window);
        try
        {
            var options = (Expander)typeof(MainWindow).GetMethod("LibraryAuditOptions", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, null)!;
            window.Content = options; options.IsExpanded = true; Render(window);
            Click(window, "Duplikate und Mehrdisc-Spiele prüfen"); var audit = await Next(window);
            CheckFooter(audit, "Mehrdisc-Playlist vorbereiten", width, height);
            audit.GetVisualDescendants().OfType<ListBox>().Single().SelectedIndex = 0;
            Click(audit, "Ausgewählte Dateien per SHA-256 vergleichen"); var hashes = await Next(audit);
            CheckFooter(hashes, "Ausgewählte Duplikate zusammenführen", width, height);
            hashes.GetVisualDescendants().OfType<ListBox>().Single().SelectedIndex = 0;
            Click(hashes, "Ausgewählte Duplikate zusammenführen"); var merge = await Next(hashes);
            CheckFooter(merge, "Geprüfte Einträge zusammenführen", width, height);
            ControllerUi.Handle(merge, 0x2000); await Task.Delay(20); Click(hashes, "Schließen");
            Click(audit, "Mehrdisc-Playlist vorbereiten"); var playlist = await Next(audit);
            CheckFooter(playlist, "Playlist erstellen und hinzufügen", width, height);
            ControllerUi.Handle(playlist, 0x2000); await Task.Delay(20);
            Assert.False(File.Exists(Path.Combine(dir.Path, "old/Game.m3u")));
            var busy = (Task<bool>)typeof(MainWindow).GetMethod("BusyAsync", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, [new Func<CancellationToken, Task>(token => Task.Delay(Timeout.Infinite, token))])!;
            audit.Close(); Assert.False(await busy); Assert.True(audit.IsVisible); Click(audit, "Schließen");
            var relocated = games.Select(g => { var file = dir.File("new/" + Path.GetFileName(g.Path), "same"); File.Delete(g.Path); return file; }).ToArray();
            var plan = RomRelocation.Preview(before, Path.Combine(dir.Path, "old"), Path.Combine(dir.Path, "new"));
            var reviewTask = (Task)typeof(MainWindow).GetMethod("ReviewRomRelocationAsync", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, [plan, new TextBlock(), window])!;
            var relocation = await Next(window); CheckFooter(relocation, "Geprüfte Pfade übernehmen", width, height);
            ControllerUi.Handle(relocation, 0x2000); await reviewTask;
            Assert.Equal(before, store.GetAll()); Assert.All(relocated, p => Assert.Equal("same", File.ReadAllText(p)));
        }
        finally { foreach (var child in window.OwnedWindows.ToArray()) child.Close(); window.Close(); }
    }
}
