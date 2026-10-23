using System.Text.RegularExpressions;

namespace EmuVora.Core;

public record LibraryAuditGroup(string Kind, string Title, string System, IReadOnlyList<Game> Games);

public static class LibraryAudit
{
    // Require an explicit marker; region, revision and edition suffixes remain part of the identity.
    private static readonly Regex Disc = new(@"\s*[\(\[]\s*(?:disc|disk|cd)\s*0*(?<number>[1-9]\d?)(?:\s*(?:of|/)\s*\d+)?\s*[\)\]]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public static IReadOnlyList<LibraryAuditGroup> Inspect(IEnumerable<Game> games, CancellationToken token = default)
    {
        var titles = new Dictionary<(string, string), List<Game>>();
        var discs = new Dictionary<(string, string, string), List<(Game Game, int Number)>>();
        foreach (var game in games)
        {
            token.ThrowIfCancellationRequested();
            var system = game.System.ToUpperInvariant();
            var title = game.Title.Trim().ToUpperInvariant();
            if (!titles.TryGetValue((system, title), out var matches)) titles[(system, title)] = matches = [];
            matches.Add(game);
            var match = Disc.Match(game.Title);
            if (!match.Success || game.Path.EndsWith(".m3u", StringComparison.OrdinalIgnoreCase)) continue;
            var name = Disc.Replace(game.Title, "", 1).Trim();
            var folder = Path.GetDirectoryName(Path.GetFullPath(game.Path))!.ToUpperInvariant();
            var key = (system, name.ToUpperInvariant(), folder);
            if (!discs.TryGetValue(key, out var entries)) discs[key] = entries = [];
            entries.Add((game, int.Parse(match.Groups["number"].Value)));
        }
        var result = new List<LibraryAuditGroup>();
        foreach (var entries in titles.Values)
        {
            token.ThrowIfCancellationRequested();
            var distinct = entries.DistinctBy(g => Path.GetFullPath(g.Path), StringComparer.OrdinalIgnoreCase).ToArray();
            if (distinct.Length > 1) result.Add(new("Gleicher Titel – Inhalte ungeprüft", distinct[0].Title, distinct[0].System, distinct));
        }
        foreach (var entries in discs.Values)
        {
            token.ThrowIfCancellationRequested();
            if (entries.Select(e => e.Number).Distinct().Count() < 2) continue;
            var ordered = entries.OrderBy(e => e.Number).ThenBy(e => e.Game.Path, StringComparer.OrdinalIgnoreCase).Select(e => e.Game).ToArray();
            result.Add(new("Mehrdisc-Kandidat", Disc.Replace(ordered[0].Title, "", 1).Trim(), ordered[0].System, ordered));
        }
        return result.OrderBy(g => g.System).ThenBy(g => g.Title).ToArray();
    }
}
