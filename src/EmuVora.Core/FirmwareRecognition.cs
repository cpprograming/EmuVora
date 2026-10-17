using System.Security.Cryptography;
using System.Text.Json;

namespace EmuVora.Core;

public static class FirmwareRecognition
{
    // Identification metadata only, from libretro-database/dat/System.dat. No BIOS data.
    public record Signature(string Name,long Size,string Sha1);
    private static readonly Signature[] Known=Load();
    private static Signature[] Load()
    {
        using var stream=typeof(FirmwareRecognition).Assembly.GetManifestResourceStream("EmuVora.Core.firmware-signatures.json")!;
        return JsonSerializer.Deserialize<Signature[]>(stream)??[];
    }
    public static string? Identify(string source)=>Identify(source,Known);
    public static string? Identify(string source,IEnumerable<Signature> signatures)
    {
        var size=new FileInfo(source).Length;var candidates=signatures.Where(s=>s.Size==size).ToArray();
        if(candidates.Length==0)return null;
        using var stream=File.OpenRead(source);var hash=Convert.ToHexString(SHA1.HashData(stream));
        var names=candidates.Where(s=>s.Sha1.Equals(hash,StringComparison.OrdinalIgnoreCase)).Select(s=>s.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if(names.Length!=1)return null;
        var name=names[0];
        if(Path.IsPathRooted(name)||name.Split('/','\\').Any(p=>p is ".." or "." or ""))throw new InvalidDataException("Ungültiger Firmware-Zielname.");
        return name;
    }
}
