using System.Text.Json;

namespace EmuVora.Core;

public record SaveTransferResult(int Copied,int Preserved);

public static class SaveData
{
    internal static readonly Dictionary<string,string[]> Folders=new()
    {
        ["duckstation"]=["memcards","savestates"], ["pcsx2"]=["memcards","sstates"],
        ["dolphin"]=["User/GC","User/Wii","User/StateSaves"],
        ["ppsspp"]=["memstick/PSP/SAVEDATA","memstick/PSP/PPSSPP_STATE"]
    };
    private const string PendingFile="pending-save-restores.json";
    internal static void PrepareRestore(AppSettings settings,string oldRoot,string stage)
    {
        var pending=new Dictionary<string,string>();
        foreach(var (id,folders) in Folders)
        {
            if(!settings.EmulatorPaths.TryGetValue(id,out var executable)||!Backups.Inside(executable,Path.Combine(oldRoot,"emulators")))continue;
            var relative=Path.GetRelativePath(oldRoot,Path.GetDirectoryName(executable)!);
            if(folders.Any(folder=>Directory.Exists(Path.Combine(stage,relative,folder))))pending[id]=relative;
        }
        // Never trust pending instructions from an imported archive: derive them from its data.
        File.WriteAllText(Path.Combine(stage,PendingFile),JsonSerializer.Serialize(pending,SettingsStore.JsonOptions));
    }
    public static SaveTransferResult RestorePending(string id,string executable,string dataDirectory)
    {
        var pendingFile=Path.Combine(dataDirectory,PendingFile);
        if(!Folders.TryGetValue(id,out var folders)||!File.Exists(pendingFile))return new(0,0);
        Backups.NoLinks(pendingFile);
        if(new FileInfo(pendingFile).Length>64*1024)throw new IOException("Spielstandszuordnung ist zu groß.");
        var pending=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(pendingFile))??throw new IOException("Spielstandszuordnung ist ungültig.");
        if(!pending.TryGetValue(id,out var relative))return new(0,0);
        if(string.IsNullOrEmpty(relative)||Path.IsPathRooted(relative)||relative.Contains(':'))throw new IOException("Spielstandsquelle ist ungültig.");
        var source=Path.GetFullPath(Path.Combine(dataDirectory,relative));var destination=Path.GetDirectoryName(Path.GetFullPath(executable))!;
        var managed=Path.Combine(dataDirectory,"emulators");
        if(!Backups.Inside(source,managed)||!Backups.Inside(destination,managed))throw new IOException("Spielstände dürfen nur zwischen verwalteten Emulatorordnern übernommen werden.");
        Backups.NoLinks(source);Backups.NoLinks(destination);
        if(source.Equals(destination,StringComparison.OrdinalIgnoreCase))return new(0,0);
        if(Backups.Inside(source,destination)||Backups.Inside(destination,source))throw new IOException("Spielstandsquelle und Ziel müssen getrennt sein.");
        int copied=0,preserved=0;
        foreach(var folder in folders)
        {
            var origin=Path.Combine(source,folder);if(!Directory.Exists(origin))continue;
            var queue=new Stack<string>();queue.Push(origin);
            while(queue.TryPop(out var current))
            {
                Backups.NoLinks(current);
                foreach(var file in Directory.EnumerateFiles(current))
                {
                    Backups.NoLinks(file);var target=Path.Combine(destination,Path.GetRelativePath(source,file));Backups.NoLinks(target);
                    if(File.Exists(target)){preserved++;continue;}
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    var temporary=target+"."+Guid.NewGuid().ToString("N")+".tmp";
                    try{File.Copy(file,temporary,false);File.Move(temporary,target,false);copied++;}
                    finally{if(File.Exists(temporary))File.Delete(temporary);}
                }
                foreach(var child in Directory.EnumerateDirectories(current))queue.Push(child);
            }
        }
        // A failed copy leaves this entry pending. A retry copies only missing files.
        var report=Path.Combine(dataDirectory,"save-restore-report.txt");
        File.AppendAllText(report,$"{DateTimeOffset.Now:g} · {id}: {copied} Dateien übernommen, {preserved} vorhandene Dateien erhalten.\nQuelle bleibt erhalten: {source}\nZiel: {destination}\n\n");
        pending.Remove(id);AtomicFile.WriteText(pendingFile,JsonSerializer.Serialize(pending,SettingsStore.JsonOptions));
        return new(copied,preserved);
    }
}
