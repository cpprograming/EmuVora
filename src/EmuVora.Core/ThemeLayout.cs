using System.Globalization;

namespace EmuVora.Core;

public sealed record ThemePlacement(double X, double Y, double Width, double Height, double OriginX = 0, double OriginY = 0,
    int Layer = 0, double Opacity = 1, bool Visible = true)
{
    public double Left => X - Width * OriginX;
    public double Top => Y - Height * OriginY;
}
public sealed record ThemeLayout(IReadOnlyDictionary<string, ThemePlacement> Elements, double AspectRatio,
    bool Usable, IReadOnlyList<string> Notes)
{
    public static readonly ThemePlacement NativeList = new(.03, .04, .94, .92, Layer: 10000);
    public static ThemeLayout From(EmulationStationTheme? theme)
    {
        var elements = new Dictionary<string, ThemePlacement>(); var notes = new HashSet<string>(); var usable = false;
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
            var hasSize = Pair(p.GetValueOrDefault("size") ?? p.GetValueOrDefault("maxSize"), out var width, out var height) && width is > 0 and <= 2 && height is > 0 and <= 2;
            if (!hasPos) { x = defaults.X; y = defaults.Y; notes.Add("Native Ersatzposition: " + role); }
            if (!hasSize) { width = defaults.Width; height = defaults.Height; notes.Add("Native Ersatzgröße (auch automatische Null-Achsen): " + role); }
            var origin = Pair(p.GetValueOrDefault("origin"), out var ox, out var oy) && ox is >= 0 and <= 1 && oy is >= 0 and <= 1;
            if (!origin) { ox = 0; oy = 0; if (p.ContainsKey("origin")) notes.Add("Ungültiger Ursprung ersetzt: " + role); }
            var z = Number(p.GetValueOrDefault("zIndex"), out var layer) ? (int)Math.Clamp(layer, -1000, 1000) : defaults.Layer;
            var opacity = Number(p.GetValueOrDefault("opacity"), out var alpha) ? Math.Clamp(alpha, 0, 1) : 1;
            var visible = !string.Equals(p.GetValueOrDefault("visible"), "false", StringComparison.OrdinalIgnoreCase) && p.GetValueOrDefault("visible") != "0";
            var placement = new ThemePlacement(x, y, width, height, ox, oy, z, opacity, visible);
            if (role == "gamelist")
            {
                usable = hasPos && hasSize && placement.Left >= 0 && placement.Top >= 0 && placement.Left + width <= 1.001 && placement.Top + height <= 1.001 && width >= .2 && height >= .2 && visible && opacity >= .4;
                if (!usable) notes.Add("Spieleliste hat kein bedienbares Rechteck; native Liste wird verwendet.");
                // Keep the interactive list readable above decorative layers.
                placement = placement with { Layer = 10000, Opacity = Math.Max(.6, opacity), Visible = true };
            }
            elements[role] = placement;
        }
        if (!usable) notes.Add("Kein vollständig nutzbares Listenlayout erkannt; native Liste wird verwendet.");
        return new(elements, ratio, usable, notes.Order(StringComparer.Ordinal).ToArray());
    }
    private static bool Number(string? text, out double value) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);
    private static bool Pair(string? text, out double x, out double y)
    {
        x = y = 0; var values = text?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return values?.Length == 2 && Number(values[0], out x) && Number(values[1], out y);
    }
}
