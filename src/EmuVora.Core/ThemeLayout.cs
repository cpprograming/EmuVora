using System.Globalization;

namespace EmuVora.Core;

public sealed record ThemePlacement(double X, double Y, double Width, double Height, double OriginX = 0, double OriginY = 0,
    int Layer = 0, double Opacity = 1, bool Visible = true,
    bool ProportionalImage = false, string TextAlignment = "left", string VerticalAlignment = "top")
{
    public double Left => X - Width * OriginX;
    public double Top => Y - Height * OriginY;
}
public sealed record ThemeLayout(IReadOnlyDictionary<string, ThemePlacement> Elements, double AspectRatio,
    bool Usable, IReadOnlyList<string> Notes)
{
    public static readonly ThemePlacement NativeList = new(.03, .04, .94, .92, Layer: 10000);
    public const string DecorationPrefix = "decoration:";
    public const int DecorationLimit = 32;
    public IReadOnlyDictionary<string, ThemeElement> Decorations { get; init; } = new Dictionary<string, ThemeElement>();
    public static ThemeLayout From(EmulationStationTheme? theme)
    {
        var elements = new Dictionary<string, ThemePlacement>(); var decorations = new Dictionary<string, ThemeElement>(); var notes = new HashSet<string>(); var usable = false;
        var ratio = 16d / 9;
        if (theme == null) return new(elements, ratio, false, ["Kein aktives Theme-Layout; native Liste wird verwendet."]);
        var parts = theme.AspectRatio.Split(':');
        if (parts.Length == 2 && Number(parts[0], out var rw) && Number(parts[1], out var rh) && rh > 0 && rw / rh is >= .5 and <= 4) ratio = rw / rh;
        foreach (var element in theme.Elements)
        {
            var role = element.Name switch
            {
                "gamelist" when element.Type == "textlist" => "gamelist",
                "background" or "logo" or "md_image" when element.Type == "image" => element.Name,
                "md_name" when element.Type == "text" => "md_name",
                "md_description" or "description" when element.Type == "text" => "md_description",
                _ => null
            };
            var decorative = false;
            if (role == null && element.Name != "canvas" && !element.Name.StartsWith("md_", StringComparison.OrdinalIgnoreCase) && !element.Name.StartsWith("game-", StringComparison.OrdinalIgnoreCase))
            {
                var content = element.Properties.GetValueOrDefault(element.Type == "image" ? "path" : "text");
                if (element.Type is "image" or "text" && !string.IsNullOrWhiteSpace(content) && !content.Contains("${"))
                {
                    if (decorations.Count >= DecorationLimit) { notes.Add("Dekorationslimit erreicht: höchstens 32 statische Elemente."); continue; }
                    role = DecorationPrefix + element.Type + ":" + element.Name; decorative = true;
                }
            }
            if (role == null) { if (element.Name != "canvas") notes.Add("Layout-Element nicht abgebildet: " + element.Type + " / " + element.Name); continue; }
            var defaults = role switch
            {
                "gamelist" => new ThemePlacement(.04, .18, .48, .75),
                "background" => new(0, 0, 1, 1, Layer: -100),
                "logo" => new(.56, .04, .36, .12, Layer: 20),
                "md_image" => new(.56, .22, .36, .55, Layer: 10),
                "md_name" => new(.56, .15, .4, .07, Layer: 20),
                _ => new ThemePlacement(.56, .8, .4, .16, Layer: 20)
            };
            var p = element.Properties; var hasPos = Pair(p.GetValueOrDefault("pos"), out var x, out var y) && x is >= -1 and <= 2 && y is >= -1 and <= 2;
            var proportional = element.Type == "image" && role != "background";
            var hasSize = Pair(p.GetValueOrDefault("size") ?? p.GetValueOrDefault("maxSize"), out var width, out var height)
                && width is >= 0 and <= 2 && height is >= 0 and <= 2 && (width > 0 || height > 0)
                && (proportional || width > 0 && height > 0);
            if (decorative && (!hasPos || !hasSize)) { notes.Add("Dekoration ohne gültige Position/Größe ausgelassen: " + element.Name); continue; }
            if (!hasPos) { x = defaults.X; y = defaults.Y; notes.Add("Native Ersatzposition: " + role); }
            if (!hasSize) { width = defaults.Width; height = defaults.Height; notes.Add("Native Ersatzgröße: " + role); }
            var origin = Pair(p.GetValueOrDefault("origin"), out var ox, out var oy) && ox is >= 0 and <= 1 && oy is >= 0 and <= 1;
            if (!origin) { ox = 0; oy = 0; if (p.ContainsKey("origin")) notes.Add("Ungültiger Ursprung ersetzt: " + role); }
            var z = Number(p.GetValueOrDefault("zIndex"), out var layer) ? (int)Math.Clamp(layer, -1000, 1000) : defaults.Layer;
            var opacity = Number(p.GetValueOrDefault("opacity"), out var alpha) ? Math.Clamp(alpha, 0, 1) : 1;
            var visible = !string.Equals(p.GetValueOrDefault("visible"), "false", StringComparison.OrdinalIgnoreCase) && p.GetValueOrDefault("visible") != "0";
            string Alignment(string key, string fallback, params string[] allowed)
            {
                var value = p.GetValueOrDefault(key)?.Trim().ToLowerInvariant();
                if (value == null) return fallback;
                if (allowed.Contains(value)) return value;
                notes.Add("Ungültige Textausrichtung ersetzt: " + role + " / " + key); return fallback;
            }
            var horizontal = element.Type == "text" ? Alignment("alignment", "left", "left", "center", "right") : "left";
            var vertical = element.Type == "text" ? Alignment("verticalAlignment", "top", "top", "center", "bottom") : "top";
            var placement = new ThemePlacement(x, y, width, height, ox, oy, z, opacity, visible, proportional, horizontal, vertical);
            if (role == "gamelist")
            {
                usable = hasPos && hasSize && placement.Left >= 0 && placement.Top >= 0 && placement.Left + width <= 1.001 && placement.Top + height <= 1.001 && width >= .2 && height >= .2 && visible && opacity >= .4;
                if (!usable) notes.Add("Spieleliste hat kein bedienbares Rechteck; native Liste wird verwendet.");
                // Keep the interactive list readable above decorative layers.
                placement = placement with { Layer = 10000, Opacity = Math.Max(.6, opacity), Visible = true };
            }
            elements[role] = placement;
            if (decorative)
            {
                decorations[role] = element;
                notes.Add("Statische Dekoration angepasst: " + element.Type + " / " + element.Name + (element.Type == "text" ? " (native Schrift; höchstens 512 Zeichen)" : " (proportional)"));
            }
        }
        if (!usable) notes.Add("Kein vollständig nutzbares Listenlayout erkannt; native Liste wird verwendet.");
        return new(elements, ratio, usable, notes.Order(StringComparer.Ordinal).ToArray()) { Decorations = decorations };
    }
    private static bool Number(string? text, out double value) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);
    private static bool Pair(string? text, out double x, out double y)
    {
        x = y = 0; var values = text?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return values?.Length == 2 && Number(values[0], out x) && Number(values[1], out y);
    }
}
