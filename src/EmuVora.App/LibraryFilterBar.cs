using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using EmuVora.Core;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private readonly WrapPanel activeFilters = new() { IsVisible = false };
    private bool changingLibraryFilters;
    private LibraryQuery CurrentFilterQuery() => new(search.Text ?? "", systems.SelectedItem as string, view, Sort: sort.SelectedIndex, Collection: collectionChoice.SelectedItem as string);
    private void HandleLibraryFilterChange()
    {
        if (changingHome || changingLibraryFilters) return;
        if (pendingLibraryNavigationQuery is {} pending && pending == CurrentFilterQuery()) return;
        pendingLibraryNavigationQuery = null; Refresh();
    }
    private void RefreshActiveFilters()
    {
        activeFilters.Children.Clear(); var filters = LibraryFilters.Describe(CurrentFilterQuery()); activeFilters.IsVisible = filters.Count > 0;
        if (filters.Count == 0) return;
        activeFilters.Children.Add(new TextBlock { Text = filters.Count + " Filter aktiv", VerticalAlignment = VerticalAlignment.Center, FontSize = settings.TvMode ? 16 : 12, Opacity = .7, Margin = new(0, 0, 10, 6) });
        foreach (var filter in filters)
        {
            var button = ActionButton(filter.Label + " ×", () => RemoveLibraryFilter(filter.Kind)); button.Tag = filter.Kind; button.MaxWidth = settings.TvMode ? 300 : 250; button.Margin = new(0, 0, 8, 6);
            var content = new Grid { ColumnDefinitions = new("*,Auto") }; content.Children.Add(new TextBlock { Text = filter.Label, TextTrimming = TextTrimming.CharacterEllipsis, MaxLines = 1 });
            var remove = new TextBlock { Text = "×", Margin = new(8, 0, 0, 0) }; Grid.SetColumn(remove, 1); content.Children.Add(remove); button.Content = content; button.ContentTemplate = new FuncDataTemplate<Control>((control, _) => control);
            button.Classes.Add("active-library-filter"); AutomationProperties.SetName(button, filter.Label + " entfernen"); ToolTip.SetTip(button, filter.Label + "\nDiesen Filter entfernen"); activeFilters.Children.Add(button);
        }
        var reset = ActionButton("Alle Filter zurücksetzen", ClearLibraryFilters); reset.Margin = new(0, 0, 0, 6); reset.Classes.Add("clear-library-filters"); activeFilters.Children.Add(reset);
    }
    private void RemoveLibraryFilter(LibraryFilterKind kind) => ChangeLibraryFilters(() =>
    {
        if (kind == LibraryFilterKind.Search) search.Text = "";
        else if (kind == LibraryFilterKind.System) systems.SelectedItem = "Alle Konsolen";
        else { view = "Alle Spiele"; collectionChoice.SelectedItem = null; }
    }, kind == LibraryFilterKind.Search);
    private void ClearLibraryFilters() => ChangeLibraryFilters(() => { search.Text = ""; systems.SelectedItem = "Alle Konsolen"; view = "Alle Spiele"; collectionChoice.SelectedItem = null; }, true);
    private void ChangeLibraryFilters(Action change, bool focusSearch)
    {
        var selected = Selected?.Path; changingLibraryFilters = true;
        try { change(); } finally { changingLibraryFilters = false; }
        CompleteLibraryNavigation(selected, 0, false, resetTheme: false);
        if (focusSearch) search.Focus(); else controllerUiMode = false;
    }
}
