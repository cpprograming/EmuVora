namespace EmuVora.Core;

public sealed record LibraryQuery(string Search = "", string? System = null, string View = "Alle Spiele",
    int Sort = 0, string? Collection = null, string? SelectedPath = null, int Page = 0, bool FindSelectedPage = false);

public sealed record LibraryPage(IReadOnlyList<GameVariantGroup> Families, IReadOnlyList<Game> Games,
    int Index, int FileCount)
{
    public int TitleCount => Families.Count;
}

// Layouts consume the same filtered, grouped and bounded page; they never query or edit the database.
public static class LibraryPresentation
{
    public const int PageSize = 48;
    public static LibraryPage Create(IReadOnlyList<Game> games, AppSettings settings,
        IReadOnlyDictionary<string, DateTimeOffset> addedTimes, LibraryQuery query)
    {
        var text = query.Search.Trim();
        var filtered = games.Where(g => (query.System is null or "Alle Konsolen" || g.System == query.System)
            && (g.Title.Contains(text, StringComparison.OrdinalIgnoreCase) || g.System.Contains(text, StringComparison.OrdinalIgnoreCase)));
        filtered = query.View switch
        {
            "Favoriten" => filtered.Where(g => g.Favorite),
            "Zuletzt gespielt" => filtered.Where(g => g.LastPlayed != null),
            "Konsole zuordnen" => filtered.Where(g => !EmulatorCatalog.Systems.Contains(g.System)),
            "Fehlende Dateien" => filtered.Where(g => !File.Exists(g.Path)),
            "Fehlende Cover" => filtered.Where(g => CoverLibrary.IsMissing(settings, g.Path)),
            "Zuletzt hinzugefügt" => filtered.Where(g => addedTimes.ContainsKey(g.Path)),
            "Sammlung" => filtered.Where(g => query.Collection is {} name && settings.Collections.TryGetValue(name, out var members) && members.Contains(g.Path)),
            _ => filtered
        };
        var ordered = (query.Sort == 3 || query.View == "Zuletzt hinzugefügt"
            ? filtered.OrderByDescending(g => addedTimes.GetValueOrDefault(g.Path)).ThenBy(g => g.Title)
            : query.Sort == 2 || query.View == "Zuletzt gespielt" ? filtered.OrderByDescending(g => g.LastPlayed)
            : query.Sort == 1 ? filtered.OrderBy(g => g.System).ThenBy(g => g.Title) : filtered.OrderBy(g => g.Title)).ToArray();
        var families = settings.GroupGameVersions ? GameVariants.Group(ordered, query.SelectedPath)
            : ordered.Select(g => new GameVariantGroup(g, new[] { g })).ToArray();
        var page = query.Page;
        if (query.FindSelectedPage && query.SelectedPath != null)
        {
            var index = Array.FindIndex(families.ToArray(), f => f.Representative.Path.Equals(query.SelectedPath, StringComparison.OrdinalIgnoreCase));
            if (index >= 0) page = index / PageSize;
        }
        page = Math.Clamp(page, 0, Math.Max(0, (families.Count - 1) / PageSize));
        return new(families, families.Skip(page * PageSize).Take(PageSize).Select(f => f.Representative).ToArray(), page, ordered.Length);
    }
}
