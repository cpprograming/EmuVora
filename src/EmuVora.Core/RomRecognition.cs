using System.IO.Compression;
using System.Security.Cryptography;

namespace EmuVora.Core;

public record TitleSuggestion(string Path,string System,string PreviousTitle,string Title,long? SourceLength=null,DateTime? SourceLastWriteUtc=null);
public record RecognitionResult(IReadOnlyList<TitleSuggestion> Suggestions,IReadOnlyList<string> Notes);
public static class RomRecognition
{
    private const long MaximumRomBytes=64L*1024*1024;
    public static async Task<RecognitionResult> ScanAsync(IEnumerable<Game> games,RomDatabase database,IProgress<string>? progress=null,CancellationToken token=default)
    {
        var suggestions=new List<TitleSuggestion>();var notes=new List<string>();
        foreach(var game in games)
        {
            token.ThrowIfCancellationRequested();if(game.System!=database.System)continue;progress?.Report(game.Title);
            try
            {
                var extensions=SystemCatalog.Find(game.System)?.Extensions.Split(' ')??[];
                await using var file=new FileStream(game.Path,FileMode.Open,FileAccess.Read,FileShare.Read,81920,true);
                var sourceLength=file.Length;var sourceWrite=File.GetLastWriteTimeUtc(game.Path);
                (long Size,string Hash) identity;
                if(System.IO.Path.GetExtension(game.Path).Equals(".zip",StringComparison.OrdinalIgnoreCase))
                {
                    using var zip=new ZipArchive(file,ZipArchiveMode.Read,true);if(zip.Entries.Count>10000)throw new IOException("ZIP enthält zu viele Einträge.");
                    var entries=zip.Entries.Where(e=>extensions.Contains(System.IO.Path.GetExtension(e.FullName).TrimStart('.'),StringComparer.OrdinalIgnoreCase)).ToArray();
                    if(entries.Length!=1||entries[0].Length>MaximumRomBytes)throw new IOException("ZIP benötigt genau eine passende ROM mit höchstens 64 MiB.");
                    using var stream=entries[0].Open();identity=await HashAsync(stream,token);
                }
                else
                {
                    if(!extensions.Contains(System.IO.Path.GetExtension(game.Path).TrimStart('.'),StringComparer.OrdinalIgnoreCase)||file.Length>MaximumRomBytes)throw new IOException("Dateiformat oder Größe nicht unterstützt.");
                    identity=await HashAsync(file,token);
                }
                var title=database.Find(identity.Size,identity.Hash);
                if(title==null)notes.Add(game.Path+": kein eindeutiger Prüfsummentreffer.");
                else if(title!=game.Title)suggestions.Add(new(game.Path,game.System,game.Title,title,sourceLength,sourceWrite));
                else notes.Add(game.Path+": Titel bereits passend.");
            }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidDataException){notes.Add(game.Path+": "+ex.Message);}
        }
        return new(suggestions,notes);
    }
    private static async Task<(long,string)> HashAsync(Stream input,CancellationToken token)
    {
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA1);var buffer=new byte[81920];long size=0;int read;
        while((read=await input.ReadAsync(buffer,token))>0){size+=read;if(size>MaximumRomBytes)throw new IOException("ROM überschreitet 64 MiB.");hash.AppendData(buffer,0,read);}
        return (size,Convert.ToHexString(hash.GetHashAndReset()));
    }
}
