using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace EmuVora.Core;

public record DataDirectorySummary(string Directory,long Games);
public sealed class DataDirectorySwitch(string locatorFile)
{
    private const string HistoryName="data-location-history.json";
    public static DataDirectorySummary Inspect(string directory)
    {
        if(!Path.IsPathFullyQualified(directory))throw new IOException("Bitte einen vollständigen Datenpfad wählen.");
        var full=Path.GetFullPath(directory);
        if(ExternalSaves.Equal(full,Path.GetPathRoot(full)!))throw new IOException("Bitte einen Datenordner statt eines Laufwerks wählen.");
        DataLocation.CheckAvailable(full);
        var settings=Path.Combine(full,"settings.json");var database=Path.Combine(full,"library.db");
        if(!File.Exists(settings)||!File.Exists(database))throw new IOException("Dieser Ordner enthält keine vorhandene EmuVora-Bibliothek. Bitte ihren Datenordner mit settings.json und library.db wählen.");
        if(new FileInfo(settings).Length>16*1024*1024)throw new IOException("Die Einstellungsdatei ist zu groß.");
        if(JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(settings).TrimStart('\uFEFF'),SettingsStore.JsonOptions)==null)throw new IOException("Die Einstellungen sind ungültig.");
        using var connection=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=database,Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString());connection.Open();
        using var command=connection.CreateCommand();command.CommandText="PRAGMA quick_check";
        if(command.ExecuteScalar()?.ToString()!="ok")throw new IOException("Die gewählte Bibliotheksdatenbank ist beschädigt.");
        command.CommandText="PRAGMA table_info(games)";var columns=new HashSet<string>();using(var reader=command.ExecuteReader())while(reader.Read())columns.Add(reader.GetString(1));
        if(!new[]{"path","title","system"}.All(columns.Contains))throw new IOException("Die Datenbank enthält keine unterstützte EmuVora-Spielebibliothek.");
        command.CommandText="SELECT COUNT(*) FROM games";return new(full,Convert.ToInt64(command.ExecuteScalar()));
    }
    public static string? Previous(string current)
    {
        var file=Path.Combine(current,HistoryName);if(!File.Exists(file))return null;
        if(new FileInfo(file).Length>65536)throw new IOException("Der gespeicherte Rückweg ist ungültig.");
        var history=JsonSerializer.Deserialize<History>(File.ReadAllText(file),SettingsStore.JsonOptions);
        if(history==null||!Path.IsPathFullyQualified(history.PreviousDataDirectory))throw new IOException("Der gespeicherte Rückweg ist ungültig.");
        return Path.GetFullPath(history.PreviousDataDirectory);
    }
    public DataDirectorySummary Switch(string current,string target,Action<string> launch,CancellationToken token=default)
    {
        var summary=Inspect(target);current=Path.GetFullPath(current);
        if(ExternalSaves.Equal(current,summary.Directory))throw new IOException("Diese Bibliothek ist bereits geöffnet.");
        var historyFile=Path.Combine(summary.Directory,HistoryName);
        var files=new[]{Path.GetFullPath(locatorFile),Path.GetFullPath(locatorFile)+".bak",historyFile,historyFile+".bak"}.Distinct(StringComparer.OrdinalIgnoreCase);
        var snapshots=files.ToDictionary(file=>file,file=>File.Exists(file)?File.ReadAllBytes(file):null);
        token.ThrowIfCancellationRequested();
        try
        {
            DataLocation.Prepare(summary.Directory);
            AtomicFile.WriteText(historyFile,JsonSerializer.Serialize(new History(current),SettingsStore.JsonOptions));
            new DataLocation(locatorFile).Select(summary.Directory);
            // Once launch starts, the switch is committed; cancellation must not undo a live child.
            launch(summary.Directory);return summary;
        }
        catch(Exception error)
        {
            var failures=new List<Exception>{error};
            foreach(var (file,bytes) in snapshots)
                try{if(bytes==null){if(File.Exists(file))File.Delete(file);}else if(!File.Exists(file)||!File.ReadAllBytes(file).AsSpan().SequenceEqual(bytes))File.WriteAllBytes(file,bytes);}
                catch(Exception rollbackError){failures.Add(rollbackError);}
            if(failures.Count>1)throw new AggregateException("Wechsel fehlgeschlagen; der gespeicherte Datenpfad konnte nicht vollständig zurückgesetzt werden. Die Bibliotheksdateien wurden nicht verschoben.",failures);
            throw;
        }
    }
    private record History(string PreviousDataDirectory);
}
