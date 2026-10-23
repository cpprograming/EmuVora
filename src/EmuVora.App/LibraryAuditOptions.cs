using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using EmuVora.Core;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private Expander LibraryAuditOptions()
    {
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = "Gleiche Titel und ausdrücklich nummerierte Discs prüfen. Gleiche Namen beweisen keine identischen Dateien. Regionen und Editionen werden getrennt behandelt. Die Prüfung verändert keine Dateien oder Bibliothekseinträge.", TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(AsyncButton("Duplikate und Mehrdisc-Spiele prüfen", async () =>
        {
            var owner = OwnedWindows.LastOrDefault(w => w.IsActive) ?? this;
            IReadOnlyList<LibraryAuditGroup>? groups = null;
            var snapshot = allGames.ToArray();
            if (!await BusyAsync(async token => groups = await Task.Run(() => LibraryAudit.Inspect(snapshot, token), token)) || groups == null || isClosed || !owner.IsVisible) return;
            var content = new StackPanel { Spacing = 14, Margin = new(24) };
            content.Children.Add(new TextBlock { Text = $"{groups.Count} Gruppen zur Prüfung. Es wurde nichts verändert.", TextWrapping = TextWrapping.Wrap });
            content.Children.Add(new TextBlock { Text = "Mehrdisc-Kandidaten liegen im selben Ordner. Prüfe vor einer Playlist-Erstellung die Disc-Reihenfolge und die Unterstützung deines Emulators. Bereits vorhandene M3U-Playlists fasst der Import zusammen; früher importierte Einzeleinträge bleiben erhalten.", TextWrapping = TextWrapping.Wrap });
            content.Children.Add(new ListBox { Height = 360, ItemsSource = groups, ItemTemplate = new FuncDataTemplate<LibraryAuditGroup>((group, _) => new TextBlock { Text = group == null ? "" : $"{group.Kind} · {group.System}\n{group.Title}\n" + string.Join("\n", group.Games.Select(g => g.Path)), TextWrapping = TextWrapping.Wrap, Margin = new(8) }) });
            var dialog = Dialog("Duplikate und Mehrdisc-Spiele", content);
            content.Children.Add(ActionButton("Schließen", () => dialog.Close()));
            await dialog.ShowDialog(owner);
        }));
        return new Expander { Header = "Duplikate und Mehrdisc-Spiele", Content = panel };
    }
}
