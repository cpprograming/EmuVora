using System.Text.RegularExpressions;

namespace EmuVora.Core;

public record ExternalSaveFolder(string Id,string Path)
{
    public override string ToString()=>$"{(Directory.Exists(Path)?"✓":"Nicht erreichbar:")} {Path}";
}

public static class ExternalSaves
{
    public static ExternalSaveFolder[] ValidateSelection(string dataDirectory,IEnumerable<ExternalSaveFolder> folders)
    {
        var selected=folders.ToArray();if(selected.Length>32)throw new IOException("Höchstens 32 zusätzliche Spielstandsordner auswählen.");
        var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var paths=new List<string>();
        foreach(var folder in selected)
        {
            if(folder==null||!ValidId(folder.Id)||!ids.Add(folder.Id)||!Path.IsPathFullyQualified(folder.Path))throw new IOException("Ungültiger zusätzlicher Spielstandsordner.");
            var full=Path.GetFullPath(folder.Path).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(full.Equals(Path.GetPathRoot(full)?.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),StringComparison.OrdinalIgnoreCase))throw new IOException("Bitte einen Spielstandsordner statt eines ganzen Laufwerks wählen.");
            if(Equal(full,dataDirectory)||Backups.Inside(dataDirectory,full))throw new IOException("Der Datenordner und seine übergeordneten Ordner sind keine zusätzlichen Spielstandsordner.");
            if(paths.Any(path=>Equal(path,full)||Backups.Inside(path,full)||Backups.Inside(full,path)))throw new IOException("Zusätzliche Spielstandsordner dürfen sich nicht überschneiden.");
            if(!Directory.Exists(full))throw new DirectoryNotFoundException("Spielstandsordner nicht erreichbar: "+full);
            Backups.NoLinks(full);paths.Add(full);
        }
        return selected.Select((folder,index)=>folder with {Path=paths[index]}).ToArray();
    }
    internal static bool ValidId(string? id)=>id!=null&&Regex.IsMatch(id,"^[a-f0-9]{32}$");
    internal static bool Equal(string a,string b)=>Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).Equals(Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),StringComparison.OrdinalIgnoreCase);
}
