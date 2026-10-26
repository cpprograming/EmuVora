using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace EmuVora.Core;

public record GdiTrack(string Path,long Offset,int SectorSize);
public static class GdiManifest
{
    private static readonly Regex Row=new("^(\\d{1,10})\\s+(\\d{1,10})\\s+([04])\\s+(2048|2352)\\s+(?:\"([^\"]+)\"|(\\S+))\\s+(\\d{1,10})$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
    public static IReadOnlyList<GdiTrack> Read(string file,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();file=Path.GetFullPath(file);Backups.NoLinks(file);var folder=Path.GetDirectoryName(file)!;
        using var input=File.Open(file,FileMode.Open,FileAccess.Read,FileShare.Read);if(input.Length>1024*1024)throw new IOException("GDI-Datei ist größer als 1 MiB.");
        using var reader=new StreamReader(input,new UTF8Encoding(false,true),false);string text;try{text=reader.ReadToEnd();}catch(DecoderFallbackException ex){throw new IOException("GDI-Datei muss als UTF-8/ASCII vorliegen.",ex);}
        var lines=text.TrimStart('\uFEFF').Split('\n').Select(l=>l.Trim()).Where(l=>l.Length>0).ToArray();
        if(lines.Length==0||!int.TryParse(lines[0],NumberStyles.None,CultureInfo.InvariantCulture,out var count)||count<3||count>99||lines.Length!=count+1)throw new IOException("GDI-Trackzahl muss 3 bis 99 sein und den Trackzeilen entsprechen.");
        var result=new List<GdiTrack>();
        for(var i=1;i<=count;i++)
        {
            token.ThrowIfCancellationRequested();if(lines[i].Length>4096)throw new IOException("GDI-Trackzeile ist zu lang.");var match=Row.Match(lines[i]);
            if(!match.Success||!uint.TryParse(match.Groups[1].Value,out var track)||track!=i||!uint.TryParse(match.Groups[2].Value,out var fad)||!long.TryParse(match.Groups[7].Value,out var offset)||offset>int.MaxValue)throw new IOException("Ungültige oder nicht fortlaufende GDI-Trackzeile.");
            var type=match.Groups[3].Value;if(i==1&&type!="4"||i==2&&type!="0"||i==3&&(type!="4"||fad!=45000))throw new IOException("GDI-Starttracks entsprechen nicht dem unterstützten Dreamcast-Layout.");
            var name=match.Groups[5].Success?match.Groups[5].Value:match.Groups[6].Value;
            if(name!=name.Trim()||Path.IsPathRooted(name)||name.Contains(':')||name.Any(char.IsControl))throw new IOException("GDI-Tracks müssen relative Pfade im Disc-Ordner verwenden.");
            var path=Path.GetFullPath(Path.Combine(folder,name));if(!Backups.Inside(path,folder))throw new IOException("GDI-Track liegt außerhalb des Disc-Ordners.");Backups.NoLinks(path);
            using(var trackFile=File.Open(path,FileMode.Open,FileAccess.Read,FileShare.Read))if(trackFile.Length==0||offset>=trackFile.Length)throw new IOException("GDI-Track fehlt, ist leer oder sein Datei-Offset ist ungültig.");
            result.Add(new(path,offset,int.Parse(match.Groups[4].Value,CultureInfo.InvariantCulture)));
        }
        return result;
    }
}
