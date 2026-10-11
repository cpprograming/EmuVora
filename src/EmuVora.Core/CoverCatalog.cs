using System.Buffers.Binary;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EmuVora.Core;

public record CoverBatchProgress(int Completed,int Total,int Found,int Missing,int Failed,string Title);
public record CoverBatchResult(int Found,int Missing,int Failed,IReadOnlyList<string> Report);

// Only whole normalized titles match: no fuzzy guesses that confuse sequels.
public sealed class CoverCatalog(HttpClient http,string cacheDirectory,string region="Europe")
{
    private readonly Dictionary<string,IndexedCover[]> indexes=[];
    private readonly Dictionary<string,string> unavailable=[];
    private record IndexedCover(string Name,string Title);
    private static readonly Regex Tags=new(@"\([^)]*\)|\[[^]]*\]",RegexOptions.Compiled);
    public static string Normalize(string title)
    {
        var text=Tags.Replace(title,"").Normalize(NormalizationForm.FormD);
        return new string(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }
    public static string? Match(string title,IEnumerable<string> names,string region="Europe")=>Match(title,names.Select(n=>new IndexedCover(n,Normalize(Path.GetFileNameWithoutExtension(n)))).ToArray(),region);
    private static string? Match(string title,IndexedCover[] names,string region)
    {
        var exact=names.FirstOrDefault(n=>n.Name.Equals(title+".png",StringComparison.OrdinalIgnoreCase));if(exact!=null)return exact.Name;
        var key=Normalize(title);if(key.Length==0)return null;
        var tags=Tags.Matches(title).Select(m=>m.Value).ToArray();
        int Rank(string name)=>tags.Length>0&&tags.All(t=>name.Contains(t,StringComparison.OrdinalIgnoreCase))?0:name.Contains("("+region,StringComparison.OrdinalIgnoreCase)?1:name.Contains("(World",StringComparison.OrdinalIgnoreCase)?2:name.Contains("(USA",StringComparison.OrdinalIgnoreCase)?3:4;
        return names.Where(n=>n.Title==key).OrderBy(n=>Rank(n.Name)).ThenBy(n=>n.Name,StringComparer.Ordinal).Select(n=>n.Name).FirstOrDefault();
    }
    private async Task<byte[]> ReadBounded(HttpResponseMessage response,int maximum,CancellationToken token)
    {
        response.EnsureSuccessStatusCode();if(response.Content.Headers.ContentLength>maximum)throw new IOException("Die Coverquelle liefert eine zu große Datei.");
        using var input=await response.Content.ReadAsStreamAsync(token);using var output=new MemoryStream();var buffer=new byte[81920];int read;
        while((read=await input.ReadAsync(buffer,token))>0){if(output.Length+read>maximum)throw new IOException("Die Coverquelle liefert eine zu große Datei.");output.Write(buffer,0,read);}return output.ToArray();
    }
    private async Task<IndexedCover[]> Index(string repository,CancellationToken token)
    {
        if(indexes.TryGetValue(repository,out var cached))return cached;
        if(unavailable.TryGetValue(repository,out var error))throw new IOException(error);
        var file=Path.Combine(cacheDirectory,repository+".json");string[]? names=null;
        if(File.Exists(file)&&new FileInfo(file).Length<=12_000_000&&File.GetLastWriteTimeUtc(file)>DateTime.UtcNow.AddDays(-30))
            try{names=JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(file,token));}catch(JsonException){ }
        if(names==null)
        {
            try
            {
                using var request=new HttpRequestMessage(HttpMethod.Get,$"https://api.github.com/repos/libretro-thumbnails/{repository}/git/trees/master?recursive=1");request.Headers.UserAgent.ParseAdd("EmuVora/0.6");
                using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
                if(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)throw new IOException("GitHub-Coverlimit erreicht. Später erneut versuchen; vorhandene Cover bleiben erhalten.");
                if(response.StatusCode==HttpStatusCode.NotFound){names=[];}
                else
                {
                    using var json=JsonDocument.Parse(await ReadBounded(response,24_000_000,token));
                    if(json.RootElement.TryGetProperty("truncated",out var truncated)&&truncated.GetBoolean())throw new IOException("Der Coverkatalog ist unvollständig.");
                    names=json.RootElement.GetProperty("tree").EnumerateArray().Where(e=>e.GetProperty("type").GetString()=="blob").Select(e=>e.GetProperty("path").GetString()!).Where(p=>p.StartsWith("Named_Boxarts/",StringComparison.Ordinal)&&p.EndsWith(".png",StringComparison.OrdinalIgnoreCase)&&!p[14..].Contains('/')).Select(p=>p[14..]).ToArray();
                }
                Directory.CreateDirectory(cacheDirectory);AtomicFile.WriteText(file,JsonSerializer.Serialize(names));
            }
            catch(Exception ex) when(ex is HttpRequestException or IOException){unavailable[repository]=ex.Message;throw;}
        }
        var index=await Task.Run(()=>names.Where(n=>!string.IsNullOrWhiteSpace(n)&&n.Length<512&&Path.GetFileName(n)==n&&n.EndsWith(".png",StringComparison.OrdinalIgnoreCase)).Take(100_000).Select(n=>new IndexedCover(n,Normalize(Path.GetFileNameWithoutExtension(n)))).ToArray(),token);
        indexes[repository]=index;return index;
    }
    public async Task<string?> DownloadAsync(Game game,string folder,CancellationToken token=default)
    {
        var repository=CoverLookup.Repository(game.System);if(repository==null)return null;
        var name=Match(game.Title,await Index(repository,token),region);if(name==null)return null;
        var url=$"https://raw.githubusercontent.com/libretro-thumbnails/{repository}/master/Named_Boxarts/{Uri.EscapeDataString(name)}";
        using var response=await http.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,token);if(response.StatusCode==HttpStatusCode.NotFound)return null;
        var image=await ReadBounded(response,10_000_000,token);
        if(image.Length<24||!image.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))throw new IOException("Die Coverquelle liefert kein PNG-Bild.");
        var width=BinaryPrimitives.ReadUInt32BigEndian(image.AsSpan(16,4));var height=BinaryPrimitives.ReadUInt32BigEndian(image.AsSpan(20,4));
        if(width==0||height==0||width>8192||height>8192||width* (long)height>32_000_000)throw new IOException("Ungültige Covergröße.");
        Directory.CreateDirectory(folder);var file=Path.Combine(folder,EsDeFrontend.GameId(game)+".png");var temporary=file+".new";
        try{await File.WriteAllBytesAsync(temporary,image,token);token.ThrowIfCancellationRequested();File.Move(temporary,file,true);}finally{if(File.Exists(temporary))File.Delete(temporary);}return file;
    }
    public async Task<CoverBatchResult> DownloadMissingAsync(IEnumerable<Game> games,Dictionary<string,string> covers,string folder,Action<Game,string> saved,IProgress<CoverBatchProgress>? progress=null,CancellationToken token=default)
    {
        var pending=games.Where(g=>!covers.TryGetValue(g.Path,out var file)||!File.Exists(file)).ToArray();int found=0,missing=0,failed=0,completed=0;var report=new List<string>();
        foreach(var game in pending)
        {
            token.ThrowIfCancellationRequested();
            try{var file=await DownloadAsync(game,folder,token);if(file!=null){saved(game,file);found++;}else{missing++;report.Add(game.System+" · "+game.Title+": kein passender Titel im Coverkatalog.");}}
            catch(OperationCanceledException) when(token.IsCancellationRequested){throw;}
            catch(Exception ex) when(ex is HttpRequestException or IOException or TaskCanceledException or JsonException){failed++;report.Add(game.System+" · "+game.Title+": "+ex.Message);}
            progress?.Report(new(++completed,pending.Length,found,missing,failed,game.Title));
        }
        return new(found,missing,failed,report);
    }
}
