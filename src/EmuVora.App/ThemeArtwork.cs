using Avalonia.Media.Imaging;
using SkiaSharp;
using Svg.Skia;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace EmuVora.App;

public static class ThemeArtwork
{
    public static Bitmap Load(string file)
    {
        if(new FileInfo(file).Length>16*1024*1024)throw new IOException("Theme-Bild ist zu groß.");
        if(!Path.GetExtension(file).Equals(".svg",StringComparison.OrdinalIgnoreCase))
        {using var stream=File.OpenRead(file);return Bitmap.DecodeToWidth(stream,1920);}
        // Illustrator logos define local namespace entities in their DOCTYPE.
        // Bound expansion and disable external resolution, then remove the DTD.
        using var reader=XmlReader.Create(file,new(){DtdProcessing=DtdProcessing.Parse,XmlResolver=null,MaxCharactersFromEntities=65536,MaxCharactersInDocument=16*1024*1024});
        var document=XDocument.Load(reader);
        document.DocumentType?.Remove();
        foreach(var instruction in document.DescendantNodes().OfType<XProcessingInstruction>().ToArray())instruction.Remove();
        foreach(var node in document.Descendants())
        {
            if(node.Name.LocalName is "script" or "image" or "foreignObject")throw new IOException("SVG mit externen/aktiven Inhalten wird nicht geladen.");
            foreach(var attribute in node.Attributes())
                if(attribute.Name.LocalName.StartsWith("on",StringComparison.OrdinalIgnoreCase)||attribute.Name.LocalName=="href"&&!attribute.Value.StartsWith('#'))throw new IOException("SVG enthält externe Verweise.");
        }
        var text=document.ToString();
        if(text.Contains("@import",StringComparison.OrdinalIgnoreCase)||Regex.IsMatch(text,@"url\(\s*['"" ]*[^#'"" ]",RegexOptions.IgnoreCase))throw new IOException("SVG enthält externe Ressourcen.");
        using var svg=new SKSvg();svg.FromSvg(text);
        var picture=svg.Picture??throw new IOException("SVG konnte nicht gerendert werden.");var bounds=picture.CullRect;
        if(bounds.Width<=0||bounds.Height<=0||!float.IsFinite(bounds.Width)||!float.IsFinite(bounds.Height))throw new IOException("SVG-Abmessungen sind ungültig.");
        var scale=Math.Min(1920/bounds.Width,1080/bounds.Height);
        using var surface=SKSurface.Create(new SKImageInfo(Math.Max(1,(int)(bounds.Width*scale)),Math.Max(1,(int)(bounds.Height*scale))));
        surface.Canvas.Clear(SKColors.Transparent);surface.Canvas.Scale(scale);surface.Canvas.Translate(-bounds.Left,-bounds.Top);surface.Canvas.DrawPicture(picture);
        using var image=surface.Snapshot();using var png=image.Encode(SKEncodedImageFormat.Png,100);using var encoded=png.AsStream();return new Bitmap(encoded);
    }
}
