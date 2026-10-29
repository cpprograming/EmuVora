using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Avalonia.Threading;
using EmuVora.Core;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private async Task ShowThemeImportAsync(string source)
    {
        if (operation != null) return;
        var owner = OwnedWindows.LastOrDefault(w => w.IsActive) ?? this;
        var preferDark = settings.Theme != "Classic Light"; ThemeOptions? options = null;
        if (!await BusyAsync(async token => options = await Task.Run(() => EmulationStationTheme.InspectOptions(source, preferDark), token)) || options == null || isClosed || !owner.IsVisible) return;
        var body = new StackPanel { Spacing = 12 };
        body.Children.Add(new TextBlock { Text = "Theme auswählen und prüfen", FontSize = 26, FontWeight = FontWeight.Bold });
        body.Children.Add(new TextBlock { Text = "Vorschau mit Beispieldaten in EmuVoras Oberfläche. Unterstützte Farben, Bilder und Layout-Rechtecke werden angepasst. Weitere Originalelemente, Videos und Animationen bleiben offen; der Bericht nennt die Grenzen.", TextWrapping = TextWrapping.Wrap });
        body.Children.Add(new TextBlock { Text = "Quelle: " + Path.GetFullPath(source), TextWrapping = TextWrapping.Wrap, Opacity = .7 });
        ComboBox Choice(string label, IReadOnlyList<ThemeOption> choices, string selected)
        {
            body.Children.Add(new TextBlock { Text = label });
            var items = choices.Count == 0 ? new[] { new ThemeOption("", "Automatisch / klassisch") } : choices;
            var control = new ComboBox { ItemsSource = items, SelectedItem = items.FirstOrDefault(o => o.Id == selected) ?? items.First(), HorizontalAlignment = HorizontalAlignment.Stretch,
                ItemTemplate = new FuncDataTemplate<ThemeOption>((o, _) => new TextBlock { Text = o == null ? "" : o.Id.Length == 0 ? o.Label : o.ToString(), TextWrapping = TextWrapping.Wrap }) };
            body.Children.Add(control); return control;
        }
        var variant = Choice("Theme-Variante", options.Variants, options.Default.Variant);
        var scheme = Choice("Farbschema", options.ColorSchemes, options.Default.ColorScheme);
        var aspect = Choice("Vorlagen-Seitenverhältnis", options.AspectRatios, options.Default.AspectRatio);
        body.Children.Add(new TextBlock { Text = "Das Vorlagenformat wählt passende Theme-Includes. Das Theme-Layout wird mit diesem Seitenverhältnis ins Fenster eingepasst; die Windows-Auflösung bleibt erhalten.", TextWrapping = TextWrapping.Wrap, Opacity = .75 });
        body.Children.Add(new TextBlock { Text = "Konsole für die Vorschau" });
        var system = new ComboBox { ItemsSource = EmulatorCatalog.Systems, SelectedItem = EmulatorCatalog.Systems.Contains(Selected?.System ?? "") ? Selected!.System : "NES", HorizontalAlignment = HorizontalAlignment.Stretch }; body.Children.Add(system);
        var useLayout = new CheckBox { Content = "Theme-Layout in Vorschau und nach Übernahme verwenden", IsChecked = IsThemeLayout }; body.Children.Add(useLayout);
        body.Children.Add(new TextBlock { Text = "Teilweiser Layout-Adapter: Liste, Bilder und Texte. Die native Liste bleibt bedienbar; bei ungeeigneten oder kleinen Flächen wird sie als Ersatzansicht verwendet.", TextWrapping = TextWrapping.Wrap, Opacity = .75 });
        var preview = new ContentControl(); body.Children.Add(preview);
        var report = new TextBlock { TextWrapping = TextWrapping.Wrap }; body.Children.Add(report);
        var actions = new StackPanel { Spacing = 8 }; var dialog = LibraryDialog("EmuVora · Theme-Import mit Vorschau", body, actions); dialog.Width = 900; dialog.Height = 900;
        ThemeImportPlan? plan = null; var revision = 0; var images = new List<Bitmap>();
        Button check = new(), apply = new();
        check = AsyncButton("Vorschau prüfen", Verify);
        apply = AsyncButton("Geprüftes Theme übernehmen", async () =>
        {
            if (operation != null || plan == null) return;
            var selected = plan; string? imported = null;
            var completed = await BusyAsync(async token => imported = await Task.Run(() => EmulationStationTheme.ApplyImport(selected, dataDir, token), token));
            if (!completed || imported == null) { plan = null; report.Text = status.Text; State(); return; }
            if (isClosed || !dialog.IsVisible) return;
            var previous = settings.EmulationStationTheme; var previousLayout = settings.LibraryLayout; settings.EmulationStationTheme = imported;
            settings.LibraryLayout = selected.Selection?.UseLayout == true ? "Theme" : IsThemeLayout ? "Grid" : settings.LibraryLayout;
            try { settingsStore.Save(settings); } catch { settings.EmulationStationTheme = previous; settings.LibraryLayout = previousLayout; throw; }
            ClearThemeStageImages(); themeBackground.Source = themeLogo.Source = null; foreach (var image in themeImages.Values) image.Dispose(); themeImages.Clear();
            loadedThemes.Clear(); libraryLayoutChoice.SelectedIndex = IsThemeLayout ? 3 : IsCarouselLayout ? 2 : IsListLayout ? 1 : 0; ConfigureLibraryLayout(); ApplyTheme(); Refresh(); status.Text = "Geprüftes Theme mit gewählter Variante übernommen. Importbericht unter Darstellung verfügbar."; dialog.Close();
        });
        actions.Children.Add(check); actions.Children.Add(apply); actions.Children.Add(ActionButton("Abbrechen", () => { if (operation != null) RequestOperationCancel(); else dialog.Close(); }));
        void State()
        {
            var idle = operation == null; check.IsEnabled = idle; apply.IsEnabled = idle && plan != null;
            variant.IsEnabled = idle && options.Variants.Count > 0; scheme.IsEnabled = idle && options.ColorSchemes.Count > 0; aspect.IsEnabled = idle && options.AspectRatios.Count > 0; system.IsEnabled = useLayout.IsEnabled = idle;
        }
        void Clear()
        {
            foreach (var image in preview.GetVisualDescendants().OfType<Image>()) image.Source = null;
            preview.Content = null; foreach (var image in images) image.Dispose(); images.Clear();
        }
        void Invalidate()
        {
            revision++; plan = null; Clear(); report.Text = "Auswahl geändert. Vorschau vor dem Übernehmen erneut prüfen."; State();
        }
        async Task Verify()
        {
            if (operation != null) return;
            Invalidate(); var stamp = revision; var selection = new ThemeSelection(((ThemeOption)variant.SelectedItem!).Id, ((ThemeOption)scheme.SelectedItem!).Id, ((ThemeOption)aspect.SelectedItem!).Id, useLayout.IsChecked == true);
            var console = system.SelectedItem as string ?? "NES"; ThemeImportPlan? result = null;
            report.Text = "Auswahl und Dateiinhalte werden geprüft. Die bisherige Darstellung bleibt aktiv.";
            if (!await BusyAsync(async token => result = await Task.Run(() => EmulationStationTheme.PreviewImport(source, console, preferDark, selection, token), token)) || result == null || stamp != revision || isClosed || !dialog.IsVisible) { if (dialog.IsVisible) report.Text = status.Text; State(); return; }
            try
            {
                var renderNotes = new List<string>(); preview.Content = ThemeSample(result.Preview, images, selection.UseLayout, renderNotes);
                report.Text = $"{result.Files.Count} Dateien · {result.Bytes / 1024d / 1024d:F1} MiB · ausgewählte Kombination für alle {EmulatorCatalog.Systems.Length} Systeme strukturell geprüft.\n\n" + string.Join("\n", result.Notes.Concat(renderNotes));
                plan = result;
                Dispatcher.UIThread.Post(() => { if (dialog.IsVisible && stamp == revision) preview.BringIntoView(); }, DispatcherPriority.Loaded);
            }
            catch { Clear(); plan = null; throw; }
            finally { State(); }
        }
        variant.SelectionChanged += (_, _) => Invalidate(); scheme.SelectionChanged += (_, _) => Invalidate(); aspect.SelectionChanged += (_, _) => Invalidate(); system.SelectionChanged += (_, _) => Invalidate();
        useLayout.IsCheckedChanged += (_, _) => Invalidate();
        FeedbackChanged += State; dialog.Closed += (_, _) => { FeedbackChanged -= State; Clear(); }; State();
        await dialog.ShowDialog(owner);
    }
    private Control ThemeSample(EmulationStationTheme theme, List<Bitmap> images, bool useLayout = false, List<string>? renderNotes = null)
    {
        ThemeElement? Find(string name) => theme.Elements.LastOrDefault(e => e.Name == name);
        IBrush ColorFor(ThemeElement? element, string key, IBrush fallback)
        {
            var value = element?.Properties.GetValueOrDefault(key)?.TrimStart('#');
            if (value?.Length == 8) value = value[6..] + value[..6];
            return value?.Length is 6 or 8 && Color.TryParse("#" + value, out var color) ? new SolidColorBrush(color) : fallback;
        }
        Image? ImageFor(string name, double opacity)
        {
            var path = Find(name)?.Properties.GetValueOrDefault("path"); if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            var bitmap = ThemeArtwork.Load(path); images.Add(bitmap); return new Image { Source = bitmap, Stretch = Stretch.Uniform, Opacity = opacity };
        }
        if (useLayout)
        {
            var stage = new ThemeLayoutPanel { Height = settings.TvMode ? 350 : 300, Model = ThemeLayout.From(theme), Background = ColorFor(Find("canvas") ?? Find("background"), "color", Background ?? Brushes.Black) };
            if (ImageFor("background", 1) is {} stageBackgroundImage) { stageBackgroundImage.Stretch = Stretch.UniformToFill; stageBackgroundImage.IsHitTestVisible = false; stage.AddSlot("background", stageBackgroundImage); }
            if (ImageFor("logo", 1) is {} stageLogoImage) { stageLogoImage.IsHitTestVisible = false; stage.AddSlot("logo", stageLogoImage); }
            var list = new ListBox { ItemsSource = new[] { "Beispielspiel", "Weiteres Spiel", "Drittes Spiel" }, SelectedIndex = 0, Background = stage.Background, Foreground = ColorFor(Find("gamelist"), "primaryColor", Foreground ?? Brushes.White) };
            stage.AddSlot("gamelist", list);
            stage.AddSlot("md_name", new TextBlock { Text = "Beispielspiel", FontSize = 18, TextWrapping = TextWrapping.Wrap, Foreground = ColorFor(Find("md_name"), "color", Foreground ?? Brushes.White), IsHitTestVisible = false });
            stage.AddSlot("md_description", new TextBlock { Text = "Beispiel-Konsole\n3 Starts · Beispieldaten", FontSize = 13, TextWrapping = TextWrapping.Wrap, Foreground = ColorFor(Find("md_description") ?? Find("description"), "color", Foreground ?? Brushes.White), IsHitTestVisible = false });
            stage.AddSlot("md_image", new Border { Background = Brush.Parse("#354C56"), Child = new TextBlock { Text = "EMUVORA", Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }, IsHitTestVisible = false });
            var decorationNotes = ThemeDecorationRenderer.Replace(stage, element =>
            {
                var path = element.Properties.GetValueOrDefault("path"); if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
                var bitmap = ThemeArtwork.LoadDecoration(path); images.Add(bitmap); return bitmap;
            }, FontFamily, Foreground ?? Brushes.White, settings.TvMode ? 17 : 14);
            renderNotes?.AddRange(decorationNotes);
            var adapted = new Border { CornerRadius = new(12), Child = stage, ClipToBounds = true }; adapted.Classes.Add("theme-preview"); return adapted;
        }
        var canvas = new Grid { Height = settings.TvMode ? 330 : 280, Background = ColorFor(Find("canvas"), "color", Background ?? Brushes.Black), ClipToBounds = true };
        if (ImageFor("background", .25) is {} background) { background.Stretch = Stretch.UniformToFill; canvas.Children.Add(background); }
        var content = new Grid { RowDefinitions = new("Auto,*,Auto"), Margin = new(18), RowSpacing = 12 };
        var heading = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        if (ImageFor("logo", 1) is {} logo) { logo.Width = 150; logo.Height = 48; heading.Children.Add(logo); }
        heading.Children.Add(new TextBlock { Text = "Deine Spiele", FontSize = 23, Foreground = ColorFor(Find("md_name"), "color", Foreground ?? Brushes.White), VerticalAlignment = VerticalAlignment.Center }); content.Children.Add(heading);
        var samples = new StackPanel { Orientation = IsListLayout ? Orientation.Vertical : Orientation.Horizontal, Spacing = 12 };
        foreach (var title in new[] { "Beispielspiel", "Weiteres Spiel" })
        {
            var sample = new StackPanel { Spacing = 8 };
            if (!IsListLayout) sample.Children.Add(new Border { Width = 90, Height = 110, Background = Brush.Parse("#354C56"), CornerRadius = new(8), Child = new TextBlock { Text = "EMUVORA", Foreground = Brushes.White, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center } });
            sample.Children.Add(new TextBlock { Text = title, FontSize = 15, Foreground = ColorFor(Find("gamelist"), "primaryColor", Foreground ?? Brushes.White) }); samples.Children.Add(sample);
        }
        Grid.SetRow(samples, 1); content.Children.Add(samples);
        var caption = new TextBlock { Text = "EmuVora-Vorschau · Beispieldaten · " + (IsCarouselLayout ? "Karussell" : IsListLayout ? "Liste" : "Raster"), FontSize = 12, Foreground = ColorFor(Find("gamelist"), "primaryColor", Foreground ?? Brushes.White), TextWrapping = TextWrapping.Wrap }; Grid.SetRow(caption, 2); content.Children.Add(caption); canvas.Children.Add(content);
        var border = new Border { CornerRadius = new(12), Child = canvas, ClipToBounds = true }; border.Classes.Add("theme-preview"); return border;
    }
}
