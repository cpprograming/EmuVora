using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace EmuVora.Core;

public record Game(string Title, string System, string Path);
public record EmulatorDefinition(string Id, string Name, string[] Systems, string Homepage, string ExecutableHint, string ArgumentsTemplate);

public static class EmulatorCatalog
{
    // Metadata only: downloads must be approved and verified against an official release manifest.
    public static readonly EmulatorDefinition[] Known =
    [
        new("retroarch", "RetroArch", ["NES", "SNES", "GB", "GBC", "GBA", "N64"], "https://www.retroarch.com/", "retroarch.exe", "-L \"{core}\" \"{rom}\""),
        new("duckstation", "DuckStation", ["PS1"], "https://github.com/stenzek/duckstation", "duckstation-qt-x64-ReleaseLTCG.exe", "-batch -fullscreen \"{rom}\""),
        new("pcsx2", "PCSX2", ["PS2"], "https://pcsx2.net/", "pcsx2-qt.exe", "-fullscreen \"{rom}\""),
        new("dolphin", "Dolphin", ["GameCube", "Wii"], "https://dolphin-emu.org/", "Dolphin.exe", "-b -e \"{rom}\""),
        new("ppsspp", "PPSSPP", ["PSP"], "https://www.ppsspp.org/", "PPSSPPWindows64.exe", "\"{rom}\"")
    ];
}

public static class RomScanner
{
    private static readonly Dictionary<string,string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".nes"]="NES", [".sfc"]="SNES", [".smc"]="SNES", [".gb"]="GB", [".gbc"]="GBC", [".gba"]="GBA",
        [".n64"]="N64", [".z64"]="N64", [".v64"]="N64", [".gcm"]="GameCube", [".rvz"]="GameCube",
        [".wbfs"]="Wii", [".cso"]="PSP", [".chd"]="Disc", [".cue"]="Disc", [".iso"]="Disc"
    };
    public static IReadOnlyList<Game> Scan(string folder)
    {
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException(folder);
        var games = new List<Game>();
        foreach (var path in Directory.EnumerateFiles(folder,"*",SearchOption.AllDirectories))
        {
            if (!Extensions.TryGetValue(System.IO.Path.GetExtension(path), out var system)) continue;
            // Disc images are ambiguous; parent folder can disambiguate, otherwise user must select system.
            if (system == "Disc")
            {
                var parent = Directory.GetParent(path)?.Name.ToUpperInvariant() ?? "";
                system = parent switch { "PS1" or "PSX" => "PS1", "PS2" => "PS2", "PSP" => "PSP", "WII" => "Wii", "GAMECUBE" or "GC" => "GameCube", _ => "Unknown disc" };
            }
            games.Add(new Game(System.IO.Path.GetFileNameWithoutExtension(path),system,path));
        }
        return games.OrderBy(g=>g.System).ThenBy(g=>g.Title).ToArray();
    }
}

public sealed class LibraryStore
{
    private readonly string _connection;
    public LibraryStore(string file)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        _connection = new SqliteConnectionStringBuilder { DataSource=file }.ToString();
        using var c = new SqliteConnection(_connection); c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS games(path TEXT PRIMARY KEY, title TEXT NOT NULL, system TEXT NOT NULL);";
        cmd.ExecuteNonQuery();
    }
    public void Upsert(IEnumerable<Game> games)
    {
        using var c = new SqliteConnection(_connection); c.Open();
        using var tx = c.BeginTransaction();
        foreach(var game in games)
        {
            using var cmd = c.CreateCommand(); cmd.Transaction = tx;
            cmd.CommandText="INSERT INTO games(path,title,system) VALUES($p,$t,$s) ON CONFLICT(path) DO UPDATE SET title=$t,system=$s";
            cmd.Parameters.AddWithValue("$p",game.Path); cmd.Parameters.AddWithValue("$t",game.Title); cmd.Parameters.AddWithValue("$s",game.System); cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }
    public IReadOnlyList<Game> GetAll()
    {
        var result = new List<Game>(); using var c = new SqliteConnection(_connection); c.Open();
        using var cmd=c.CreateCommand(); cmd.CommandText="SELECT title,system,path FROM games ORDER BY system,title";
        using var r=cmd.ExecuteReader(); while(r.Read()) result.Add(new Game(r.GetString(0),r.GetString(1),r.GetString(2)));
        return result;
    }
}

public interface IEmulatorAdapter
{
    EmulatorDefinition Definition { get; }
    bool IsInstalled(string executablePath);
    Task LaunchAsync(Game game, string executablePath, string? corePath = null);
}

public sealed class ExternalEmulatorAdapter(EmulatorDefinition definition) : IEmulatorAdapter
{
    public EmulatorDefinition Definition => definition;
    public bool IsInstalled(string executablePath) => File.Exists(executablePath);
    public Task LaunchAsync(Game game, string executablePath, string? corePath = null)
    {
        if (!IsInstalled(executablePath)) throw new FileNotFoundException("Emulator executable not found",executablePath);
        if (!File.Exists(game.Path)) throw new FileNotFoundException("ROM not found",game.Path);
        if (!definition.Systems.Contains(game.System)) throw new InvalidOperationException("Unsupported system for emulator");
        if (definition.Id == "retroarch" && (string.IsNullOrWhiteSpace(corePath) || !File.Exists(corePath)))
            throw new InvalidOperationException("Configure a valid RetroArch core path first.");
        var psi = new ProcessStartInfo(executablePath) { UseShellExecute=false, WorkingDirectory=Path.GetDirectoryName(executablePath)! };
        if (definition.Id == "retroarch") { psi.ArgumentList.Add("-L"); psi.ArgumentList.Add(corePath!); }
        if (definition.Id == "duckstation") { psi.ArgumentList.Add("-batch"); psi.ArgumentList.Add("-fullscreen"); }
        if (definition.Id == "pcsx2") psi.ArgumentList.Add("-fullscreen");
        if (definition.Id == "dolphin") { psi.ArgumentList.Add("-b"); psi.ArgumentList.Add("-e"); }
        psi.ArgumentList.Add(game.Path);
        Process.Start(psi);
        return Task.CompletedTask;
    }
}

public interface IEmulatorPackageProvider
{
    Task<IReadOnlyList<EmulatorRelease>> GetOfficialReleasesAsync(CancellationToken cancellationToken);
    Task<string> DownloadVerifiedAsync(EmulatorRelease release, string destination, CancellationToken cancellationToken);
}
public record EmulatorRelease(string Version, Uri OfficialDownloadUrl, string Sha256);
// Deliberately no generic arbitrary-URL installer. A future implementation must pin trusted hosts,
// validate SHA-256/signatures, obtain explicit consent, back up settings and support rollback.
