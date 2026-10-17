using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace EmuVora.Core;

public record BackupEntry(string Path,long Size,string Sha256);
public record BackupManifest(int Format,string SourceDirectory,DateTimeOffset Created,IReadOnlyList<BackupEntry> Files,IReadOnlyList<ExternalSaveFolder>? ExternalFolders=null);

public static class Backups
{
    private const long MaximumBytes=20L*1024*1024*1024;
    private static readonly string[] Folders=["covers","saves","states"];
    // These are data folders only. Emulator executables, BIOS and ROMs are excluded.
    public static async Task CreateAsync(string source,string destination,CancellationToken token=default)
    {
        source=Path.GetFullPath(source);destination=Path.GetFullPath(destination);
        if(Inside(destination,source))throw new IOException("Sicherung bitte außerhalb des Datenordners speichern.");
        NoLinks(source);NoLinks(Path.GetDirectoryName(destination)!);
        var stage=Path.Combine(Path.GetDirectoryName(destination)!,".emuvora-backup-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
        var temporary=destination+"."+Guid.NewGuid().ToString("N")+".part";
        try
        {
            var files=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            var settingsFile=Path.Combine(source,"settings.json");NoLinks(settingsFile);
            var settings=new SettingsStore(settingsFile).Load();
            var external=ExternalSaves.ValidateSelection(source,settings.ExternalSaveFolders);
            foreach(var folder in external)if(Inside(destination,folder.Path))throw new IOException("Sicherung bitte außerhalb aller ausgewählten Spielstandsordner speichern.");
            files.Add("settings.json",settingsFile);
            var database=Path.Combine(source,"library.db");NoLinks(database);
            using(var input=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=database,Mode=SqliteOpenMode.ReadOnly}.ToString()))
            using(var output=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.Combine(stage,"library.db"),Pooling=false}.ToString()))
            {input.Open();output.Open();input.BackupDatabase(output);}
            files.Add("library.db",Path.Combine(stage,"library.db"));
            foreach(var folder in Folders)AddFolder(Path.Combine(source,folder));
            foreach(var (id,folders) in SaveData.Folders)
                if(settings.EmulatorPaths.TryGetValue(id,out var executable)&&Inside(executable,source))
                    foreach(var folder in folders)AddFolder(Path.Combine(Path.GetDirectoryName(executable)!,folder));
            foreach(var folder in external)AddFolder(folder.Path,"external-saves/"+folder.Id);
            void AddFolder(string folder,string? prefix=null)
            {
                if(!Directory.Exists(folder)){if(prefix!=null)throw new DirectoryNotFoundException("Spielstandsordner nicht erreichbar: "+folder);return;}
                var pending=new Stack<string>();pending.Push(folder);
                while(pending.TryPop(out var current))
                {
                    token.ThrowIfCancellationRequested();NoLinks(current);
                    foreach(var file in Directory.EnumerateFiles(current))
                    {
                        token.ThrowIfCancellationRequested();NoLinks(file);
                        var relative=prefix==null?Path.GetRelativePath(source,file):prefix+"/"+Path.GetRelativePath(folder,file).Replace('\\','/');
                        files.TryAdd(relative.Replace('\\','/'),file);if(files.Count>100000)throw new IOException("Zu viele Dateien für eine Sicherung.");
                    }
                    foreach(var child in Directory.EnumerateDirectories(current))pending.Push(child);
                }
            }
            var entries=new List<BackupEntry>();long total=0;
            using(var stream=new FileStream(temporary,FileMode.CreateNew))
            using(var archive=new ZipArchive(stream,ZipArchiveMode.Create))
            {
                foreach(var (relative,file) in files)
                {
                    token.ThrowIfCancellationRequested();NoLinks(file);
                    using var input=File.OpenRead(file);total=checked(total+input.Length);if(total>MaximumBytes)throw new IOException("Sicherung überschreitet 20 GiB.");
                    using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                    var entry=archive.CreateEntry("data/"+relative,CompressionLevel.Fastest);
                    using(var output=entry.Open()){var buffer=new byte[81920];int count;while((count=await input.ReadAsync(buffer,token))>0){hash.AppendData(buffer,0,count);await output.WriteAsync(buffer.AsMemory(0,count),token);}}
                    entries.Add(new(relative,input.Length,Convert.ToHexString(hash.GetHashAndReset())));
                }
                await using var manifest=archive.CreateEntry("manifest.json").Open();
                await JsonSerializer.SerializeAsync(manifest,new BackupManifest(2,source,DateTimeOffset.UtcNow,entries,external),SettingsStore.JsonOptions,token);
            }
            token.ThrowIfCancellationRequested();File.Move(temporary,destination,false);
        }
        finally{if(File.Exists(temporary))File.Delete(temporary);Directory.Delete(stage,true);}
    }
    public static BackupManifest Inspect(string archivePath)
    {
        using var archive=ZipFile.OpenRead(archivePath);return ReadManifest(archive);
    }
    private static BackupManifest ReadManifest(ZipArchive archive)
    {
        if(archive.Entries.Count>100001)throw new IOException("Zu viele Archiveinträge.");
        var entry=archive.GetEntry("manifest.json")??throw new IOException("Kein EmuVora-Sicherungsarchiv.");
        if(entry.Length>16*1024*1024)throw new IOException("Manifest ist zu groß.");
        using var stream=entry.Open();var manifest=JsonSerializer.Deserialize<BackupManifest>(stream,SettingsStore.JsonOptions)??throw new IOException("Manifest fehlt.");
        if(manifest.Format is not (1 or 2)||!Path.IsPathFullyQualified(manifest.SourceDirectory)||manifest.Files==null||manifest.Files.Count>100000)throw new IOException("Sicherungsformat ungültig.");
        var external=manifest.ExternalFolders??[];var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if(external.Count>32||manifest.Format==1&&external.Count>0)throw new IOException("Ungültige externe Ordnerliste.");
        foreach(var folder in external)if(folder==null||!ExternalSaves.ValidId(folder.Id)||!ids.Add(folder.Id)||!Path.IsPathFullyQualified(folder.Path))throw new IOException("Ungültiger externer Ordner im Archiv.");
        var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);long size=0;
        foreach(var file in manifest.Files)
        {
            if(file.Path==null||file.Sha256==null||file.Sha256.Length!=64||file.Size<0||!SafePath(file.Path)||!names.Add(file.Path))throw new IOException("Ungültiger Dateieintrag.");
            var parts=file.Path.Split('/');
            if(parts[0].Equals("external-saves",StringComparison.OrdinalIgnoreCase)&&(parts.Length<3||!ids.Contains(parts[1])))throw new IOException("Nicht zugeordnete externe Spielstände im Archiv.");
            size=checked(size+file.Size);if(size>MaximumBytes)throw new IOException("Sicherung überschreitet 20 GiB.");
            if(archive.GetEntry("data/"+file.Path)?.Length!=file.Size)throw new IOException("Dateiliste stimmt nicht mit dem Archiv überein.");
        }
        if(archive.Entries.Select(e=>e.FullName).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=archive.Entries.Count||archive.Entries.Count!=names.Count+1||!names.Contains("settings.json")||!names.Contains("library.db"))throw new IOException("Unvollständiges oder mehrdeutiges Archiv.");
        return manifest;
    }
    public static async Task<string> RestoreAsync(string archivePath,string parent,CancellationToken token=default)
    {
        parent=Path.GetFullPath(parent);NoLinks(parent);
        var stage=Path.Combine(parent,".emuvora-restore-"+Guid.NewGuid().ToString("N"));
        var destination=Path.Combine(parent,"EmuVora-Wiederhergestellt-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(stage);
        try
        {
            BackupManifest manifest;
            using(var archive=ZipFile.OpenRead(archivePath))
            {
                manifest=ReadManifest(archive);
                foreach(var file in manifest.Files)
                {
                    token.ThrowIfCancellationRequested();var target=Path.Combine(stage,file.Path.Replace('/',Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    await using var input=archive.GetEntry("data/"+file.Path)!.Open();
                    await using var output=new FileStream(target,FileMode.CreateNew);
                    using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);var buffer=new byte[81920];long count=0;int read;
                    while((read=await input.ReadAsync(buffer,token))>0){count+=read;if(count>file.Size)throw new IOException("Entpackte Datei ist zu groß.");hash.AppendData(buffer,0,read);await output.WriteAsync(buffer.AsMemory(0,read),token);}
                    if(count!=file.Size||!Convert.ToHexString(hash.GetHashAndReset()).Equals(file.Sha256,StringComparison.OrdinalIgnoreCase))throw new IOException("Integritätsprüfung fehlgeschlagen: "+file.Path);
                }
            }
            var settingsPath=Path.Combine(stage,"settings.json");
            var settings=JsonSerializer.Deserialize<AppSettings>(await File.ReadAllTextAsync(settingsPath,token),SettingsStore.JsonOptions)??throw new IOException("Ungültige Einstellungen.");
            SaveData.PrepareRestore(settings,manifest.SourceDirectory,stage);
            settings.ExternalSaveFolders=[];
            foreach(var folder in manifest.ExternalFolders??[])
            {
                Directory.CreateDirectory(Path.Combine(stage,"external-saves",folder.Id));
                settings.ExternalSaveFolders.Add(new(folder.Id,Path.Combine(destination,"external-saves",folder.Id)));
            }
            if(settings.ExternalSaveFolders.Count>0)
                await File.WriteAllTextAsync(Path.Combine(stage,"external-save-folders.json"),JsonSerializer.Serialize((manifest.ExternalFolders??[]).Select(folder=>new {OriginalPath=folder.Path,RestoredPath=Path.Combine(destination,"external-saves",folder.Id)}),SettingsStore.JsonOptions),token);
            string Rebase(string path)=>Inside(path,manifest.SourceDirectory)?Path.Combine(destination,Path.GetRelativePath(manifest.SourceDirectory,path)):path;
            foreach(var map in new[]{settings.Covers,settings.EmulatorPaths,settings.CorePaths})foreach(var key in map.Keys.ToArray())map[key]=Rebase(map[key]);
            settings.EmulationStationTheme=settings.EmulationStationTheme is{} theme?Rebase(theme):null;
            settings.EsDeExecutable=settings.EsDeExecutable is{} esde?Rebase(esde):null;
            settings.Packages.Clear(); // Emulator binaries are intentionally not part of the backup.
            await File.WriteAllTextAsync(settingsPath,JsonSerializer.Serialize(settings,SettingsStore.JsonOptions),token);
            using(var database=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.Combine(stage,"library.db"),Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString()))
            {database.Open();using var command=database.CreateCommand();command.CommandText="PRAGMA integrity_check";if(command.ExecuteScalar()?.ToString()!="ok")throw new IOException("Bibliotheksdatenbank ist beschädigt.");}
            token.ThrowIfCancellationRequested();Directory.Move(stage,destination);return destination;
        }
        finally{if(Directory.Exists(stage))Directory.Delete(stage,true);}
    }
    private static bool SafePath(string path)=>!Path.IsPathRooted(path)&&!path.Contains('\\')&&!path.Contains(':')&&path.Split('/').All(p=>p.Length>0&&p!="."&&p!=".."&&!p.EndsWith('.')&&!p.EndsWith(' ')&&p.IndexOfAny(Path.GetInvalidFileNameChars())<0);
    internal static bool Inside(string path,string root)=>Path.IsPathFullyQualified(path)&&Path.GetFullPath(path).StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);
    internal static void NoLinks(string path)
    {
        for(var current=Path.GetFullPath(path);current!=null;current=Path.GetDirectoryName(current))
            if((File.Exists(current)||Directory.Exists(current))&&(File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new IOException("Verknüpfte Dateien/Ordner werden nicht gesichert oder wiederhergestellt: "+current);
    }
}
