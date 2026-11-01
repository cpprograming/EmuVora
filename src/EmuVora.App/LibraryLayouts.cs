using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.Core;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private readonly ComboBox libraryLayoutChoice = new() { ItemsSource = new[] { "Cover-Raster", "Kompakte Liste", "Cover-Karussell", "Theme-Layout" }, MinWidth = 152 };
    private readonly Dictionary<string, (int Page, string? Selected, Vector Offset)> layoutPositions = [];
    private int layoutRevision;
    private bool changingLibraryLayout;
    private bool IsListLayout => settings.LibraryLayout == "List";
    private bool IsCarouselLayout => settings.LibraryLayout == "Carousel";
    private int carouselCardWidth = 180;
    private int carouselResizeRevision;
    private int CarouselWidth() => gamesList.Bounds.Height <= 0 ? 180
        : Math.Clamp((int)((gamesList.Bounds.Height - (settings.TvMode ? 160 : 140)) / 1.34), 100, settings.TvMode ? 280 : 240);
    private void ResizeCarousel()
    {
        if (!IsCarouselLayout || CarouselWidth() == carouselCardWidth) return;
        carouselCardWidth = CarouselWidth(); var revision = ++carouselResizeRevision;
        Dispatcher.UIThread.Post(() =>
        {
            if (isClosed || !IsCarouselLayout || revision != carouselResizeRevision) return;
            Refresh(Selected?.Path, keepPage: true); ScrollToLibrarySelection();
        }, DispatcherPriority.Background);
    }

    private void ConfigureLibraryLayout()
    {
        if (IsCarouselLayout) carouselCardWidth = CarouselWidth();
        currentThemeSystem = null;
        gamesList.Classes.Set("list-layout", IsListLayout || IsThemeLayout);
        gamesList.Classes.Set("carousel-layout", IsCarouselLayout);
        ScrollViewer.SetHorizontalScrollBarVisibility(gamesList, IsCarouselLayout ? Avalonia.Controls.Primitives.ScrollBarVisibility.Auto : Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(gamesList, Avalonia.Controls.Primitives.ScrollBarVisibility.Auto);
        gamesList.ItemsPanel = new FuncTemplate<Panel?>(() => IsCarouselLayout ? new VirtualizingStackPanel { Orientation = Orientation.Horizontal } : IsListLayout || IsThemeLayout ? new VirtualizingStackPanel() : new WrapPanel());
        gamesList.ItemTemplate = new FuncDataTemplate<Game>((game, _) => game == null ? new Border() : IsListLayout || IsThemeLayout ? CreateListRow(game) : CreateCard(game));
        if (librarySurface.Content != null) MountLibrarySurface();
    }
    private void ChangeLibraryLayout()
    {
        var next = libraryLayoutChoice.SelectedIndex switch { 1 => "List", 2 => "Carousel", 3 => "Theme", _ => "Grid" };
        if (settings.LibraryLayout == next) return;
        var selected = Selected?.Path;
        var scroll = gamesList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        layoutPositions[IsThemeLayout ? "Theme" : IsCarouselLayout ? "Carousel" : IsListLayout ? "List" : "Grid"] = (libraryPage, selected, scroll?.Offset ?? default);
        settings.LibraryLayout = next; settingsStore.Save(settings);
        changingLibraryLayout = true;
        ConfigureLibraryLayout(); Refresh(selected, keepPage: true);
        var revision = ++layoutRevision;
        Dispatcher.UIThread.Post(() =>
        {
            if (isClosed || layoutRevision != revision) return;
            gamesList.UpdateLayout();
            var target = gamesList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            if (target != null && layoutPositions.TryGetValue(next, out var position)
                && position.Page == libraryPage && position.Selected == Selected?.Path) target.Offset = position.Offset;
            else if (Selected != null) ScrollToLibrarySelection();
            changingLibraryLayout = false;
        }, DispatcherPriority.Background);
    }
    private void ScrollToLibrarySelection()
    {
        if (Selected is not {} selected) return;
        gamesList.ScrollIntoView(selected);
        if (!IsCarouselLayout) return;
        Dispatcher.UIThread.Post(() =>
        {
            if (isClosed || !IsCarouselLayout || Selected?.Path != selected.Path) return;
            gamesList.UpdateLayout();
            var item = gamesList.GetVisualDescendants().OfType<ListBoxItem>().FirstOrDefault(i => i.IsSelected);
            var scroll = gamesList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            if (item != null && scroll != null && item.TranslatePoint(new(item.Bounds.Width / 2, 0), scroll) is {} point)
                scroll.Offset = new(Math.Max(0, scroll.Offset.X + point.X - scroll.Viewport.Width / 2), 0);
        }, DispatcherPriority.Background);
    }
    private string LibraryItemInfo(Game game) => game.System
        + (Versions(game).Count > 1 ? $" · {Versions(game).Count} Versionen" : "")
        + (File.Exists(game.Path) ? "" : " · Datei fehlt");

    private Control CreateListRow(Game game)
    {
        var tv = settings.TvMode; var coverWidth = tv ? 68 : 54;
        var row = new Grid { ColumnDefinitions = new("Auto,*"), ColumnSpacing = tv ? 18 : 14, Margin = new(12, 8), MinHeight = tv ? 100 : 80 };
        var artwork = GameArtworkFrame(game, coverWidth, tv ? 92 : 74, compact: true); artwork.Width = coverWidth; artwork.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(artwork);
        var text = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(GameTitleText(game, compact: true));text.Children.Add(GameCaption(LibraryItemInfo(game)));
        text.Children.Add(GameCaption($"{game.PlayCount} Starts · " + (game.LastPlayed is {} last ? "Zuletzt " + last.ToLocalTime().ToString("g") : "Noch nicht gespielt"), secondary: true));
        Grid.SetColumn(text, 1); row.Children.Add(text);
        ToolTip.SetTip(row, game.Title + "\n" + game.Path); AutomationProperties.SetName(row, game.Title + " · " + LibraryItemInfo(game));
        row.Classes.Add("library-list-row"); return row;
    }
}
