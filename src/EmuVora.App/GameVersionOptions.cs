using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using EmuVora.Core;

namespace EmuVora.App;
public sealed partial class MainWindow
{
    private readonly Dictionary<string, IReadOnlyList<Game>> visibleVersions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Button chooseVersion = new() { Content = "Version auswählen", IsVisible = false };
    private IReadOnlyList<Game> Versions(Game game) => visibleVersions.GetValueOrDefault(game.Path) ?? new[] { game };
    private async Task<Game?> ChooseVersionAsync(Game game)
    {
        var versions = Versions(game); if (versions.Count < 2) return game;
        var panel = new StackPanel { Spacing = 14, Margin = new(24) };
        panel.Children.Add(new TextBlock { Text = game.Title + $" · {versions.Count} Versionen", FontSize = 23, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock { Text = "Gleicher Titel bedeutet nicht gleiche Datei-Inhalte. Favoriten, Cover und Verlauf gelten jeweils für die ausgewählte Datei. Keine Datei wird gelöscht.", TextWrapping = TextWrapping.Wrap });
        var list = new ListBox { Height = 240, ItemsSource = versions, SelectedItem = game, ItemTemplate = new FuncDataTemplate<Game>((g, _) => new TextBlock { Text = g == null ? "" : $"{g.Title}{(g.Favorite ? " · ★" : "")}\n{g.Path}\n{g.PlayCount} Starts", TextWrapping = TextWrapping.Wrap, Margin = new(8) }) }; panel.Children.Add(list);
        var dialog = Dialog("Spielversion auswählen", panel);
        panel.Children.Add(ActionButton("Diese Version auswählen", () => { if (list.SelectedItem is Game selected) dialog.Close(selected); }));
        panel.Children.Add(ActionButton("Abbrechen", () => dialog.Close()));
        return await dialog.ShowDialog<Game?>(OwnedWindows.LastOrDefault(w => w.IsActive) ?? this);
    }
}
