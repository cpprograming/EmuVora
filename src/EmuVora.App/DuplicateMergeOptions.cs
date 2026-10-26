using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using EmuVora.Core;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private async Task<bool> ReviewDuplicateMergeAsync(ContentMatch match, Window owner, TextBlock message)
    {
        if (operation != null || !CanBackUp()) return false;
        var panel = new StackPanel { Spacing = 12, Margin = new(24) };
        var template = new FuncDataTemplate<Game>((g, _) => new TextBlock { Text = g == null ? "" : g.Title + "\n" + g.Path, TextWrapping = TextWrapping.Wrap });
        var keeper = new ComboBox { ItemsSource = match.Games, ItemTemplate = template, SelectedIndex = 0, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        bool Has(Dictionary<string, string> values, Game game) => values.Keys.Any(p => Path.IsPathFullyQualified(p) && Path.GetFullPath(p).Equals(Path.GetFullPath(game.Path), StringComparison.OrdinalIgnoreCase));
        var initial = DuplicateMerge.Preview(dataDir, match, match.Games[0].Path);
        var covers = new ComboBox { ItemsSource = match.Games.Where(g => Has(settings.Covers, g)).ToArray(), ItemTemplate = template, SelectedItem = match.Games.FirstOrDefault(g => g.Path == initial.CoverSource), PlaceholderText = "Kein zugeordnetes Cover", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        var aspects = new ComboBox { ItemsSource = match.Games.Where(g => Has(settings.GameAspectRatios, g)).ToArray(), ItemTemplate = template, SelectedItem = match.Games.FirstOrDefault(g => g.Path == initial.AspectSource), PlaceholderText = "Automatisches Bildprofil", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        panel.Children.Add(new TextBlock { Text = "Verbleibender Titel und ROM-Pfad" }); panel.Children.Add(keeper);
        panel.Children.Add(new TextBlock { Text = "Cover übernehmen von (einschließlich Schutz)" }); panel.Children.Add(covers);
        panel.Children.Add(new TextBlock { Text = "Eigenes Bildprofil übernehmen von" }); panel.Children.Add(aspects);
        var summary = new TextBlock { TextWrapping = TextWrapping.Wrap }; panel.Children.Add(summary);
        panel.Children.Add(new TextBlock { Text = "Nur Bibliothekseinträge werden zusammengeführt. Alle ROMs, Cover-Dateien und Spielstände bleiben erhalten. Favoriten und Sammlungen werden vereinigt, Spielstarts addiert und der jüngste Startzeitpunkt übernommen. Bereits früher kopierte Zähler können dadurch doppelt zählen. Der früheste bekannte Importzeitpunkt bleibt erhalten.\n\nVorher werden Bibliothek und Einstellungen gesichert. Ursprüngliche Einträge bleiben außerdem im Zusammenführungsarchiv der Datenbank. Andere Titel/Cover/Profile sind in der Sicherung erhalten. Spielstände der anderen ROM-Pfade werden nicht automatisch dem verbleibenden Pfad zugeordnet. Bei ES-DE danach die Bibliothek neu abgleichen.", TextWrapping = TextWrapping.Wrap });
        var actions = new StackPanel { Spacing = 8 };
        var review = LibraryDialog("Duplikate zusammenführen", panel, actions); DuplicateMergePlan? plan = initial;
        var accept = ActionButton("Geprüfte Einträge zusammenführen", () => review.Close(plan));
        void Update()
        {
            try
            {
                plan = DuplicateMerge.Preview(dataDir, match, ((Game)keeper.SelectedItem!).Path, (covers.SelectedItem as Game)?.Path, (aspects.SelectedItem as Game)?.Path);
                var game = plan.Combined.Game; summary.Text = $"{match.Games.Count} Karten → 1 Karte · {game.PlayCount} Spielstarts · Favorit: {(game.Favorite ? "Ja" : "Nein")}\nLetzter Start: {game.LastPlayed?.ToLocalTime().ToString("g") ?? "–"}\nDatei-Inhalte werden vor der Übernahme erneut geprüft."; accept.IsEnabled = true;
            }
            catch (Exception ex) { plan = null; summary.Text = ex.Message; accept.IsEnabled = false; }
        }
        keeper.SelectionChanged += (_, _) => Update(); covers.SelectionChanged += (_, _) => Update(); aspects.SelectionChanged += (_, _) => Update(); Update();
        actions.Children.Add(accept); actions.Children.Add(ActionButton("Abbrechen", () => review.Close()));
        var selected = await review.ShowDialog<DuplicateMergePlan?>(owner);
        if (selected == null || isClosed || operation != null || !CanBackUp()) return false;
        DuplicateMergeResult? result = null; message.Text = "Datei-Inhalte werden erneut geprüft; danach Sicherung und Zusammenführung. Abbrechen ist während der Prüfung möglich.";
        var completed = await BusyAsync(async token =>
        {
            try { result = await DuplicateMerge.ApplyAsync(dataDir, selected, token); }
            finally
            {
                try { settings = settingsStore.Load(); if (!isClosed) { UpdateCollections(); Reload(selected.Keeper.Path); } }
                catch (Exception ex)
                {
                    if (isClosed) throw;
                    foreach (var dialog in OwnedWindows.ToArray()) dialog.Close(); var blocked = new StackPanel { Spacing = 16, Margin = new(24) };
                    blocked.Children.Add(new TextBlock { Text = "Zusammenführung konnte nicht wieder aufgenommen werden. Bitte EmuVora schließen und die Sicherung im Datenordner prüfen.\n" + ex.Message, TextWrapping = TextWrapping.Wrap }); blocked.Children.Add(ActionButton("EmuVora schließen", Close)); Content = blocked; throw;
                }
            }
        });
        if (!completed || result == null) { message.Text = status.Text; return false; }
        message.Text = status.Text = $"{result.Removed} doppelte Karten zusammengeführt. ROM-Dateien erhalten. Sicherung: {result.BackupDirectory}"; return true;
    }
}
