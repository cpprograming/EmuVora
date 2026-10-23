namespace EmuVora.Core;

public static class GameCollections
{
    public static string ValidateName(string? name)
    {
        name=name?.Trim();
        if(string.IsNullOrEmpty(name)||name.Length>60||name.Any(char.IsControl))
            throw new InvalidDataException("Bitte einen Namen mit 1 bis 60 Zeichen ohne Steuerzeichen eingeben.");
        return name;
    }
    public static void Create(AppSettings settings,string name)
    {
        name=ValidateName(name);
        if(settings.Collections.Count>=100)throw new InvalidDataException("Höchstens 100 Sammlungen sind möglich.");
        if(!settings.Collections.TryAdd(name,new(StringComparer.OrdinalIgnoreCase)))throw new InvalidDataException("Eine Sammlung mit diesem Namen existiert bereits.");
    }
    public static void Rename(AppSettings settings,string previous,string name)
    {
        name=ValidateName(name);
        if(!settings.Collections.TryGetValue(previous,out var members))throw new InvalidDataException("Sammlung nicht gefunden.");
        if(!previous.Equals(name,StringComparison.OrdinalIgnoreCase)&&settings.Collections.ContainsKey(name))throw new InvalidDataException("Eine Sammlung mit diesem Namen existiert bereits.");
        settings.Collections.Remove(previous);settings.Collections.Add(name,members);
    }
    public static Dictionary<string,HashSet<string>> Copy(AppSettings settings)=>settings.Collections.ToDictionary(c=>c.Key,c=>new HashSet<string>(c.Value,StringComparer.OrdinalIgnoreCase),StringComparer.OrdinalIgnoreCase);
}
