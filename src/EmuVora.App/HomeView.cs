using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Avalonia.Threading;
using EmuVora.Core;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private readonly ScrollViewer homeSurface = new() { IsVisible = false, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    private readonly Grid libraryArea = new() { RowDefinitions = new("*,Auto") };
    private readonly Grid libraryFilters = new() { ColumnDefinitions = new("*,Auto,Auto"), RowDefinitions = new("Auto,Auto"), ColumnSpacing = 10, RowSpacing = 4 };
    private bool IsHome => view == "Startseite";
    private bool changingHome;
    private int libraryNavigationRevision;
    private LibraryQuery? pendingLibraryNavigationQuery;
    private readonly List<UniformGrid> homeGameGrids = [];
    private int HomeColumns => ClientSize.Width - (settings.TvMode ? 320 : 240) - 60 >= 1800 ? 6 : ClientSize.Width - (settings.TvMode ? 320 : 240) - 60 >= 840 ? 3 : 2;
    private void UpdateHomeColumns() { foreach (var grid in homeGameGrids) grid.Columns = HomeColumns; }
    private HomeReturn? homeReturn;
    private sealed record HomeReturn(string View, string Search, string? System, int Sort, string? Collection, int Page, string? Path);
    private void ShowHome()
    {
        libraryNavigationRevision++;
        pendingLibraryNavigationQuery = null;
        if (!IsHome) homeReturn = new(view, search.Text ?? "", systems.SelectedItem as string, sort.SelectedIndex, collectionChoice.SelectedItem as string, libraryPage, Selected?.Path);
        view = "Startseite"; Refresh();
    }
    private void ReturnFromHome()
    {
        var previous = homeReturn; changingHome = true;
        try
        {
            view = previous?.View ?? "Alle Spiele"; search.Text = previous?.Search ?? ""; systems.SelectedItem = previous?.System ?? "Alle Konsolen"; sort.SelectedIndex = previous?.Sort ?? 0;
            collectionChoice.SelectedItem = previous?.Collection; libraryPage = previous?.Page ?? 0;
            if (view == "Sammlung" && collectionChoice.SelectedItem == null) view = "Alle Spiele";
        }
        finally { changingHome = false; }
        homeReturn = null; controllerUiMode = false; CompleteLibraryNavigation(previous?.Path, previous?.Page ?? 0, true);
    }
    private void OpenHomeSelection(Game? game = null, string? collection = null, string? targetView = null)
    {
        changingHome = true;
        try { view = targetView ?? (collection == null ? "Alle Spiele" : "Sammlung"); search.Text = ""; systems.SelectedItem = "Alle Konsolen"; sort.SelectedIndex = targetView == "Zuletzt gespielt" ? 2 : 0; collectionChoice.SelectedItem = collection; }
        finally { changingHome = false; }
        homeReturn = null; controllerUiMode = false; CompleteLibraryNavigation(game?.Path, 0, false);
    }
    private void CompleteLibraryNavigation(string? path, int page, bool restorePage, bool resetTheme = true)
    {
        var revision = ++libraryNavigationRevision; var target = CurrentFilterQuery(); pendingLibraryNavigationQuery = target;
        if (restorePage) libraryPage = page; if (resetTheme) ApplyTheme(); Refresh(path, keepPage: restorePage); gamesList.Focus();
        // Ignore only deferred notifications for this exact destination; newer user input still refreshes normally.
        Dispatcher.UIThread.Post(() =>
        {
            if (revision == libraryNavigationRevision && ReferenceEquals(pendingLibraryNavigationQuery, target)) pendingLibraryNavigationQuery = null;
        }, DispatcherPriority.Background);
    }
    private void RefreshHome()
    {
        homeSurface.Content = null; homeGameGrids.Clear();
        foreach (var image in gamesList.GetVisualDescendants().OfType<Image>()) image.Source = null;
        gamesList.ItemsSource = null; detailCover.Content = null; ClearThemeStageImages();
        foreach (var image in coverImages.Values) image.Dispose(); coverImages.Clear();
        libraryArea.IsVisible = libraryFilters.IsVisible = false; homeSurface.IsVisible = true; ApplyTheme(); ApplyLayout();
        var data = HomePresentation.Create(allGames, settings); libraryHeading.Text = "Willkommen zurück"; libraryEyebrow.Text = "DEIN EMUVORA";
        summary.Text = $"{data.Titles} Titel · {data.Files} Dateien\n{data.Systems} " + (data.Systems == 1 ? "Konsole" : "Konsolen");
        var body = new StackPanel { Spacing = settings.TvMode ? 28 : 22, Margin = new(0, 0, 12, 16) };
        var intro = new StackPanel { Spacing = 10, Margin = new(20) };
        intro.Children.Add(new TextBlock { Text = "Deine Klassiker. Griffbereit.", FontSize = settings.TvMode ? 30 : 25, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        intro.Children.Add(new TextBlock { Text = "Steige bei deinen letzten Spielen ein oder öffne deine Sammlung. Spielkarten führen zur Bibliothek mit Details und Versionsauswahl.", FontSize = settings.TvMode ? 18 : 14, TextWrapping = TextWrapping.Wrap, Opacity = .75 });
        var actions = new WrapPanel(); var library = ActionButton("Zur Bibliothek", ReturnFromHome); library.Margin = new(0, 0, 8, 8); actions.Children.Add(library);
        var customize = AsyncButton("Startseite anpassen", ShowHomeOptionsAsync); customize.Margin = new(0, 0, 8, 8); actions.Children.Add(customize); intro.Children.Add(actions);
        var hero = new Border { CornerRadius = new(16), Child = intro, BorderThickness = new(1), BorderBrush = Resources["EmuAccent"] as IBrush, Background = Resources["EmuSurface"] as IBrush }; body.Children.Add(hero);
        void Games(string title, IReadOnlyList<Game> games, string target, string emptyText)
        {
            var section = new StackPanel { Spacing = 10 }; var heading = new WrapPanel(); heading.Children.Add(new TextBlock { Text = title, FontSize = settings.TvMode ? 24 : 20, FontWeight = FontWeight.SemiBold, Margin = new(0, 0, 16, 8) });
            var all = ActionButton("Alle anzeigen", () => OpenHomeSelection(targetView: target)); all.Margin = new(0, 0, 0, 8); heading.Children.Add(all); section.Children.Add(heading);
            if (games.Count == 0) section.Children.Add(new TextBlock { Text = emptyText, TextWrapping = TextWrapping.Wrap, Opacity = .7 });
            else { var cards = new UniformGrid { Columns = HomeColumns }; homeGameGrids.Add(cards); foreach (var game in games) cards.Children.Add(HomeGameCard(game)); section.Children.Add(cards); }
            body.Children.Add(section);
        }
        if (allGames.Count == 0)
        {
            body.Children.Add(new TextBlock { Text = "Deine Bibliothek wartet auf ihre ersten Spiele. Importiere einen Ordner mit deinen ROMs.", TextWrapping = TextWrapping.Wrap });
            var import = AsyncButton("Ersten ROM-Ordner importieren", () => ImportAsync()); import.IsEnabled = operation == null; body.Children.Add(import);
        }
        if (settings.HomeShowRecent) Games("Zuletzt gespielt", data.Recent, "Zuletzt gespielt", "Hier erscheinen Spiele nach ihrem ersten Start.");
        if (settings.HomeShowFavorites) Games("Deine Favoriten", data.Favorites, "Favoriten", "Markiere Spiele in der Bibliothek als Favoriten, um sie hier wiederzufinden.");
        if (settings.HomeShowCollections)
        {
            var section = new StackPanel { Spacing = 10 }; section.Children.Add(new TextBlock { Text = "Deine Sammlungen", FontSize = settings.TvMode ? 24 : 20, FontWeight = FontWeight.SemiBold });
            if (data.Collections.Count == 0) section.Children.Add(new TextBlock { Text = "Erstelle eigene Sammlungen für deine Spiele.", Opacity = .7, TextWrapping = TextWrapping.Wrap });
            var cards = new WrapPanel(); foreach (var collection in data.Collections)
            {
                var button = ActionButton(collection.Name + $"\n{collection.Titles} Titel · {collection.Files} Dateien", () => OpenHomeSelection(collection: collection.Name)); button.Width = settings.TvMode ? 260 : 220; button.MinHeight = 76; button.Margin = new(0, 0, 10, 10);
                button.Content = new TextBlock { Text = collection.Name + $"\n{collection.Titles} Titel · {collection.Files} Dateien", TextWrapping = TextWrapping.Wrap, MaxLines = 3 }; button.ContentTemplate = new FuncDataTemplate<Control>((control, _) => control); AutomationProperties.SetName(button, collection.Name + " · Sammlung öffnen"); button.Classes.Add("home-collection"); cards.Children.Add(button);
            }
            section.Children.Add(cards); var manage = AsyncButton("Sammlungen verwalten", ManageCollectionsAsync); manage.IsEnabled = operation == null; section.Children.Add(manage); body.Children.Add(section);
        }
        if (!settings.HomeShowRecent && !settings.HomeShowFavorites && !settings.HomeShowCollections) body.Children.Add(new TextBlock { Text = "Alle Bereiche sind ausgeblendet. Über „Startseite anpassen“ kannst du sie wieder einblenden.", TextWrapping = TextWrapping.Wrap, Opacity = .7 });
        homeSurface.Content = body; foreach (var (name, button) in viewButtons) { button.FontWeight = name == view ? FontWeight.Bold : FontWeight.Normal; button.Background = name == view ? Resources["EmuSelection"] as IBrush : Brushes.Transparent; }
        if (!OwnedWindows.Any(w => w.IsVisible)) library.Focus();
    }
    private Button HomeGameCard(Game game)
    {
        var tv = settings.TvMode; var width = tv ? 220 : 180; var content = new StackPanel { Spacing = 8 };
        content.Children.Add(new Border { Height = tv ? 210 : 180, CornerRadius = new(10), Child = Artwork(game, width), ClipToBounds = true });
        content.Children.Add(new TextBlock { Text = game.Title, FontSize = tv ? 20 : 16, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, MaxLines = 2, Height = tv ? 54 : 44 });
        content.Children.Add(new TextBlock { Text = game.System + (game.LastPlayed is {} last ? " · " + last.ToLocalTime().ToString("d") : ""), FontSize = tv ? 15 : 12, Opacity = .7, TextTrimming = TextTrimming.CharacterEllipsis });
        var button = ActionButton(game.Title, () => OpenHomeSelection(game)); button.Content = content; button.MaxWidth = tv ? 320 : 300; button.Margin = new(0, 0, 12, 12); button.Padding = new(10); button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        button.ContentTemplate = new FuncDataTemplate<Control>((control, _) => control); button.Tag = game.Path;
        button.Classes.Add("home-game"); AutomationProperties.SetName(button, game.Title + " · " + game.System + " · Spieldetails öffnen"); ToolTip.SetTip(button, game.Title + "\nSpieldetails öffnen");
        button.Styles.Add(new Style(s => s.OfType<Button>().Class(":focus-visible")) { Setters = { new Setter(Button.BorderBrushProperty, new DynamicResourceExtension("EmuAccent")), new Setter(Button.BorderThicknessProperty, new Thickness(2)) } });
        return button;
    }
    private async Task ShowHomeOptionsAsync()
    {
        if (operation != null) return;
        var body = new StackPanel { Spacing = 12 }; var recent = new CheckBox { Content = "Zuletzt gespielt anzeigen", IsChecked = settings.HomeShowRecent }; var favorites = new CheckBox { Content = "Favoriten anzeigen", IsChecked = settings.HomeShowFavorites }; var collections = new CheckBox { Content = "Sammlungen anzeigen", IsChecked = settings.HomeShowCollections }; var startup = new CheckBox { Content = "EmuVora mit der Startseite öffnen", IsChecked = settings.HomeOnStartup };
        body.Children.Add(new TextBlock { Text = "Deine Startseite", FontSize = 25, FontWeight = FontWeight.Bold }); foreach (var control in new[] { recent, favorites, collections, startup }) body.Children.Add(control);
        body.Children.Add(new TextBlock { Text = "Die Auswahl verändert ausschließlich die Darstellung. Spiele, Favoriten und Sammlungen bleiben erhalten.", TextWrapping = TextWrapping.Wrap });
        var actions = new StackPanel { Spacing = 8 }; var dialog = LibraryDialog("Startseite anpassen", body, actions); actions.Children.Add(ActionButton("Speichern", () => dialog.Close(true))); actions.Children.Add(ActionButton("Abbrechen", () => dialog.Close(false)));
        if (!await dialog.ShowDialog<bool>(this) || isClosed) return;
        var previous = (settings.HomeShowRecent, settings.HomeShowFavorites, settings.HomeShowCollections, settings.HomeOnStartup);
        settings.HomeShowRecent = recent.IsChecked == true; settings.HomeShowFavorites = favorites.IsChecked == true; settings.HomeShowCollections = collections.IsChecked == true; settings.HomeOnStartup = startup.IsChecked == true;
        try { settingsStore.Save(settings); } catch { (settings.HomeShowRecent, settings.HomeShowFavorites, settings.HomeShowCollections, settings.HomeOnStartup) = previous; throw; }
        Refresh();
    }
}
