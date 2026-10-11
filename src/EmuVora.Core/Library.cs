using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO.Compression;

namespace EmuVora.Core;

public record Game(string Title,string System,string Path,bool Favorite=false,DateTimeOffset? LastPlayed=null,int PlayCount=0);
public record ScanResult(IReadOnlyList<Game> Games,IReadOnlyList<string> Warnings);
public record EmulatorDefinition(string Id,string Name,string[] Systems,string Homepage,string ExecutableHint);
public static class EmulatorCatalog
{
    public static readonly EmulatorDefinition[] Known =
    [
        new("retroarch","RetroArch",SystemCatalog.RetroSystems.Select(s=>s.Name).ToArray(),"https://www.retroarch.com/","retroarch.exe"),
        new("duckstation","DuckStation",["PS1"],"https://github.com/stenzek/duckstation","duckstation-qt-x64-ReleaseLTCG.exe"),
        new("pcsx2","PCSX2",["PS2"],"https://pcsx2.net/","pcsx2-qt.exe"),
        new("dolphin","Dolphin",["GameCube","Wii"],"https://dolphin-emu.org/","Dolphin.exe"),
        new("ppsspp","PPSSPP",["PSP"],"https://www.ppsspp.org/","PPSSPPWindows64.exe")
    ];
    public static string[] Systems=>Known.SelectMany(e=>e.Systems).Distinct().OrderBy(s=>s).ToArray();
    public static EmulatorDefinition Get(string id)=>Known.First(e=>e.Id==id);
}
public static class RomScanner
{
    private static readonly Dictionary<string,string> Extensions=new(StringComparer.OrdinalIgnoreCase)
    {
        [".nes"]="NES",[".sfc"]="SNES",[".smc"]="SNES",[".gb"]="GB",[".gbc"]="GBC",[".gba"]="GBA",
        [".n64"]="N64",[".z64"]="N64",[".v64"]="N64",[".gcm"]="GameCube",[".rvz"]="Unknown disc",
        [".wbfs"]="Wii",[".cso"]="PSP",[".chd"]="Unknown disc",[".cue"]="Unknown disc",[".iso"]="Unknown disc",
        [".m3u"]="Unknown disc",[".md"]="Mega Drive",[".gen"]="Mega Drive",[".sms"]="Master System"
    };
    private static readonly Dictionary<string,string> Folders=new(StringComparer.OrdinalIgnoreCase)
    { ["PS1"]="PS1",["PSX"]="PS1",["PlayStation"]="PS1",["PS2"]="PS2",["PlayStation 2"]="PS2",["PSP"]="PSP",["Wii"]="Wii",["GameCube"]="GameCube",["GC"]="GameCube" };
    public static IReadOnlyList<Game> Scan(string folder)=>ScanDetailed(folder).Games;
    public static ScanResult ScanDetailed(string folder,CancellationToken cancellationToken=default)
    {
        if(!Directory.Exists(folder))throw new DirectoryNotFoundException(folder);
        var root=System.IO.Path.GetFullPath(folder);var games=new List<Game>();var warnings=new List<string>();
        var referenced=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var pending=new Stack<string>();pending.Push(root);
        while(pending.TryPop(out var directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                foreach(var child in Directory.EnumerateDirectories(directory))
                    if((File.GetAttributes(child)&FileAttributes.ReparsePoint)==0)pending.Push(child);
                foreach(var file in Directory.EnumerateFiles(directory))
                {
                    cancellationToken.ThrowIfCancellationRequested();var extension=System.IO.Path.GetExtension(file);
                    // CHDs beside Arcade ZIP sets are dependencies, not separate library games.
                    if(extension.Equals(".chd",StringComparison.OrdinalIgnoreCase)&&DetectFolderSystem(file,root) is{} archiveFolder&&SystemCatalog.Find(archiveFolder)?.ArchiveSet==true)continue;
                    string? system;
                    if(extension.Equals(".zip",StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var folderSystem=DetectFolderSystem(file,root);
                            if(folderSystem!=null&&SystemCatalog.Find(folderSystem)?.ArchiveSet==true){games.Add(new(System.IO.Path.GetFileNameWithoutExtension(file),folderSystem,System.IO.Path.GetFullPath(file)));continue;}
                            using var zip=ZipFile.OpenRead(file);var roms=zip.Entries.Select(e=>SystemCatalog.FromExtension(System.IO.Path.GetExtension(e.FullName))).Where(s=>s!=null).ToArray();
                            if(roms.Length!=1){warnings.Add(file+": ZIP muss genau eine unterstützte Cartridge-ROM enthalten.");continue;}system=roms[0];
                        }
                        catch(Exception ex) when(ex is IOException or InvalidDataException or UnauthorizedAccessException){warnings.Add(file+": "+ex.Message);continue;}
                    }
                    else if(!Extensions.TryGetValue(extension,out system))
                    {
                        var folderSystem=DetectFolderSystem(file,root);var definition=folderSystem==null?null:SystemCatalog.Find(folderSystem);
                        if(definition!=null&&definition.Extensions.Split(' ').Contains(extension.TrimStart('.'),StringComparer.OrdinalIgnoreCase))system=folderSystem;
                        else system=SystemCatalog.FromExtension(extension);
                        if(system==null)continue;
                    }
                    if(system==null)continue;
                    if(extension.Equals(".md",StringComparison.OrdinalIgnoreCase)&&!HasSegaHeader(file))continue;
                    if(system=="Unknown disc")system=DetectDiscSystem(file,root);
                    else if(DetectFolderSystem(file,root) is{} folderMatch&&SystemCatalog.Find(folderMatch) is{} folderDefinition&&folderDefinition.Extensions.Split(' ').Contains(extension.TrimStart('.'),StringComparer.OrdinalIgnoreCase))system=folderMatch;
                    games.Add(new(System.IO.Path.GetFileNameWithoutExtension(file),system,System.IO.Path.GetFullPath(file)));
                    if(extension.Equals(".m3u",StringComparison.OrdinalIgnoreCase)||extension.Equals(".cue",StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            foreach(var line in File.ReadLines(file))
                            {
                                var name=line.Trim();
                                if(extension.Equals(".cue",StringComparison.OrdinalIgnoreCase))
                                {
                                    var match=Regex.Match(name,"^FILE\\s+(?:\"([^\"]+)\"|(\\S+))",RegexOptions.IgnoreCase);
                                    if(!match.Success)continue;name=match.Groups[1].Success?match.Groups[1].Value:match.Groups[2].Value;
                                }
                                if(name.Length==0||name.StartsWith('#'))continue;
                                var target=System.IO.Path.GetFullPath(System.IO.Path.Combine(directory,name));if(File.Exists(target))referenced.Add(target);
                            }
                        }
                        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException){warnings.Add($"{file}: {ex.Message}");}
                    }
                }
            }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){warnings.Add($"{directory}: {ex.Message}");}
        }
        return new(games.Where(g=>!referenced.Contains(g.Path)).DistinctBy(g=>g.Path,StringComparer.OrdinalIgnoreCase).OrderBy(g=>g.System).ThenBy(g=>g.Title).ToArray(),warnings);
    }
    private static string DetectDiscSystem(string file,string root)
    {
        if(System.IO.Path.GetExtension(file).Equals(".iso",StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var stream=File.OpenRead(file);Span<byte> header=stackalloc byte[32];
                if(stream.Read(header)==32)
                {
                    if(header[0x18]==0x5D&&header[0x19]==0x1C&&header[0x1A]==0x9E&&header[0x1B]==0xA3)return "Wii";
                    if(header[0x1C]==0xC2&&header[0x1D]==0x33&&header[0x1E]==0x9F&&header[0x1F]==0x3D)return "GameCube";
                }
            }
            catch(IOException){ }catch(UnauthorizedAccessException){ }
        }
        return DetectFolderSystem(file,root)??"Unknown disc";
    }
    private static string? DetectFolderSystem(string file,string root)
    {
        for(var directory=Directory.GetParent(file);directory!=null;directory=directory.Parent)
        {if(Folders.TryGetValue(directory.Name,out var system))return system;if(SystemCatalog.FromFolder(directory.Name) is{} retro)return retro;if(directory.FullName.Equals(root,StringComparison.OrdinalIgnoreCase))break;}
        return null;
    }
    private static bool HasSegaHeader(string file)
    {
        try{using var stream=File.OpenRead(file);if(stream.Length<0x104)return false;stream.Position=0x100;Span<byte> bytes=stackalloc byte[4];return stream.Read(bytes)==4&&bytes.SequenceEqual("SEGA"u8);}
        catch(IOException){return false;}catch(UnauthorizedAccessException){return false;}
    }
}
public sealed class LibraryStore
{
    private readonly string connection;
    public LibraryStore(string file)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(file))!);
        connection=new SqliteConnectionStringBuilder{DataSource=file}.ToString();using var c=Open();using var cmd=c.CreateCommand();
        cmd.CommandText="CREATE TABLE IF NOT EXISTS games(path TEXT PRIMARY KEY COLLATE NOCASE,title TEXT NOT NULL,system TEXT NOT NULL);";cmd.ExecuteNonQuery();
        cmd.CommandText="PRAGMA table_info(games)";var columns=new HashSet<string>();using(var reader=cmd.ExecuteReader())while(reader.Read())columns.Add(reader.GetString(1));
        foreach(var(name,type)in new[]{("favorite","INTEGER NOT NULL DEFAULT 0"),("lastPlayed","TEXT"),("playCount","INTEGER NOT NULL DEFAULT 0"),("customized","INTEGER NOT NULL DEFAULT 0")})
            if(!columns.Contains(name)){cmd.CommandText=$"ALTER TABLE games ADD COLUMN {name} {type}";cmd.ExecuteNonQuery();}
    }
    private SqliteConnection Open(){var c=new SqliteConnection(connection);c.Open();return c;}
    public void Upsert(IEnumerable<Game> games)
    {
        using var c=Open();using var tx=c.BeginTransaction();foreach(var game in games)
        {
            using var cmd=c.CreateCommand();cmd.Transaction=tx;
            cmd.CommandText="INSERT INTO games(path,title,system) SELECT $p,$t,$s WHERE NOT EXISTS(SELECT 1 FROM games WHERE path=$p COLLATE NOCASE);";
            cmd.Parameters.AddWithValue("$p",game.Path);cmd.Parameters.AddWithValue("$t",game.Title);cmd.Parameters.AddWithValue("$s",game.System);cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }
    public void Update(Game game)=>Execute("UPDATE games SET title=$t,system=$s,favorite=$f,customized=1 WHERE path=$p COLLATE NOCASE",game);
    public void Remove(Game game)=>Execute("DELETE FROM games WHERE path=$p COLLATE NOCASE",game);
    public void MarkPlayed(Game game)
    {
        using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="UPDATE games SET lastPlayed=$d,playCount=playCount+1 WHERE path=$p COLLATE NOCASE";
        cmd.Parameters.AddWithValue("$d",DateTimeOffset.UtcNow.ToString("O"));cmd.Parameters.AddWithValue("$p",game.Path);cmd.ExecuteNonQuery();
    }
    private void Execute(string sql,Game game)
    {
        using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText=sql;cmd.Parameters.AddWithValue("$p",game.Path);cmd.Parameters.AddWithValue("$t",game.Title);cmd.Parameters.AddWithValue("$s",game.System);cmd.Parameters.AddWithValue("$f",game.Favorite?1:0);cmd.ExecuteNonQuery();
    }
    public IReadOnlyList<Game> GetAll()
    {
        var result=new List<Game>();using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT title,system,path,favorite,lastPlayed,playCount FROM games ORDER BY system,title";
        using var r=cmd.ExecuteReader();while(r.Read())result.Add(new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetInt32(3)!=0,r.IsDBNull(4)?null:DateTimeOffset.Parse(r.GetString(4)),r.GetInt32(5)));return result;
    }
    public void Restore(IEnumerable<Game> games)
    {
        using var c=Open();using var tx=c.BeginTransaction();foreach(var game in games)
        {
            if(string.IsNullOrWhiteSpace(game.Title)||string.IsNullOrWhiteSpace(game.Path)||!System.IO.Path.IsPathFullyQualified(game.Path))throw new InvalidDataException("Invalid library entry.");
            using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText="UPDATE games SET title=$t,system=$s,favorite=$f,lastPlayed=$d,playCount=$n,customized=1 WHERE path=$p COLLATE NOCASE; INSERT INTO games(path,title,system,favorite,lastPlayed,playCount,customized) SELECT $p,$t,$s,$f,$d,$n,1 WHERE NOT EXISTS(SELECT 1 FROM games WHERE path=$p COLLATE NOCASE)";
            cmd.Parameters.AddWithValue("$p",game.Path);cmd.Parameters.AddWithValue("$t",game.Title);cmd.Parameters.AddWithValue("$s",game.System);cmd.Parameters.AddWithValue("$f",game.Favorite?1:0);cmd.Parameters.AddWithValue("$d",(object?)game.LastPlayed?.ToString("O")??DBNull.Value);cmd.Parameters.AddWithValue("$n",Math.Max(0,game.PlayCount));cmd.ExecuteNonQuery();
        }tx.Commit();
    }
}
public sealed class ExternalEmulatorAdapter(EmulatorDefinition definition)
{
    public ProcessStartInfo CreateStartInfo(Game? game,string executablePath,string? corePath=null,bool fullscreen=true)
    {
        if(!File.Exists(executablePath))throw new FileNotFoundException("Emulator-Datei nicht gefunden.",executablePath);
        if(!System.IO.Path.GetExtension(executablePath).Equals(".exe",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Bitte eine Emulator-EXE wählen.");
        var psi=new ProcessStartInfo(executablePath){UseShellExecute=false,WorkingDirectory=System.IO.Path.GetDirectoryName(executablePath)!};if(game==null)return psi;
        if(!File.Exists(game.Path))throw new FileNotFoundException("ROM-Datei nicht gefunden. Ordner prüfen oder Spiel aus der Bibliothek entfernen.",game.Path);
        if(!definition.Systems.Contains(game.System))throw new InvalidOperationException("Diese Konsole wird vom Emulator nicht unterstützt.");
        if(definition.Id=="retroarch")
        {
            if(string.IsNullOrWhiteSpace(corePath)||!File.Exists(corePath))throw new InvalidOperationException($"Für {game.System} zuerst einen RetroArch-Core einrichten.");
            psi.ArgumentList.Add("-L");psi.ArgumentList.Add(corePath);if(fullscreen)psi.ArgumentList.Add("--fullscreen");
        }
        if(definition.Id=="duckstation"){psi.ArgumentList.Add("-batch");if(fullscreen)psi.ArgumentList.Add("-fullscreen");}
        if(definition.Id=="pcsx2"){psi.ArgumentList.Add("-batch");if(fullscreen)psi.ArgumentList.Add("-fullscreen");}
        if(definition.Id=="dolphin"){psi.ArgumentList.Add("-b");psi.ArgumentList.Add("-e");}
        psi.ArgumentList.Add(game.Path);return psi;
    }
    public Process Launch(Game? game,string executablePath,string? corePath=null,bool fullscreen=true)=>Process.Start(CreateStartInfo(game,executablePath,corePath,fullscreen))??throw new InvalidOperationException("Emulator konnte nicht gestartet werden.");
}
