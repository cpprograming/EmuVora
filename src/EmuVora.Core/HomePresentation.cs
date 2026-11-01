namespace EmuVora.Core;

public sealed record HomeCollection(string Name, int Titles, int Files);
public sealed record HomeDashboard(IReadOnlyList<Game> Recent, IReadOnlyList<Game> Favorites,
    IReadOnlyList<HomeCollection> Collections, int Titles, int Files, int Systems);

public static class HomePresentation
{
    public const int GameLimit = 6;
    public const int CollectionLimit = 8;
    public static HomeDashboard Create(IReadOnlyList<Game> games, AppSettings settings)
    {
        IReadOnlyList<Game> Picks(string view) => LibraryPresentation.Create(games, settings,
            new Dictionary<string, DateTimeOffset>(), new(View: view)).Families.Take(GameLimit).Select(g => g.Representative).ToArray();
        int Titles(IEnumerable<Game> entries) => settings.GroupGameVersions ? GameVariants.Group(entries).Count : entries.Count();
        var collections = settings.HomeShowCollections ? settings.Collections.OrderBy(c => c.Key, StringComparer.OrdinalIgnoreCase).Take(CollectionLimit).Select(c =>
        {
            var members = new HashSet<string>(c.Value, StringComparer.OrdinalIgnoreCase); var included = games.Where(g => members.Contains(g.Path)).ToArray();
            return new HomeCollection(c.Key, Titles(included), included.Length);
        }).ToArray() : [];
        return new(settings.HomeShowRecent ? Picks("Zuletzt gespielt") : [], settings.HomeShowFavorites ? Picks("Favoriten") : [], collections,
            Titles(games), games.Count, games.Select(g => g.System).Distinct().Count());
    }
}
