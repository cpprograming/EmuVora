using System.Text.Json;

namespace EmuVora.Core;

public sealed record ThemeSelection(string Variant = "", string ColorScheme = "", string AspectRatio = "");
public sealed record ThemeOption(string Id, string Label)
{
    public override string ToString() => string.IsNullOrWhiteSpace(Label) || Label == Id ? Id : Label + " · " + Id;
}
public sealed record ThemeOptions(IReadOnlyList<ThemeOption> Variants, IReadOnlyList<ThemeOption> ColorSchemes,
    IReadOnlyList<ThemeOption> AspectRatios, ThemeSelection Default);

public sealed partial class EmulationStationTheme
{
    internal const string SelectionFile = "emuvora-theme-options.json";
    public static ThemeOptions InspectOptions(string directory, bool preferDark = true)
    {
        var root = Path.GetFullPath(directory); Backups.NoLinks(root);
        var file = Path.Combine(root, "capabilities.xml");
        if (!File.Exists(file)) return new([], [], [], new());
        var xml = ReadXml(file);
        if (xml.Name.LocalName != "themeCapabilities") throw new IOException("Ungültige ES-DE-capabilities.xml.");
        IReadOnlyList<ThemeOption> Read(string kind, bool values = false) => xml.Elements(kind)
            .Where(e => !string.Equals(e.Element("selectable")?.Value.Trim(), "false", StringComparison.OrdinalIgnoreCase))
            .Select(e => new ThemeOption((values ? e.Value : (string?)e.Attribute("name") ?? "").Trim(), e.Element("label")?.Value.Trim() ?? ""))
            .Where(e => e.Id.Length > 0).GroupBy(e => e.Id, StringComparer.Ordinal).Select(g => g.First()).ToArray();
        var variants = Read("variant"); var schemes = Read("colorScheme"); var aspects = Read("aspectRatio", true);
        foreach (var options in new[] { variants, schemes, aspects })
            if (options.Count > 256 || options.Any(o => o.Id.Length > 256 || o.Label.Length > 512 || o.Id.Any(char.IsControl))) throw new IOException("Zu viele oder ungültige Theme-Optionen.");
        return new(variants, schemes, aspects, new(variants.FirstOrDefault()?.Id ?? "",
            schemes.FirstOrDefault(o => o.Id.Contains(preferDark ? "dark" : "light", StringComparison.OrdinalIgnoreCase))?.Id ?? schemes.FirstOrDefault()?.Id ?? "",
            aspects.FirstOrDefault(o => o.Id == "16:9")?.Id ?? aspects.FirstOrDefault()?.Id ?? ""));
    }
    private static ThemeSelection ResolveSelection(string root, bool preferDark, ThemeSelection? selection)
    {
        var options = InspectOptions(root, preferDark);
        var file = Path.Combine(root, SelectionFile);
        if (selection == null && File.Exists(file))
        {
            Backups.NoLinks(file);
            if (new FileInfo(file).Length > 65536) throw new IOException("Gespeicherte Theme-Auswahl ist zu groß.");
            try { selection = JsonSerializer.Deserialize<ThemeSelection>(File.ReadAllText(file), SettingsStore.JsonOptions) ?? throw new IOException("Theme-Auswahl ist leer."); }
            catch (JsonException ex) { throw new IOException("Gespeicherte Theme-Auswahl ist ungültig.", ex); }
        }
        selection ??= options.Default;
        void Check(string value, IReadOnlyList<ThemeOption> available)
        {
            if (value == null || (available.Count == 0 ? value.Length != 0 : !available.Any(o => o.Id == value))) throw new IOException("Die ausgewählte Theme-Option ist nicht verfügbar. Vorschau erneut prüfen.");
        }
        Check(selection.Variant, options.Variants); Check(selection.ColorScheme, options.ColorSchemes); Check(selection.AspectRatio, options.AspectRatios);
        return selection;
    }
}
