using System.Buffers.Binary;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EmuVora.Core;

public record CoverBatchProgress(int Completed,int Total,int Found,int Missing,int Failed,string Title);
public record CoverBatchResult(int Found,int Missing,int Failed,IReadOnlyList<string> Report);

// Only whole normalized titles match: no fuzzy guesses that confuse sequels.
public sealed class CoverCatalog(HttpClient http,string cacheDirectory,string region="Europe",string? apiToken=null)
{
    private readonly Dictionary<string,IndexedCover[]> indexes=[];
    private readonly Dictionary<string,string> unavailable=[];
    private record IndexedCover(string Name,string Title,string Words);
    private static readonly Dictionary<string,string> Roman=new(){{"i","1"},{"ii","2"},{"iii","3"},{"iv","4"},{"v","5"},{"vi","6"},{"vii","7"},{"viii","8"},{"ix","9"},{"x","10"},{"xi","11"},{"xii","12"},{"xiii","13"}};
    private static string Words(string title)
    {
        var text=TitleText(title).Normalize(NormalizationForm.FormD);
        var tokens=Regex.Matches(text,@"[\p{L}\p{N}]+[\p{Mn}\p{L}\p{N}]*").Select(m=>new string(m.Value.Where(c=>System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)!=System.Globalization.UnicodeCategory.NonSpacingMark).Select(char.ToLowerInvariant).ToArray())).ToArray();
        return string.Join(" ",tokens.Select((word,i)=>i>0&&Roman.TryGetValue(word,out var number)?number:word).Where(w=>w is not ("the" or "a" or "an")).OrderBy(w=>w,StringComparer.Ordinal));
    }
    private static IndexedCover Indexed(string name)=>new(name,Normalize(Path.GetFileNameWithoutExtension(name)),Words(Path.GetFileNameWithoutExtension(name)));
    private static readonly Regex Tags=new(@"\([^)]*\)|\[[^]]*\]",RegexOptions.Compiled);
    private static readonly HashSet<string> Metadata=new(StringComparer.OrdinalIgnoreCase)
    {
        "USA","US","U","Europe","EU","E","Japan","JPN","J","World","Asia","Brazil","BR","Germany","France","Spain","Italy","UK","United Kingdom","Australia","Canada","China","Taiwan","Hong Kong","Korea","Russia","Sweden","Netherlands","Denmark","Finland","Norway","Portugal","Unknown",
        "SGB Enhanced","GB Compatible","Virtual Console","Rumble Version","Switch Online","Unl","Pirate","Hack","Proto","Prototype","Beta","Demo","Sample"
    };
    private static bool IsMetadata(string value)
    {
        value=value.Trim();
        if(value.Split(',').All(part=>Metadata.Contains(part.Trim())))return true;
        return Regex.IsMatch(value,@"^(?:(?:En|Fr|De|Es|It|Nl|Pt|Sv|Da|No|Fi|Pl|Ru|Ja|Ko|Zh|Cs|Hu|El|Tr|Ar|He)(?:,\s*|$))+$",RegexOptions.IgnoreCase)
            ||Regex.IsMatch(value,@"^(?:(?:Rev|Beta|Proto|Demo|Sample)\s+[A-Za-z0-9.+-]+|v\d+(?:\.\d+)*|\d{4}(?:-\d{2}){1,2}|\d+\s*(?:Mb|Mbit|KB))$",RegexOptions.IgnoreCase);
    }
    private static string TitleText(string title)=>Tags.Replace(title,m=>m.Value[0]=='['||IsMetadata(m.Value[1..^1])?" ":" "+m.Value[1..^1]+" ");
    private static string[] MetadataTags(string title)=>Tags.Matches(title).Where(m=>m.Value[0]=='['||IsMetadata(m.Value[1..^1])).Select(m=>m.Value).ToArray();
    private static string LookupTitle(string title)=>Regex.Replace(Regex.Replace(title,@"^\d{3,5}[ ._-]+(?=\p{L})",""),@"_(USA|EUR|EUROPE|JPN|JAPAN|WORLD)(?:_.*)?$","",RegexOptions.IgnoreCase).Replace('_',' ');
    private static readonly Dictionary<(string System,string Title),string> KnownTitles=new()
    {
        [("GB","harvestmoon")]="Harvest Moon GB",[("GBC","harvestmoon")]="Harvest Moon GB",
        [("GB","spidermanandthexmenarcadesrevenge")]="Spider-Man - X-Men - Arcade's Revenge",
        [("Saturn","megaman8anniversarycollectorsedition")]="Mega Man 8",
        [("SNES","bugsbunnyinrabbitrampage")]="Bugs Bunny - Rabbit Rampage"
    };
    private static string? MatchSystem(string title,IndexedCover[] names,string region,string? system)
    {
        var match=Match(title,names,region);if(match!=null)return match;
        return system!=null&&KnownTitles.TryGetValue((system,Normalize(LookupTitle(title))),out var alias)?Match(alias,names,region):null;
    }
    public static string Normalize(string title)
    {
        var text=TitleText(title).Trim();text=Regex.Replace(text,@"^(the|a|an)\s+", "",RegexOptions.IgnoreCase);text=Regex.Replace(text,@",\s*(the|a|an)\s*$", "",RegexOptions.IgnoreCase);text=text.Normalize(NormalizationForm.FormD);
        return new string(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }
    public static string? Match(string title,IEnumerable<string> names,string region="Europe",string? system=null)=>MatchSystem(title,names.Select(n=>Indexed(n)).ToArray(),region,system);
    private static string? Match(string title,IndexedCover[] names,string region)
    {
        var direct=MatchWhole(title,names,region);if(direct!=null)return direct;
        // Collection numbering and scene region suffixes are lookup hints only.
        // Never alter the library title or accept partial/fuzzy title matches.
        var clean=LookupTitle(title);
        var cleaned=clean==title?null:MatchWhole(clean,names,region);if(cleaned!=null)return cleaned;
        var aliases=clean.Split(" ~ ",StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
        if(aliases.Length>1)
            return aliases.Select(alias=>Match(alias,names,region)).Where(name=>name!=null).OrderBy(name=>name!.Contains("("+region,StringComparison.OrdinalIgnoreCase)?0:name.Contains("(World",StringComparison.OrdinalIgnoreCase)?1:2).ThenBy(name=>name,StringComparer.Ordinal).FirstOrDefault();
        if(Regex.IsMatch(clean,@"^\d"))return null;
        var key=Words(clean);if(key.Length==0)return null;
        var candidates=names.Where(n=>n.Words==key).ToArray();
        // Reordering and Roman numerals are permitted only for a single full title.
        // Multiple distinct catalog titles with the same words remain unresolved.
        return candidates.Select(n=>n.Title).Distinct(StringComparer.Ordinal).Count()==1?MatchWhole(candidates[0].Title+string.Concat(MetadataTags(clean)),names,region):null;
    }
    private static string? MatchWhole(string title,IndexedCover[] names,string region)
    {
        var exact=names.FirstOrDefault(n=>n.Name.Equals(title+".png",StringComparison.OrdinalIgnoreCase));if(exact!=null)return exact.Name;
        var key=Normalize(title);if(key.Length==0)return null;
        var tags=MetadataTags(title);
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
        if(unavailable.TryGetValue(repository,out var error))throw new IOException(error);
        if(indexes.TryGetValue(repository,out var cached))return cached;
        var file=Path.Combine(cacheDirectory,repository+".json");string[]? names=null;
        if(File.Exists(file)&&new FileInfo(file).Length<=12_000_000&&File.GetLastWriteTimeUtc(file)>DateTime.UtcNow.AddDays(-30))
            try{names=JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(file,token));}catch(JsonException){ }
        if(names==null)
        {
            try
            {
                using var request=new HttpRequestMessage(HttpMethod.Get,$"https://api.github.com/repos/libretro-thumbnails/{repository}/git/trees/master?recursive=1");request.Headers.UserAgent.ParseAdd("EmuVora/0.6");
                // Optional CI authentication applies only to api.github.com, never
                // to artwork requests or redirects configured on another client.
                if(!string.IsNullOrEmpty(apiToken))request.Headers.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",apiToken);
                using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
                if(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)throw new IOException("GitHub-Coverlimit erreicht. Später erneut versuchen; vorhandene Cover bleiben erhalten.");
                if(response.StatusCode==HttpStatusCode.NotFound){names=[];}
                else
                {
                    using var json=JsonDocument.Parse(await ReadBounded(response,24_000_000,token));
                    if(json.RootElement.TryGetProperty("truncated",out var truncated)&&truncated.GetBoolean())
                    {
                        names=await BoxartIndex(repository,token);
                    }
                    else
                    names=json.RootElement.GetProperty("tree").EnumerateArray().Where(e=>e.GetProperty("type").GetString()=="blob").Select(e=>e.GetProperty("path").GetString()!).Where(p=>p.StartsWith("Named_Boxarts/",StringComparison.Ordinal)&&p.EndsWith(".png",StringComparison.OrdinalIgnoreCase)&&!p[14..].Contains('/')).Select(p=>p[14..]).ToArray();
                }
                Directory.CreateDirectory(cacheDirectory);AtomicFile.WriteText(file,JsonSerializer.Serialize(names));
            }
            catch(Exception ex) when(ex is HttpRequestException or IOException){unavailable[repository]=ex.Message;throw;}
        }
        var index=await Task.Run(()=>names.Where(n=>!string.IsNullOrWhiteSpace(n)&&n.Length<512&&Path.GetFileName(n)==n&&n.EndsWith(".png",StringComparison.OrdinalIgnoreCase)).Take(100_000).Select(n=>Indexed(n)).ToArray(),token);
        indexes[repository]=index;return index;
    }
    private async Task<string[]> BoxartIndex(string repository,CancellationToken token)
    {
        async Task<JsonDocument> Tree(string revision)
        {
            using var request=new HttpRequestMessage(HttpMethod.Get,$"https://api.github.com/repos/libretro-thumbnails/{repository}/git/trees/{revision}");
            request.Headers.UserAgent.ParseAdd("EmuVora/0.14");
            if(!string.IsNullOrEmpty(apiToken))request.Headers.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",apiToken);
            using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
            if(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)throw new IOException("GitHub-Coverlimit erreicht. Später erneut versuchen; vorhandene Cover bleiben erhalten.");
            var json=JsonDocument.Parse(await ReadBounded(response,24_000_000,token));
            if(json.RootElement.TryGetProperty("truncated",out var cut)&&cut.GetBoolean()){json.Dispose();throw new IOException("Der Coverkatalog ist unvollständig.");}
            return json;
        }
        using var root=await Tree("master");
        var entry=root.RootElement.GetProperty("tree").EnumerateArray().FirstOrDefault(e=>e.GetProperty("type").GetString()=="tree"&&e.GetProperty("path").GetString()=="Named_Boxarts");
        if(entry.ValueKind==JsonValueKind.Undefined)return [];
        var sha=entry.GetProperty("sha").GetString()??"";
        if(sha.Length!=40||!sha.All(Uri.IsHexDigit))throw new IOException("Ungültiger Coverkatalog-Verweis.");
        using var boxarts=await Tree(sha);
        return boxarts.RootElement.GetProperty("tree").EnumerateArray().Where(e=>e.GetProperty("type").GetString()=="blob").Select(e=>e.GetProperty("path").GetString()!).Where(p=>!p.Contains('/')&&p.EndsWith(".png",StringComparison.OrdinalIgnoreCase)).ToArray();
    }
    public async Task<string?> DownloadAsync(Game game,string folder,CancellationToken token=default)
    {
        var lookupSystem=game.System;
        if(lookupSystem is "GB" or "GBC")lookupSystem=await GameBoyCoverSystem.DetectAsync(game.Path,token)??lookupSystem;
        var repository=CoverLookup.Repository(lookupSystem);if(repository==null)return null;
        var index=await Index(repository,token);var name=MatchSystem(game.Title,index,region,lookupSystem);if(name==null)return null;
        byte[] image=[];var visited=new HashSet<string>(StringComparer.Ordinal);
        for(var hop=0;hop<5;hop++)
        {
            if(!visited.Add(name))throw new IOException("Zyklischer Cover-Verweis in der Quelle.");
            var url=$"https://raw.githubusercontent.com/libretro-thumbnails/{repository}/master/Named_Boxarts/{Uri.EscapeDataString(name)}";
            using var response=await http.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,token);if(response.StatusCode==HttpStatusCode.NotFound)return null;
            if(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests){var error="GitHub-Coverlimit erreicht. Später erneut versuchen; vorhandene Cover bleiben erhalten.";unavailable[repository]=error;throw new IOException(error);}
            image=await ReadBounded(response,10_000_000,token);
            if(image.Length>=24&&image.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))break;
            // raw.githubusercontent.com returns a Git symlink's relative target as
            // text. Follow only another indexed filename in the same cover folder.
            var target=image.Length<=512?Encoding.UTF8.GetString(image).Trim():"";
            if(hop==4||!index.Any(n=>n.Name==target))throw new IOException("Die Coverquelle liefert weder ein PNG noch einen gültigen Cover-Verweis.");name=target;
        }
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
            try{var file=await DownloadAsync(game,folder,token);if(file!=null){saved(game,file);found++;}else{missing++;report.Add(game.System+" \u00b7 "+game.Title+": kein passender Titel im Coverkatalog.");}}
            catch(OperationCanceledException) when(token.IsCancellationRequested){throw;}
            catch(Exception ex) when(ex is HttpRequestException or IOException or TaskCanceledException or JsonException){failed++;report.Add(game.System+" \u00b7 "+game.Title+": "+ex.Message);}
            progress?.Report(new(++completed,pending.Length,found,missing,failed,game.Title));
        }
        return new(found,missing,failed,report);
    }
}
