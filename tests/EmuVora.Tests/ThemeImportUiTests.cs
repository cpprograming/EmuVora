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

public class ThemeImportUiTests
{
    private static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static void Render(Window w) { w.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); w.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
    private static async Task Pump(Func<bool> ready) { for (var i = 0; i < 600 && !ready(); i++) { Dispatcher.UIThread.RunJobs(); await Task.Delay(10); } Assert.True(ready()); }
    private static Button Button(Window w, string text) => w.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == text);
    private static void Click(Window w, string text) => Button(w, text).RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
    private static Task Show(MainWindow window, string source) => (Task)typeof(MainWindow).GetMethod("ShowThemeImportAsync", Private)!.Invoke(window, [source])!;
    private static async Task<Window> Next(MainWindow window) { await Pump(() => window.OwnedWindows.Any()); var dialog = Assert.Single(window.OwnedWindows); Render(dialog); return dialog; }
    private static bool Idle(MainWindow window) => typeof(MainWindow).GetField("operation", Private)!.GetValue(window) == null;
    private static async Task Check(MainWindow window, Window dialog) { Click(dialog, "Vorschau prüfen"); await Pump(() => Idle(window)); Render(dialog); }
    private static ComboBox[] Choices(Window dialog) => dialog.GetVisualDescendants().OfType<ComboBox>().ToArray();
    private static void Select(ComboBox combo, string id) => combo.SelectedItem = combo.Items.Cast<ThemeOption>().Single(o => o.Id == id);
    private static MainWindow Open(string data, string theme, bool tv)
    {
        new SettingsStore(Path.Combine(data, "settings.json")).Save(new() { Theme = theme, TvMode = tv, OnboardingCompleted = true, PreferGerman = false, CheckAppUpdatesOnStartup = false, CheckUpdatesOnStartup = false, AutoDownloadCovers = false });
        var window = new MainWindow(data); window.Show(); Render(window); return window;
    }
    [AvaloniaTheory][InlineData("Midnight", 900, false)][InlineData("Classic Light", 640, false)][InlineData("Studio", 1100, true)]
    public async Task ReviewChangesInvalidatePreviewAndCancelLeavesActiveSettingsUntouched(string palette, int width, bool tv)
    {
        using var dir = new TestDirectory(); var root = ThemeSelectionTests.Fixture(dir); var data = Path.Combine(dir.Path, "data"); var window = Open(data, palette, tv); var before = File.ReadAllBytes(Path.Combine(data, "settings.json"));
        try
        {
            var shown = Show(window, root); var dialog = await Next(window); dialog.Width = width; dialog.Height = tv ? 850 : 760; Render(dialog);
            Assert.False(Button(dialog, "Geprüftes Theme übernehmen").IsEnabled); Click(dialog, "Geprüftes Theme übernehmen"); Assert.False(Directory.Exists(Path.Combine(data, "themes")));
            var choices = Choices(dialog); Assert.Equal(4, choices.Length); Assert.Equal(2, choices[0].ItemCount);
            Select(choices[0], "second"); Select(choices[1], "light"); Select(choices[2], "4:3"); await Check(window, dialog);
            Assert.True(Button(dialog, "Geprüftes Theme übernehmen").IsEnabled); Assert.Contains(dialog.GetVisualDescendants().OfType<Control>(), c => c.Classes.Contains("theme-preview"));
            var sample = dialog.GetVisualDescendants().OfType<Border>().Single(c => c.Classes.Contains("theme-preview")); var samplePoint = sample.TranslatePoint(new(0, 0), dialog)!.Value;
            Assert.InRange(samplePoint.Y, 0, dialog.ClientSize.Height - sample.Bounds.Height);
            Assert.Equal(before, File.ReadAllBytes(Path.Combine(data, "settings.json"))); Assert.False(Directory.Exists(Path.Combine(data, "themes")));
            var output = Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS"); if (output != null) { Directory.CreateDirectory(output); using var frame = dialog.CaptureRenderedFrame(); frame!.Save(Path.Combine(output, $"Theme-import-{palette}-{width}.png")); }
            var apply = Button(dialog, "Geprüftes Theme übernehmen"); Assert.DoesNotContain(apply.GetVisualAncestors(), a => a is ScrollViewer); var point = apply.TranslatePoint(new(0, 0), dialog)!.Value; Assert.True(point.Y + apply.Bounds.Height <= dialog.ClientSize.Height);
            Select(choices[1], "dark"); Assert.False(apply.IsEnabled); Assert.DoesNotContain(dialog.GetVisualDescendants().OfType<Control>(), c => c.Classes.Contains("theme-preview"));
            ControllerUi.Handle(dialog, 0x2000); await shown; Assert.Equal(before, File.ReadAllBytes(Path.Combine(data, "settings.json"))); Assert.False(Directory.Exists(Path.Combine(data, "themes")));
        }
        finally { foreach (var child in window.OwnedWindows.ToArray()) child.Close(); window.Close(); }
    }
    [AvaloniaFact] public async Task ApplyingUsesReviewedSelectionWhileChangedSourcesFailWithoutActivation()
    {
        using var dir = new TestDirectory(); var root = ThemeSelectionTests.Fixture(dir); var data = Path.Combine(dir.Path, "data"); var window = Open(data, "Midnight", false); var saved = new SettingsStore(Path.Combine(data, "settings.json"));
        var before = new LibraryStore(Path.Combine(data, "library.db")).GetAll();
        try
        {
            var shown = Show(window, root); var dialog = await Next(window); var choices = Choices(dialog); Select(choices[0], "second"); Select(choices[1], "light"); await Check(window, dialog);
            dir.File("theme/new.txt", "changed after review"); Click(dialog, "Geprüftes Theme übernehmen"); await Pump(() => Idle(window)); Render(dialog); Assert.True(dialog.IsVisible); Assert.False(Button(dialog, "Geprüftes Theme übernehmen").IsEnabled); Assert.Null(saved.Load().EmulationStationTheme); Assert.False(Directory.Exists(Path.Combine(data, "themes")));
            await Check(window, dialog); Click(dialog, "Geprüftes Theme übernehmen"); await shown.WaitAsync(TimeSpan.FromSeconds(10));
            var imported = saved.Load().EmulationStationTheme; Assert.NotNull(imported); Assert.StartsWith(Path.Combine(data, "themes"), imported);
            Assert.Equal("111111", EmulationStationTheme.Load(imported, "GBA").Elements.Single(e => e.Name == "gamelist").Properties["primaryColor"]);
            Assert.Equal("AA0000", EmulationStationTheme.Load(imported, "GBA").Elements.Single(e => e.Name == "md_name").Properties["color"]); Assert.Equal(before, new LibraryStore(Path.Combine(data, "library.db")).GetAll());
        }
        finally { foreach (var child in window.OwnedWindows.ToArray()) child.Close(); window.Close(); }
    }
    [AvaloniaFact] public async Task ClassicThemeAndCancelledOperationKeepDialogUsable()
    {
        using var dir = new TestDirectory(); dir.File("classic/nes/theme.xml", "<theme><formatVersion>3</formatVersion><view name='detailed'><textlist name='gamelist'><primaryColor>ABCDEF</primaryColor></textlist></view></theme>");
        var window = Open(Path.Combine(dir.Path, "data"), "Midnight", false);
        try
        {
            var shown = Show(window, Path.Combine(dir.Path, "classic")); var dialog = await Next(window); var choices = Choices(dialog); Assert.All(choices.Take(3), c => Assert.False(c.IsEnabled));
            var busy = (Task<bool>)typeof(MainWindow).GetMethod("BusyAsync", Private)!.Invoke(window, [new Func<CancellationToken, Task>(token => Task.Delay(Timeout.Infinite, token))])!;
            Assert.False(Button(dialog, "Vorschau prüfen").IsEnabled); ControllerUi.Handle(dialog, 0x2000); Assert.False(await busy); Assert.True(dialog.IsVisible); Assert.True(Button(dialog, "Vorschau prüfen").IsEnabled);
            await Check(window, dialog); Assert.True(Button(dialog, "Geprüftes Theme übernehmen").IsEnabled); Click(dialog, "Abbrechen"); await shown;
        }
        finally { foreach (var child in window.OwnedWindows.ToArray()) child.Close(); window.Close(); }
    }
    [AvaloniaFact] public async Task LayoutOptInUsesSamePanelForPreviewAndActiveLibraryAndPersists()
    {
        using var dir = new TestDirectory(); var root = ThemeLayoutTests.Fixture(dir); var data = Path.Combine(dir.Path, "data"); var window = Open(data, "Midnight", false);
        try
        {
            var shown = Show(window, root); var dialog = await Next(window);
            var layout = dialog.GetVisualDescendants().OfType<CheckBox>().Single(c => c.Content as string == "Theme-Layout in Vorschau und nach Übernahme verwenden"); layout.IsChecked = true;
            await Check(window, dialog); Assert.Single(dialog.GetVisualDescendants().OfType<ThemeLayoutPanel>()); Assert.Equal("Grid", new SettingsStore(Path.Combine(data, "settings.json")).Load().LibraryLayout);
            Click(dialog, "Geprüftes Theme übernehmen"); await shown.WaitAsync(TimeSpan.FromSeconds(10)); Render(window);
            var settings = new SettingsStore(Path.Combine(data, "settings.json")).Load(); Assert.Equal("Theme", settings.LibraryLayout); Assert.NotNull(settings.EmulationStationTheme);
            using var selection = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(settings.EmulationStationTheme, "emuvora-theme-options.json"))); Assert.True(selection.RootElement.GetProperty("UseLayout").GetBoolean());
            Assert.Single(window.GetVisualDescendants().OfType<ThemeLayoutPanel>()); Assert.Empty(new LibraryStore(Path.Combine(data, "library.db")).GetAll());
        }
        finally { foreach (var child in window.OwnedWindows.ToArray()) child.Close(); window.Close(); }
    }
}
