namespace EmuVora.Core;

public enum LibraryFilterKind { Search, System, View, Collection }
public sealed record LibraryFilter(LibraryFilterKind Kind, string Label);
public static class LibraryFilters
{
    public static IReadOnlyList<LibraryFilter> Describe(LibraryQuery query)
    {
        var filters = new List<LibraryFilter>(); var text = query.Search.Trim();
        if (text.Length > 0) filters.Add(new(LibraryFilterKind.Search, "Suche: " + text));
        if (query.System is not (null or "Alle Konsolen")) filters.Add(new(LibraryFilterKind.System, "Konsole: " + query.System));
        if (query.View == "Sammlung") filters.Add(new(LibraryFilterKind.Collection, "Sammlung: " + (string.IsNullOrEmpty(query.Collection) ? "keine ausgewählt" : query.Collection)));
        else if (query.View is not ("Alle Spiele" or "Startseite")) filters.Add(new(LibraryFilterKind.View, "Ansicht: " + query.View));
        return filters;
    }
}
