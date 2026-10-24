namespace EmuVora.Core;

public record GameVariantGroup(Game Representative, IReadOnlyList<Game> Versions);
public static class GameVariants
{
    // Names are a display grouping only; different regions/editions are not stripped and no content identity is assumed.
    public static IReadOnlyList<GameVariantGroup> Group(IEnumerable<Game> games, string? preferredPath = null) => games
        .GroupBy(g => (g.System, Title: g.Title.Trim().ToUpperInvariant()))
        .Select(group =>
        {
            var versions = group.OrderByDescending(g => g.LastPlayed).ThenByDescending(g => g.Favorite).ThenBy(g => g.Path, StringComparer.OrdinalIgnoreCase).ToArray();
            return new GameVariantGroup(versions.FirstOrDefault(g => g.Path.Equals(preferredPath, StringComparison.OrdinalIgnoreCase)) ?? versions[0], versions);
        }).ToArray();
}
