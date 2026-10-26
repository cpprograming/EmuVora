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
            content.Children.Add(new TextBlock { Text = "Eine Gruppe auswählen und deren Dateien per SHA-256 vergleichen oder einen Disc-Satz für PS1, Saturn, Dreamcast oder Mega-CD als neue Playlist vorbereiten. Verweisdateien (z. B. CUE/M3U) werden beim Inhaltsvergleich ausgelassen. Die Prüfung gilt für die Datei-Bytes, nicht für unterschiedliche Container desselben Spiels.", TextWrapping = TextWrapping.Wrap });
            var list = new ListBox { Height = 240, ItemsSource = groups, ItemTemplate = new FuncDataTemplate<LibraryAuditGroup>((group, _) => new TextBlock { Text = group == null ? "" : $"{group.Kind} · {group.System}\n{group.Title}\n" + string.Join("\n", group.Games.Select(g => g.Path)), TextWrapping = TextWrapping.Wrap, Margin = new(8) }) };
            content.Children.Add(list);
            var actions = new StackPanel { Spacing = 8 };
            var dialog = LibraryDialog("Duplikate und Mehrdisc-Spiele", content, actions);
            var message = new TextBlock { TextWrapping = TextWrapping.Wrap }; actions.Children.Add(message);
            actions.Children.Add(AsyncButton("Ausgewählte Dateien per SHA-256 vergleichen", async () =>
            {
                if (operation != null) return;
                if (list.SelectedItem is not LibraryAuditGroup selected) { message.Text = "Bitte zuerst eine Gruppe auswählen."; return; }
                message.Text = "SHA-256-Prüfung läuft. Große Disc-Dateien können länger dauern; Prüfung abbrechen bleibt verfügbar.";
                ContentCheck? check = null;
                if (!await BusyAsync(async token => check = await LibraryContent.CompareAsync(selected.Games, token)) || check == null || isClosed || !dialog.IsVisible) { message.Text = status.Text; return; }
                message.Text = $"{check.CheckedFiles} Dateien geprüft; {check.Matches.Count} Gruppen mit gleichen Datei-Bytes.";
                var result = new StackPanel { Spacing = 14, Margin = new(24) };
                result.Children.Add(new TextBlock { Text = $"{check.CheckedFiles} Dateien vollständig geprüft · {check.Matches.Count} Gruppen mit gleichen Datei-Bytes.\nErgebnis gilt zum Prüfzeitpunkt. Nichts wurde gelöscht oder zusammengeführt.", TextWrapping = TextWrapping.Wrap });
                var matches = new ListBox { Height = 230, ItemsSource = check.Matches, ItemTemplate = new FuncDataTemplate<ContentMatch>((match, _) => new TextBlock { Text = match == null ? "" : $"SHA-256: {match.Sha256}\n{match.Length:N0} Bytes\n" + string.Join("\n", match.Games.Select(g => g.Path)), TextWrapping = TextWrapping.Wrap, Margin = new(8) }) }; result.Children.Add(matches);
                if (check.Notes.Count > 0) result.Children.Add(new TextBlock { Text = string.Join("\n", check.Notes), TextWrapping = TextWrapping.Wrap });
                var resultActions = new StackPanel { Spacing = 8 };
                var review = LibraryDialog("Datei-Inhalte vergleichen", result, resultActions); var mergeStatus = new TextBlock { TextWrapping = TextWrapping.Wrap }; resultActions.Children.Add(mergeStatus);
                resultActions.Children.Add(AsyncButton("Ausgewählte Duplikate zusammenführen", async () =>
                {
                    if (matches.SelectedItem is not ContentMatch match) { mergeStatus.Text = "Bitte eine Gruppe mit gleichen Datei-Inhalten auswählen."; return; }
                    if (await ReviewDuplicateMergeAsync(match, review, mergeStatus)) matches.ItemsSource = matches.Items.Cast<ContentMatch>().Where(m => m != match).ToArray();
                }));
                resultActions.Children.Add(ActionButton("Zusammenführung abbrechen", RequestOperationCancel));
                resultActions.Children.Add(ActionButton("Schließen", () => { if (operation != null) RequestOperationCancel(); else review.Close(); })); await review.ShowDialog(dialog);
            }));
            actions.Children.Add(AsyncButton("Mehrdisc-Playlist vorbereiten", async () =>
            {
                if (list.SelectedItem is not LibraryAuditGroup selected || selected.Kind != "Mehrdisc-Kandidat") { message.Text = "Bitte einen Mehrdisc-Kandidaten auswählen."; return; }
                await PrepareDiscPlaylistAsync(selected, dialog, message);
            }));
            actions.Children.Add(ActionButton("Prüfung abbrechen", RequestOperationCancel));
            actions.Children.Add(ActionButton("Schließen", () => { if (operation != null) RequestOperationCancel(); else dialog.Close(); }));
            await dialog.ShowDialog(owner);
        }));
        return new Expander { Header = "Duplikate und Mehrdisc-Spiele", Content = panel };
    }

    private async Task PrepareDiscPlaylistAsync(LibraryAuditGroup group, Window owner, TextBlock message)
    {
        if (operation != null || !CanBackUp()) return;
        DiscPlaylistPlan? plan = null;
        if (!await BusyAsync(async token => plan = await Task.Run(() => LibraryContent.PreviewPlaylist(group, token), token)) || plan == null || isClosed || !owner.IsVisible) return;
        var content = new StackPanel { Spacing = 14, Margin = new(24) };
        content.Children.Add(new TextBlock { Text = "Neue Playlist:\n" + plan.Path + "\n\nDisc-Reihenfolge:\n" + plan.Content, TextWrapping = TextWrapping.Wrap });
        content.Children.Add(new TextBlock { Text = $"{group.System} · {LibraryContent.PlaylistFormats(group.System)} · {LibraryContent.PlaylistEmulator(group.System)}. Discs müssen vollständig nummeriert sein. Bitte Reihenfolge selbst prüfen. Eigene Core- und Emulatorprofile werden nicht verändert. Dreamcast-GDI wird einschließlich seiner Tracks geprüft. CDI und einzelne rohe Tracks werden nicht gebündelt.\n\nWichtig: Der neue Playlist-Name kann einen anderen Spielstands-/Memory-Card-Namen verwenden. Vorhandene Spielstände ggf. im Emulator zuordnen. EmuVora verschiebt oder benennt keine Spielstände um.\n\nEs entsteht eine zusätzliche Bibliothekskarte ohne bisherigen Verlauf. Die einzelnen Discs und ihre Favoriten, Cover und Verläufe bleiben erhalten. Bestehende Playlists werden nicht überschrieben.", TextWrapping = TextWrapping.Wrap });
        var actions = new StackPanel { Spacing = 8 };
        var review = LibraryDialog(group.System + "-Playlist erstellen", content, actions);
        actions.Children.Add(ActionButton("Playlist erstellen und hinzufügen", () => review.Close(true)));
        actions.Children.Add(ActionButton("Abbrechen", () => review.Close(false)));
        if (!await review.ShowDialog<bool>(owner) || isClosed || operation != null || !CanBackUp()) return;
        await BusyAsync(async token =>
        {
            var game = await Task.Run(() => LibraryContent.CreatePlaylist(plan, token), token);
            try { store.Upsert([game]); Reload(game.Path); message.Text = "Playlist hinzugefügt. Einzelne Discs und ihre Metadaten bleiben erhalten."; }
            catch { message.Text = "Playlist wurde erstellt, konnte aber nicht importiert werden. Ordner erneut scannen: " + game.Path; throw; }
        });
    }
}
