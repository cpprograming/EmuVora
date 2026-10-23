namespace EmuVora.Core;

public static class CoverLibrary
{
    public static bool IsMissing(AppSettings settings,string gamePath)=>
        !settings.Covers.TryGetValue(gamePath,out var file)||!File.Exists(file);

    public static bool CanDownload(AppSettings settings,string gamePath)=>
        IsMissing(settings,gamePath)&&!settings.ProtectedCovers.Contains(gamePath);

    // New files are immutable: a preview or replacement never overwrites an existing cover.
    public static string CopyImage(string source,string directory)
    {
        var info=new FileInfo(source);
        if(!info.Exists||info.Length==0||info.Length>20*1024*1024)
            throw new IOException("Bitte ein Bild mit höchstens 20 MiB auswählen.");
        var extension=Path.GetExtension(source).ToLowerInvariant();
        if(extension is not (".png" or ".jpg" or ".jpeg" or ".webp" or ".bmp"))
            throw new IOException("Bitte PNG, JPEG, WebP oder BMP auswählen.");
        Directory.CreateDirectory(directory);
        var target=Path.Combine(directory,Guid.NewGuid().ToString("N")+extension);
        File.Copy(source,target,false);return target;
    }
}
