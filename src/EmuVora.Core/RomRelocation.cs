using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EmuVora.Core;

public record RomPathSuggestion(Game Game,string NewPath,long Length,DateTime LastWriteUtc);
public record RomRelocationPlan(string OldRoot,string NewRoot,IReadOnlyList<RomPathSuggestion> Suggestions,IReadOnlyList<string> Notes);
public record RomRelocationResult(int Changed,string BackupDirectory);

public static class RomRelocation
{
    private const string JournalName="rom-relocation-pending.json";
    private record PathChange(string OldPath,string NewPath);
    private record Journal(string OldSettings,string NewSettings,PathChange[] Changes);
    private static string Key(string path)=>Path.GetFullPath(path);
    private static bool Same(string first,string second)=>Key(first).Equals(Key(second),StringComparison.OrdinalIgnoreCase);
    private static string Root(string path)
    {
        if(!Path.IsPathFullyQualified(path))throw new IOException("Bitte vollständige Ordnerpfade angeben.");
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }
    public static RomRelocationPlan Preview(IEnumerable<Game> games,string oldRoot,string newRoot,CancellationToken token=default)
    {
        oldRoot=Root(oldRoot);newRoot=Root(newRoot);
        if(oldRoot.Equals(newRoot,StringComparison.OrdinalIgnoreCase))throw new IOException("Alter und neuer Ordner müssen unterschiedlich sein.");
        if(!Directory.Exists(newRoot))throw new DirectoryNotFoundException("Neuer ROM-Ordner ist nicht erreichbar.");
        Backups.NoLinks(newRoot);var all=games.ToArray();var known=all.Select(g=>Path.GetFullPath(g.Path)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var suggestions=new List<RomPathSuggestion>();var notes=new List<string>();
        var ambiguous=all.GroupBy(g=>Key(g.Path),StringComparer.OrdinalIgnoreCase).Where(g=>g.Count()>1).Select(g=>g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach(var game in all.DistinctBy(g=>g.Path,StringComparer.OrdinalIgnoreCase))
        {
            token.ThrowIfCancellationRequested();
            if(!Backups.Inside(game.Path,oldRoot)||File.Exists(game.Path))continue;
            if(ambiguous.Contains(Key(game.Path))){notes.Add(game.Title+": mehrere Bibliothekseinträge für denselben Originalpfad; nicht zusammengeführt.");continue;}
            var target=Path.GetFullPath(Path.Combine(newRoot,Path.GetRelativePath(oldRoot,game.Path)));
            if(!Backups.Inside(target,newRoot))throw new IOException("Zielpfad liegt außerhalb des neuen Ordners.");
            if(known.Contains(target)){notes.Add(game.Title+": Ziel bereits in der Bibliothek; nicht zusammengeführt.");continue;}
            if(!File.Exists(target)){notes.Add(game.Title+": gleicher relativer Dateipfad am Ziel nicht gefunden.");continue;}
            try
            {
                CheckTarget(target,newRoot);var file=new FileInfo(target);
                suggestions.Add(new(game,target,file.Length,file.LastWriteTimeUtc));
            }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){notes.Add(game.Title+": "+ex.Message);}
        }
        return new(oldRoot,newRoot,suggestions,notes);
    }
    private static void CheckTarget(string path,string root)
    {
        Backups.NoLinks(path);if(!File.Exists(path))throw new IOException("Zieldatei fehlt.");
        using(var readable=File.Open(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)){ }
        var extension=Path.GetExtension(path);
        if(!extension.Equals(".m3u",StringComparison.OrdinalIgnoreCase)&&!extension.Equals(".cue",StringComparison.OrdinalIgnoreCase))return;
        if(new FileInfo(path).Length>1024*1024)throw new IOException("Disc-Liste ist zu groß (höchstens 1 MiB).");
        var count=0;
        foreach(var line in File.ReadLines(path))
        {
            var entry=line.Trim();if(entry.Length==0||entry.StartsWith('#'))continue;
            if(extension.Equals(".cue",StringComparison.OrdinalIgnoreCase))
            {
                var match=Regex.Match(entry,"^FILE\\s+(?:\"([^\"]+)\"|(\\S+))",RegexOptions.IgnoreCase);
                if(!match.Success)continue;entry=match.Groups[1].Success?match.Groups[1].Value:match.Groups[2].Value;
            }
            if(Path.IsPathRooted(entry))throw new IOException("Disc-Liste enthält absolute Pfade; zuerst selbst korrigieren.");
            var dependency=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!,entry));
            if(!Backups.Inside(dependency,root))throw new IOException("Disc-Verweis liegt außerhalb des neuen ROM-Ordners.");
            Backups.NoLinks(dependency);if(!File.Exists(dependency))throw new IOException("Disc-Datei fehlt: "+entry);count++;
            // A referenced CUE must retain its relative FILE dependencies as well.
            if(extension.Equals(".m3u",StringComparison.OrdinalIgnoreCase)&&Path.GetExtension(dependency).Equals(".cue",StringComparison.OrdinalIgnoreCase))CheckTarget(dependency,root);
        }
        if(count==0)throw new IOException("Disc-Liste enthält keine passenden Dateien.");
    }
    public static RomRelocationResult Apply(string data,RomRelocationPlan plan,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();using var launchLock=Lock(data);RecoverCore(data);if(plan.Suggestions.Count==0)throw new IOException("Keine Zuordnungen ausgewählt.");
        var changes=plan.Suggestions.Select(s=>new PathChange(s.Game.Path,s.NewPath)).ToArray();ValidateChanges(changes);
        var settingsFile=Path.Combine(data,"settings.json");var oldJson=File.ReadAllText(settingsFile);
        var settings=new SettingsStore(settingsFile).Load();var all=new LibraryStore(Path.Combine(data,"library.db")).GetAll();
        var oldRoot=Root(plan.OldRoot);var newRoot=Root(plan.NewRoot);
        var targetSettings=settings.Covers.Keys.Concat(settings.GameAspectRatios.Keys).Concat(settings.ProtectedCovers).Where(Path.IsPathFullyQualified).Select(Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach(var item in plan.Suggestions)
        {
            if(!Backups.Inside(item.Game.Path,oldRoot)||!Backups.Inside(item.NewPath,newRoot)||!Path.GetFullPath(Path.Combine(newRoot,Path.GetRelativePath(oldRoot,item.Game.Path))).Equals(item.NewPath,StringComparison.OrdinalIgnoreCase))throw new IOException("Die Pfadvorschau ist ungültig.");
            ValidateFile(item,newRoot);
            if(targetSettings.Contains(Key(item.NewPath)))throw new IOException("Am Ziel sind bereits eigene Spieleinstellungen hinterlegt. Keine Zuordnung geändert.");
        }
        var pathsByOld=changes.ToDictionary(c=>Key(c.OldPath),c=>c.NewPath,StringComparer.OrdinalIgnoreCase);
        Move(settings.Covers,pathsByOld);Move(settings.GameAspectRatios,pathsByOld);Move(settings.ProtectedCovers,pathsByOld);
        foreach(var members in settings.Collections.Values)Move(members,pathsByOld);
        // Retain still available scan roots and roots containing other library entries.
        var moved=changes.Select(c=>c.OldPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        settings.RomFolders.RemoveAll(folder=>Path.IsPathFullyQualified(folder)&&(Same(folder,oldRoot)||Backups.Inside(folder,oldRoot))&&!Directory.Exists(folder)&&!all.Any(g=>Backups.Inside(g.Path,folder)&&!moved.Contains(g.Path)));
        if(!settings.RomFolders.Contains(newRoot,StringComparer.OrdinalIgnoreCase))settings.RomFolders.Add(newRoot);
        var newJson=JsonSerializer.Serialize(settings,SettingsStore.JsonOptions);
        var backup=Path.Combine(data,"relocation-backups",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(backup);
        File.Copy(settingsFile,Path.Combine(backup,"settings.json"));
        using var db=Open(data);using(var copy=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.Combine(backup,"library.db"),Pooling=false}.ToString())){copy.Open();db.BackupDatabase(copy);}
        var journalFile=Path.Combine(data,JournalName);var journal=new Journal(oldJson,newJson,changes);
        var journalJson=JsonSerializer.Serialize(journal,SettingsStore.JsonOptions);
        if(System.Text.Encoding.UTF8.GetByteCount(journalJson)>40*1024*1024)throw new IOException("Die Zuordnung ist zu groß. Bitte kleinere ROM-Ordner getrennt zuordnen.");
        try
        {
            using(var tx=db.BeginTransaction())
            {
                var currentPaths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using(var paths=db.CreateCommand()){paths.Transaction=tx;paths.CommandText="SELECT path FROM games";using var reader=paths.ExecuteReader();while(reader.Read())currentPaths.Add(Key(reader.GetString(0)));}
                foreach(var item in plan.Suggestions)
                {
                    token.ThrowIfCancellationRequested();ValidateFile(item,newRoot);
                    using var check=db.CreateCommand();check.Transaction=tx;check.CommandText="SELECT title,system,path,favorite,lastPlayed,playCount FROM games WHERE path=$old COLLATE NOCASE";check.Parameters.AddWithValue("$old",item.Game.Path);
                    using(var reader=check.ExecuteReader())
                    {
                        if(!reader.Read()||new Game(reader.GetString(0),reader.GetString(1),reader.GetString(2),reader.GetInt32(3)!=0,reader.IsDBNull(4)?null:DateTimeOffset.Parse(reader.GetString(4)),reader.GetInt32(5))!=item.Game)throw new IOException("Ein Bibliothekseintrag wurde seit der Vorschau geändert. Bitte erneut prüfen.");
                    }
                    if(currentPaths.Contains(Key(item.NewPath)))throw new IOException("Ein Zielpfad wurde bereits importiert. Bitte erneut prüfen.");
                }
                token.ThrowIfCancellationRequested();AtomicFile.WriteText(journalFile,journalJson);
                foreach(var change in changes)
                {
                    token.ThrowIfCancellationRequested();using var command=db.CreateCommand();command.Transaction=tx;command.CommandText="UPDATE games SET path=$new WHERE path=$old COLLATE NOCASE";command.Parameters.AddWithValue("$old",change.OldPath);command.Parameters.AddWithValue("$new",change.NewPath);
                    if(command.ExecuteNonQuery()!=1)throw new IOException("Bibliothek wurde zwischenzeitlich geändert.");
                }
                DuplicateMerge.RelocateAliases(db,tx,oldRoot,newRoot,pathsByOld);
                token.ThrowIfCancellationRequested();AtomicFile.WriteText(settingsFile,newJson);tx.Commit();
            }
            File.Delete(journalFile);return new(changes.Length,backup);
        }
        catch{RecoverCore(data);throw;}
    }
    private static void Move(Dictionary<string,string> values,Dictionary<string,string> paths)
    {
        foreach(var entry in values.ToArray())if(Path.IsPathFullyQualified(entry.Key)&&paths.TryGetValue(Key(entry.Key),out var target))
        {values.Remove(entry.Key);if(!values.TryAdd(target,entry.Value))throw new IOException("Mehrdeutige eigene Spieleinstellungen für einen Originalpfad.");}
    }
    private static void Move(HashSet<string> values,Dictionary<string,string> paths)
    {foreach(var path in values.ToArray())if(Path.IsPathFullyQualified(path)&&paths.TryGetValue(Key(path),out var target)){values.Remove(path);values.Add(target);}}
    private static void ValidateFile(RomPathSuggestion item,string root)
    {
        if(File.Exists(item.Game.Path))throw new IOException("Die ursprüngliche ROM-Datei ist wieder erreichbar. Keine Zuordnung geändert.");
        CheckTarget(item.NewPath,root);var file=new FileInfo(item.NewPath);
        if(file.Length!=item.Length||file.LastWriteTimeUtc!=item.LastWriteUtc)throw new IOException("Die Zieldatei wurde seit der Vorschau verändert. Bitte erneut prüfen.");
    }
    private static void ValidateChanges(PathChange[] changes)
    {
        if(changes.Length==0||changes.Length>100000||changes.Any(c=>!Path.IsPathFullyQualified(c.OldPath)||!Path.IsPathFullyQualified(c.NewPath)))throw new IOException("Ungültige Pfadzuordnung.");
        var oldPaths=changes.Select(c=>Key(c.OldPath)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if(oldPaths.Count!=changes.Length||changes.Select(c=>Key(c.NewPath)).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=changes.Length||changes.Any(c=>oldPaths.Contains(Key(c.NewPath))))throw new IOException("Ungültige oder mehrdeutige Pfadzuordnung.");
    }
    private static SqliteConnection Open(string data)
    {var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.Combine(data,"library.db"),Mode=SqliteOpenMode.ReadWrite,Pooling=false}.ToString());db.Open();return db;}
    public static void Recover(string data)
    {
        if(!File.Exists(Path.Combine(data,JournalName)))return;
        using var launchLock=Lock(data);RecoverCore(data);
    }
    private static FileStream Lock(string data)
    {
        try{return new FileStream(Path.Combine(data,".game-launch.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None,1,FileOptions.DeleteOnClose);}
        catch(IOException ex){throw new IOException("Bitte laufende ES-DE-Spielstarts oder eine andere ROM-Zuordnung zuerst beenden.",ex);}
    }
    private static void RecoverCore(string data)
    {
        var journalFile=Path.Combine(data,JournalName);if(!File.Exists(journalFile))return;
        if(new FileInfo(journalFile).Length>40*1024*1024)throw new IOException("Ausstehende ROM-Zuordnung ist beschädigt; Sicherung im Datenordner prüfen.");
        Journal journal;
        try{journal=JsonSerializer.Deserialize<Journal>(File.ReadAllText(journalFile),SettingsStore.JsonOptions)??throw new JsonException();ValidateChanges(journal.Changes);if(JsonSerializer.Deserialize<AppSettings>(journal.OldSettings,SettingsStore.JsonOptions)==null||JsonSerializer.Deserialize<AppSettings>(journal.NewSettings,SettingsStore.JsonOptions)==null)throw new JsonException();}
        catch(Exception ex) when(ex is JsonException or ArgumentException or NullReferenceException){throw new IOException("Ausstehende ROM-Zuordnung ist ungültig. Die Daten bleiben erhalten; Sicherung im Datenordner prüfen.",ex);}
        using var db=Open(data);bool allOld=true,allNew=true;
        foreach(var change in journal.Changes)
        {
            using var command=db.CreateCommand();command.CommandText="SELECT path FROM games WHERE path=$old COLLATE NOCASE OR path=$new COLLATE NOCASE";command.Parameters.AddWithValue("$old",change.OldPath);command.Parameters.AddWithValue("$new",change.NewPath);
            var paths=new List<string>();using(var reader=command.ExecuteReader())while(reader.Read())paths.Add(reader.GetString(0));
            allOld&=paths.Count==1&&paths[0].Equals(change.OldPath,StringComparison.OrdinalIgnoreCase);allNew&=paths.Count==1&&paths[0].Equals(change.NewPath,StringComparison.OrdinalIgnoreCase);
        }
        if(!allOld&&!allNew)throw new IOException("Ausstehende ROM-Zuordnung passt nicht zur Bibliothek. Keine Daten überschrieben; Sicherung im Datenordner prüfen.");
        var expected=allNew?journal.NewSettings:journal.OldSettings;var settingsFile=Path.Combine(data,"settings.json");
        if(!File.Exists(settingsFile)||File.ReadAllText(settingsFile)!=expected)AtomicFile.WriteText(settingsFile,expected);
        File.Delete(journalFile);
    }
}
