using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EmuVora.Core;

public record RomDatabaseSource(string System,string Path,string Commit,long Size,string Sha256);
public record RomIdentity(string Title,long Size,string Sha1);
public sealed class RomDatabase(string system,IEnumerable<RomIdentity> entries)
{
    public string System {get;}=system;
    private readonly Dictionary<(long,string),string[]> index=entries.GroupBy(e=>(e.Size,e.Sha1.ToUpperInvariant())).ToDictionary(g=>g.Key,g=>g.Select(e=>e.Title).Distinct(StringComparer.Ordinal).ToArray());
    public int Count=>index.Count;
    public string? Find(long size,string sha1)=>index.TryGetValue((size,sha1.ToUpperInvariant()),out var titles)&&titles.Length==1?titles[0]:null;
}
public static class RomDatabases
{
    private const int MaximumDatBytes=16*1024*1024;
    public static IReadOnlyList<RomDatabaseSource> Sources {get;}=LoadSources();
    private static RomDatabaseSource[] LoadSources()
    {
        using var stream=typeof(RomDatabases).Assembly.GetManifestResourceStream("EmuVora.Core.rom-databases.json")!;
        return JsonSerializer.Deserialize<RomDatabaseSource[]>(stream)??[];
    }
    public static async Task<RomDatabase> LoadAsync(RomDatabaseSource source,HttpClient http,string cacheDirectory,CancellationToken token=default)
    {
        if(source.Size<=0||source.Size>MaximumDatBytes||!Regex.IsMatch(source.Sha256,"\\A[0-9a-fA-F]{64}\\z")||!Regex.IsMatch(source.Commit,"\\A[0-9a-fA-F]{40}\\z")||!source.Path.StartsWith("metadat/no-intro/",StringComparison.Ordinal)||source.Path.Contains(".."))throw new InvalidDataException("Ungültige Metadatenquelle.");
        Backups.NoLinks(cacheDirectory);Directory.CreateDirectory(cacheDirectory);var file=System.IO.Path.Combine(cacheDirectory,source.Sha256.ToLowerInvariant()+".dat");Backups.NoLinks(file);
        bool Valid(byte[] data)=>data.LongLength==source.Size&&Convert.ToHexString(SHA256.HashData(data)).Equals(source.Sha256,StringComparison.OrdinalIgnoreCase);
        if(File.Exists(file)&&new FileInfo(file).Length==source.Size){var cached=await File.ReadAllBytesAsync(file,token);if(Valid(cached))return Parse(source.System,new StringReader(System.Text.Encoding.UTF8.GetString(cached)),token);}
        var url="https://raw.githubusercontent.com/libretro/libretro-database/"+source.Commit+"/"+string.Join('/',source.Path.Split('/').Select(Uri.EscapeDataString));
        using var response=await http.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,token);response.EnsureSuccessStatusCode();
        if(response.RequestMessage?.RequestUri?.Host!="raw.githubusercontent.com"||response.Content.Headers.ContentLength>source.Size)throw new IOException("Unerwartete Metadatenantwort.");
        using var input=await response.Content.ReadAsStreamAsync(token);using var memory=new MemoryStream();var buffer=new byte[81920];int read;
        while((read=await input.ReadAsync(buffer,token))>0){if(memory.Length+read>source.Size)throw new IOException("Metadatenkatalog zu groß.");await memory.WriteAsync(buffer.AsMemory(0,read),token);}
        var bytes=memory.ToArray();if(!Valid(bytes))throw new IOException("Prüfsumme des Metadatenkatalogs stimmt nicht.");
        var database=Parse(source.System,new StringReader(System.Text.Encoding.UTF8.GetString(bytes)),token);
        var temporary=file+"."+Guid.NewGuid().ToString("N")+".part";
        try{await File.WriteAllBytesAsync(temporary,bytes,token);token.ThrowIfCancellationRequested();File.Move(temporary,file,true);}finally{if(File.Exists(temporary))File.Delete(temporary);}
        return database;
    }
    public static RomDatabase Parse(string system,TextReader reader,CancellationToken token=default)
    {
        var start=new Regex(@"^game\s*\($",RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));
        var name=new Regex("^name\\s+\"((?:\\\\.|[^\"\\\\])*)\"\\s*$",RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));
        var rom=new Regex("^rom\\s*\\(\\s*name\\s+\"(?:\\\\.|[^\"\\\\])*\"\\s+size\\s+(\\d+)\\s+.*?\\bsha1\\s+([A-Fa-f0-9]{40})(?:\\s+.*?)?\\s*\\)$",RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));
        var entries=new List<RomIdentity>();bool inGame=false;string? title=null;string? line;long total=0;
        while((line=reader.ReadLine())!=null)
        {
            token.ThrowIfCancellationRequested();if(line.Length>16384||(total+=line.Length)>MaximumDatBytes)throw new InvalidDataException("Metadatenformat zu groß.");line=line.Trim();
            if(start.IsMatch(line)){inGame=true;title=null;continue;}if(!inGame)continue;if(line==")"){inGame=false;continue;}
            var named=name.Match(line);if(named.Success){title=named.Groups[1].Value.Replace("\\\"","\"").Replace("\\\\","\\");if(title.Length>512||title.Any(char.IsControl))throw new InvalidDataException("Ungültiger Spieltitel.");continue;}
            if(!line.StartsWith("rom ",StringComparison.Ordinal)&&!line.StartsWith("rom\t",StringComparison.Ordinal))continue;
            var matched=rom.Match(line);if(!matched.Success||string.IsNullOrWhiteSpace(title)||!long.TryParse(matched.Groups[1].Value,out var size)||size<=0)throw new InvalidDataException("Metadatenformat nicht unterstützt.");
            entries.Add(new(title,size,matched.Groups[2].Value));if(entries.Count>200000)throw new InvalidDataException("Zu viele Metadateneinträge.");
        }
        if(entries.Count==0||inGame)throw new InvalidDataException("Unvollständiger Metadatenkatalog.");return new(system,entries);
    }
}
