using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using SharpCompress.Archives;
using System.IO.Compression;

namespace EmuVora.Core;
public record EmulatorPackage(string Id,string Version,string Url,string Sha256,string Executable);
public record PackageCatalog(int SchemaVersion,DateTimeOffset GeneratedAt,EmulatorPackage[] Packages);
public record DownloadProgress(string Message,double? Fraction=null);
public sealed class PackageManager : IDisposable
{
    public const string CatalogUrl="https://raw.githubusercontent.com/cpprograming/EmuVora/main/catalog/packages.json";
    private readonly HttpClient http;
    public static readonly Dictionary<string,string> CoreIds=SystemCatalog.RetroSystems.ToDictionary(s=>s.Name,s=>s.Core);
    public PackageManager(HttpMessageHandler? handler=null)
    {
        http=handler==null?new(new HttpClientHandler {AllowAutoRedirect=false}):new(handler);
        http.Timeout=TimeSpan.FromMinutes(15);http.DefaultRequestHeaders.UserAgent.ParseAdd("EmuVora/0.2");
    }
    public async Task<PackageCatalog> GetCatalogAsync(CancellationToken token=default)
    {
        using var response=await http.GetAsync(CatalogUrl,token);response.EnsureSuccessStatusCode();
        var text=await response.Content.ReadAsStringAsync(token);
        var catalog=JsonSerializer.Deserialize<PackageCatalog>(text.TrimStart('\uFEFF'),SettingsStore.JsonOptions)??throw new InvalidDataException("Download-Katalog ist leer.");
        if(catalog.SchemaVersion!=1||catalog.Packages.Length>32)throw new InvalidDataException("Unbekannter Download-Katalog.");
        foreach(var package in catalog.Packages)ValidatePackage(package);
        return await ResolveCurrentReleasesAsync(catalog,token);
    }
    public async Task<PackageCatalog> ResolveCurrentReleasesAsync(PackageCatalog catalog,CancellationToken token=default)
    {
        foreach(var package in catalog.Packages)ValidatePackage(package);
        // Mutable GitHub tags (notably DuckStation's "latest") must be resolved again
        // so a previously published catalog cannot silently install different bytes.
        var updated=catalog.Packages.ToArray();
        foreach(var (id,repository,pattern) in new[]{("duckstation","stenzek/duckstation","^duckstation-windows-x64-release.zip$"),("pcsx2","PCSX2/pcsx2","windows-x64-Qt\\.7z$"),("ppsspp","hrydgard/ppsspp","Windows-x64\\.zip$")})
        {
            var index=Array.FindIndex(updated,p=>p.Id==id);if(index<0)continue;
            try
            {
                using var metadata=await http.GetAsync($"https://api.github.com/repos/{repository}/releases/latest",token);metadata.EnsureSuccessStatusCode();
                using var json=JsonDocument.Parse(await metadata.Content.ReadAsStringAsync(token));var release=json.RootElement;
                var asset=release.GetProperty("assets").EnumerateArray().FirstOrDefault(a=>Regex.IsMatch(a.GetProperty("name").GetString()??"",pattern));
                if(asset.ValueKind==JsonValueKind.Undefined)continue;
                var digest=asset.TryGetProperty("digest",out var hash)?hash.GetString():null;
                if(digest==null||!digest.StartsWith("sha256:"))continue;
                var package=updated[index] with {Version=release.GetProperty("tag_name").GetString()+"-"+asset.GetProperty("id").GetInt64(),Url=asset.GetProperty("browser_download_url").GetString()!,Sha256=digest[7..]};ValidatePackage(package);updated[index]=package;
            }
            catch(Exception ex) when(ex is HttpRequestException or JsonException or InvalidOperationException){ /* The pinned catalog remains a safe fallback. */ }
        }
        return catalog with {Packages=updated};
    }
    public static void ValidatePackage(EmulatorPackage package)
    {
        if(!Regex.IsMatch(package.Id,"^[a-z0-9_-]+$")||!Regex.IsMatch(package.Sha256,"^[a-fA-F0-9]{64}$"))throw new InvalidDataException("Ungültige Paket-Prüfsumme.");
        if(!EmulatorCatalog.Known.Any(e=>e.Id==package.Id)&&package.Id!="retroarch-cores")throw new InvalidDataException("Unbekanntes Paket.");
        var url=new Uri(package.Url);if(url.Scheme!="https"||url.UserInfo.Length!=0||url.Port!=443)throw new InvalidDataException("Ungültige Download-Adresse.");
        var valid=package.Id switch
        {
            "duckstation"=>url.Host=="github.com"&&url.AbsolutePath.StartsWith("/stenzek/duckstation/releases/download/",StringComparison.Ordinal),
            "pcsx2"=>url.Host=="github.com"&&url.AbsolutePath.StartsWith("/PCSX2/pcsx2/releases/download/",StringComparison.Ordinal),
            "ppsspp"=>url.Host=="github.com"&&url.AbsolutePath.StartsWith("/hrydgard/ppsspp/releases/download/",StringComparison.Ordinal),
            "dolphin"=>url.Host=="dl.dolphin-emu.org"&&url.AbsolutePath.StartsWith("/releases/",StringComparison.Ordinal),
            "retroarch" or "retroarch-cores"=>url.Host=="buildbot.libretro.com"&&url.AbsolutePath.StartsWith("/stable/",StringComparison.Ordinal),
            _=>false
        };
        if(!valid)throw new InvalidDataException("Der Download stammt nicht aus der offiziellen Paketquelle.");
        if(Path.GetFileName(package.Executable)!=package.Executable||!Regex.IsMatch(package.Executable,"^[a-zA-Z0-9_.-]+\\.(exe|dll)$",RegexOptions.IgnoreCase))throw new InvalidDataException("Ungültiger Paket-Einstiegspunkt.");
        if(!url.AbsolutePath.EndsWith(".zip",StringComparison.OrdinalIgnoreCase)&&!url.AbsolutePath.EndsWith(".7z",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Nur portable ZIP/7z-Pakete werden unterstützt.");
    }
    public async Task<string> InstallAsync(EmulatorPackage package,string root,IProgress<DownloadProgress>? progress=null,CancellationToken token=default)
    {
        ValidatePackage(package);var directory=Path.Combine(Path.GetFullPath(root),package.Id);Directory.CreateDirectory(directory);
        var staging=Path.Combine(directory,"staging-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(staging);
        var archive=Path.Combine(staging,"download"+Path.GetExtension(new Uri(package.Url).AbsolutePath));
        try
        {
            using(var response=await GetDownloadAsync(new Uri(package.Url),token))
            {
                response.EnsureSuccessStatusCode();var total=response.Content.Headers.ContentLength;
                if(total>1_500_000_000)throw new InvalidDataException("Download ist zu groß.");
                using var input=await response.Content.ReadAsStreamAsync(token);using var output=File.Create(archive);var buffer=new byte[81920];long received=0;int read;
                while((read=await input.ReadAsync(buffer,token))>0)
                {received+=read;if(received>1_500_000_000)throw new InvalidDataException("Download ist zu groß.");await output.WriteAsync(buffer.AsMemory(0,read),token);progress?.Report(new($"{package.Id}: {received/1024/1024} MB",total>0?(double)received/total:null));}
            }
            progress?.Report(new("Prüfsumme prüfen …"));await VerifyFileAsync(archive,package.Sha256,token);
            var extracted=Path.Combine(staging,"files");Directory.CreateDirectory(extracted);
            progress?.Report(new("Paket entpacken …"));await Task.Run(()=>ExtractSafely(archive,extracted,token),token);
            var executable=Directory.EnumerateFiles(extracted,"*",SearchOption.AllDirectories).FirstOrDefault(p=>Path.GetFileName(p).Equals(package.Executable,StringComparison.OrdinalIgnoreCase))??throw new InvalidDataException("Erwartete Emulator-Datei fehlt im Paket.");
            var relative=Path.GetRelativePath(extracted,executable);
            // Every installation is immutable; previous versions and user configurations remain intact.
            var final=Path.Combine(directory,package.Sha256[..16]+"-"+Guid.NewGuid().ToString("N")[..8]);token.ThrowIfCancellationRequested();Directory.Move(extracted,final);
            return Path.Combine(final,relative);
        }
        finally{if(Directory.Exists(staging))Directory.Delete(staging,true);}
    }
    private async Task<HttpResponseMessage> GetDownloadAsync(Uri url,CancellationToken token)
    {
        for(var redirect=0;redirect<6;redirect++)
        {
            var response=await http.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,token);
            if((int)response.StatusCode is not (301 or 302 or 303 or 307 or 308))return response;
            var location=response.Headers.Location;response.Dispose();if(location==null)throw new InvalidDataException("Download-Weiterleitung fehlt.");
            url=location.IsAbsoluteUri?location:new Uri(url,location);
            if(url.Scheme!="https"||url.UserInfo.Length>0||url.Port!=443||!(url.Host is "github.com" or "release-assets.githubusercontent.com" or "objects.githubusercontent.com" or "dl.dolphin-emu.org" or "buildbot.libretro.com"))throw new InvalidDataException("Unzulässige Download-Weiterleitung.");
        }
        throw new InvalidDataException("Zu viele Download-Weiterleitungen.");
    }
    public static async Task VerifyFileAsync(string file,string expected,CancellationToken token=default)
    {
        using var stream=File.OpenRead(file);var hash=await SHA256.HashDataAsync(stream,token);
        if(!CryptographicOperations.FixedTimeEquals(hash,Convert.FromHexString(expected)))throw new InvalidDataException("Prüfsumme stimmt nicht. Die bisherige Installation bleibt erhalten.");
    }
    public static string SafeEntryPath(string root,string key)
    {
        if(string.IsNullOrWhiteSpace(key)||key.Contains(':')||key.StartsWith('/')||key.StartsWith('\\'))throw new InvalidDataException("Unsicherer Archivpfad.");
        var fullRoot=Path.GetFullPath(root)+Path.DirectorySeparatorChar;
        var full=Path.GetFullPath(Path.Combine(fullRoot,key.Replace('\\',Path.DirectorySeparatorChar).Replace('/',Path.DirectorySeparatorChar)));
        if(!full.StartsWith(fullRoot,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Archivpfad verlässt den Installationsordner.");return full;
    }
    public static void ExtractSafely(string archiveFile,string destination,CancellationToken token=default)
    {
        if(Path.GetExtension(archiveFile).Equals(".zip",StringComparison.OrdinalIgnoreCase))
        {
            using var zip=ZipFile.OpenRead(archiveFile);long length=0;
            if(zip.Entries.Count>100000)throw new InvalidDataException("Zu viele Archivdateien.");
            foreach(var entry in zip.Entries)
            {
                token.ThrowIfCancellationRequested();var path=SafeEntryPath(destination,entry.FullName);
                if((entry.ExternalAttributes>>16&0xF000)==0xA000)throw new InvalidDataException("Archiv enthält einen Link.");
                if(entry.FullName.EndsWith('/')){Directory.CreateDirectory(path);continue;}
                if((length+=entry.Length)>4_000_000_000)throw new InvalidDataException("Archiv überschreitet das Größenlimit.");
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);using var input=entry.Open();using var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write);input.CopyTo(output);
            }
            return;
        }
        using var archive=ArchiveFactory.OpenArchive(archiveFile);using var reader=archive.ExtractAllEntries();long total=0;var count=0;
        while(reader.MoveToNextEntry())
        {
            token.ThrowIfCancellationRequested();var entry=reader.Entry;var path=SafeEntryPath(destination,entry.Key??"");
            if(entry.IsDirectory){Directory.CreateDirectory(path);continue;}
            if(!string.IsNullOrEmpty(entry.LinkTarget))throw new InvalidDataException("Archiv enthält einen Link.");
            if(++count>100000||(total+=entry.Size)>4_000_000_000)throw new InvalidDataException("Archiv überschreitet das Größenlimit.");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);using var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write);reader.WriteEntryTo(output);
        }
    }
    public void Dispose()=>http.Dispose();
}
