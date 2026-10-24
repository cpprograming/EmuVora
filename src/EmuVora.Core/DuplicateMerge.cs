using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text.Json;

namespace EmuVora.Core;

public record MergeRow(Game Game, string? AddedAt, int Customized);
public record DuplicateMergePlan(ContentMatch Match, Game Keeper, string? CoverSource, string? AspectSource, MergeRow Combined, IReadOnlyList<MergeRow> OriginalRows, string OldSettings, string NewSettings);
public record DuplicateMergeResult(int Removed, string BackupDirectory, string OperationId);

public static class DuplicateMerge
{
    private const string JournalName = "duplicate-merge-pending.json";
    private record Journal(string Id, DuplicateMergePlan Plan);
    private static string Key(string path) => Path.GetFullPath(path);
    private static bool Same(string a, string b) => Key(a).Equals(Key(b), StringComparison.OrdinalIgnoreCase);
    private static SqliteConnection Open(string data)
    {
        var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(data, "library.db"), Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString()); db.Open(); return db;
    }
    private static FileStream Lock(string data) => new(Path.Combine(data, ".game-launch.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
    private static void ValidateMatch(ContentMatch match)
    {
        if (match.Games.Count < 2 || match.Games.Count > 100 || match.Length <= 0 || match.Sha256.Length != 64 || !match.Sha256.All(Uri.IsHexDigit)) throw new IOException("Ungültige Inhaltsprüfung; 2 bis 100 Dateien auswählen.");
        if (match.Games.Any(g => !Path.IsPathFullyQualified(g.Path) || LibraryContent.IsReferenceFile(g.Path)) || match.Games.Select(g => Key(g.Path)).Distinct(StringComparer.OrdinalIgnoreCase).Count() != match.Games.Count || match.Games.Select(g => g.System).Distinct().Count() != 1) throw new IOException("Nur unterschiedliche Einzeldateien desselben Systems zusammenführen.");
    }
    private static MergeRow? Read(SqliteConnection db, SqliteTransaction? tx, string path)
    {
        using var cmd = db.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = "SELECT title,system,path,favorite,lastPlayed,playCount,addedAt,customized FROM games WHERE path=$p COLLATE NOCASE"; cmd.Parameters.AddWithValue("$p", path);
        using var r = cmd.ExecuteReader(); return r.Read() ? new(new(r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt32(3) != 0, r.IsDBNull(4) ? null : DateTimeOffset.Parse(r.GetString(4)), r.GetInt32(5)), r.IsDBNull(6) ? null : r.GetString(6), r.GetInt32(7)) : null;
    }
    private static KeyValuePair<string, string>? Setting(Dictionary<string, string> values, string path)
    {
        var entries = values.Where(p => Path.IsPathFullyQualified(p.Key) && Same(p.Key, path)).ToArray();
        if (entries.Length > 1) throw new IOException("Mehrdeutige Einstellungen für denselben Dateipfad; zuerst bereinigen.");
        return entries.Length == 0 ? null : entries[0];
    }
    public static DuplicateMergePlan Preview(string data, ContentMatch match, string keeperPath, string? coverSource = null, string? aspectSource = null)
    {
        ValidateMatch(match); var keeper = match.Games.SingleOrDefault(g => Same(g.Path, keeperPath)) ?? throw new IOException("Der verbleibende Eintrag gehört nicht zur geprüften Gruppe.");
        var settingsFile = Path.Combine(data, "settings.json"); var old = File.ReadAllText(settingsFile); var settingsStore = new SettingsStore(settingsFile); var settings = settingsStore.Load();
        if (settingsStore.LoadWarning != null) throw new IOException(settingsStore.LoadWarning);
        if (File.ReadAllText(settingsFile) != old) throw new IOException("Einstellungen wurden während der Vorschau geändert."); using var db = Open(data);
        var rows = match.Games.Select(g => { var row = Read(db, null, g.Path); if (row?.Game != g) throw new IOException("Bibliothekseintrag wurde geändert; erneut prüfen."); return row; }).ToArray();
        var combined = Combine(rows, keeper);
        string? Choose(Dictionary<string, string> values, string? requested, bool preferProtected)
        {
            if (requested != null)
            {
                if (!match.Games.Any(g => Same(g.Path, requested)) || Setting(values, requested) == null) throw new IOException("Die ausgewählte Einstellung gehört nicht zur Gruppe."); return requested;
            }
            if (Setting(values, keeper.Path) != null) return keeper.Path;
            return match.Games.Where(g => Setting(values, g.Path) != null).OrderByDescending(g => preferProtected && settings.ProtectedCovers.Any(p => Path.IsPathFullyQualified(p) && Same(p, g.Path))).Select(g => g.Path).FirstOrDefault();
        }
        coverSource = Choose(settings.Covers, coverSource, true); aspectSource = Choose(settings.GameAspectRatios, aspectSource, false);
        if (coverSource != null)
        {
            var selected = Setting(settings.Covers, coverSource)!.Value.Value;
            var existing = Setting(settings.Covers, keeper.Path); if (existing.HasValue) settings.Covers.Remove(existing.Value.Key);
            settings.Covers[keeper.Path] = selected;
            var protect = settings.ProtectedCovers.Any(p => Path.IsPathFullyQualified(p) && Same(p, coverSource));
            settings.ProtectedCovers.RemoveWhere(p => Path.IsPathFullyQualified(p) && Same(p, keeper.Path)); if (protect) settings.ProtectedCovers.Add(keeper.Path);
        }
        if (aspectSource != null)
        {
            var selected = Setting(settings.GameAspectRatios, aspectSource)!.Value.Value; var existing = Setting(settings.GameAspectRatios, keeper.Path);
            if (existing.HasValue) settings.GameAspectRatios.Remove(existing.Value.Key); settings.GameAspectRatios[keeper.Path] = selected;
        }
        var paths = match.Games.Select(g => Key(g.Path)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var members in settings.Collections.Values) if (members.Any(p => Path.IsPathFullyQualified(p) && paths.Contains(Key(p)))) members.Add(keeper.Path);
        return new(match, keeper, coverSource, aspectSource, combined, rows!, old, JsonSerializer.Serialize(settings, SettingsStore.JsonOptions));
    }
    private static MergeRow Combine(IEnumerable<MergeRow> entries, Game keeper)
    {
        var rows = entries.ToArray(); int count;
        try { count = rows.Aggregate(0, (sum, r) => checked(sum + Math.Max(0, r.Game.PlayCount))); } catch (OverflowException) { throw new IOException("Zu viele Spielstarts für eine Zusammenführung."); }
        var added = rows.Where(r => r.AddedAt != null).OrderBy(r => DateTimeOffset.Parse(r.AddedAt!)).FirstOrDefault()?.AddedAt;
        return new(keeper with { Favorite = rows.Any(r => r.Game.Favorite), LastPlayed = rows.Max(r => r.Game.LastPlayed), PlayCount = count }, added, 1);
    }
    public static async Task<DuplicateMergeResult> ApplyAsync(string data, DuplicateMergePlan plan, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested(); RomRelocation.Recover(data); Recover(data); Backups.NoLinks(data);
        using var operation = Lock(data); ValidateMatch(plan.Match);
        var fresh = Preview(data, plan.Match, plan.Keeper.Path, plan.CoverSource, plan.AspectSource);
        if (fresh.Keeper != plan.Keeper || fresh.Combined != plan.Combined || !fresh.OriginalRows.SequenceEqual(plan.OriginalRows) || fresh.OldSettings != plan.OldSettings || fresh.NewSettings != plan.NewSettings) throw new IOException("Die Vorschau ist veraltet; Bibliothek und Einstellungen erneut prüfen.");
        var handles = new List<FileStream>();
        try
        {
            foreach (var game in plan.Match.Games)
            {
                token.ThrowIfCancellationRequested(); Backups.NoLinks(game.Path);
                var stream = new FileStream(game.Path, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, FileOptions.Asynchronous | FileOptions.SequentialScan); handles.Add(stream);
                if (stream.Length != plan.Match.Length || !Convert.ToHexString(await SHA256.HashDataAsync(stream, token).ConfigureAwait(false)).Equals(plan.Match.Sha256, StringComparison.OrdinalIgnoreCase)) throw new IOException("Datei-Inhalte wurden verändert; keine Einträge zusammengeführt.");
            }
            token.ThrowIfCancellationRequested(); using var db = Open(data); Backups.NoLinks(Path.Combine(data, "library.db")); Backups.NoLinks(Path.Combine(data, "settings.json"));
            var id = Guid.NewGuid().ToString("N"); var backup = Path.Combine(data, "merge-backups", id); Backups.NoLinks(Path.GetDirectoryName(backup)!); Directory.CreateDirectory(backup);
            File.WriteAllText(Path.Combine(backup, "settings.json"), plan.OldSettings);
            using (var copy = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(backup, "library.db"), Pooling = false }.ToString())) { copy.Open(); db.BackupDatabase(copy); }
            var journalFile = Path.Combine(data, JournalName); var json = JsonSerializer.Serialize(new Journal(id, plan), SettingsStore.JsonOptions);
            if (System.Text.Encoding.UTF8.GetByteCount(json) > 40 * 1024 * 1024) throw new IOException("Zusammenführung ist zu groß.");
            try
            {
                using (var tx = db.BeginTransaction())
                {
                    foreach (var row in plan.OriginalRows) if (Read(db, tx, row.Game.Path) != row) throw new IOException("Bibliotheksmetadaten wurden geändert; erneut prüfen.");
                    if (File.ReadAllText(Path.Combine(data, "settings.json")) != plan.OldSettings) throw new IOException("Einstellungen wurden geändert; erneut prüfen.");
                    token.ThrowIfCancellationRequested(); AtomicFile.WriteText(journalFile, json);
                    using (var history = db.CreateCommand())
                    {
                        var old = JsonSerializer.Deserialize<AppSettings>(plan.OldSettings, SettingsStore.JsonOptions)!; var paths = plan.Match.Games.Select(g => Key(g.Path)).ToHashSet(StringComparer.OrdinalIgnoreCase);
                        bool Included(string path) => Path.IsPathFullyQualified(path) && paths.Contains(Key(path));
                        var archived = JsonSerializer.Serialize(new { Covers = old.Covers.Where(p => Included(p.Key)).ToDictionary(), ProtectedCovers = old.ProtectedCovers.Where(Included).ToArray(), GameAspectRatios = old.GameAspectRatios.Where(p => Included(p.Key)).ToDictionary(), Collections = old.Collections.Where(c => c.Value.Any(Included)).ToDictionary(c => c.Key, c => c.Value.Where(Included).ToArray()) }, SettingsStore.JsonOptions);
                        history.Transaction = tx; history.CommandText = "INSERT INTO merge_history VALUES($id,$at,$keeper,$rows,$backup,$settings)";
                        history.Parameters.AddWithValue("$id", id); history.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O")); history.Parameters.AddWithValue("$keeper", plan.Keeper.Path); history.Parameters.AddWithValue("$rows", JsonSerializer.Serialize(plan.OriginalRows, SettingsStore.JsonOptions)); history.Parameters.AddWithValue("$backup", backup); history.Parameters.AddWithValue("$settings", archived); history.ExecuteNonQuery();
                    }
                    foreach (var game in plan.Match.Games.Where(g => !Same(g.Path, plan.Keeper.Path)))
                    {
                        using var cmd = db.CreateCommand(); cmd.Transaction = tx;
                        cmd.CommandText = "UPDATE merged_paths SET target=$keep,targetWriteUtc=$keepStamp WHERE target=$old COLLATE NOCASE; INSERT INTO merged_paths(path,target,length,lastWriteUtc,targetWriteUtc) VALUES($canonical,$keep,$length,$stamp,$keepStamp); DELETE FROM games WHERE path=$old COLLATE NOCASE";
                        cmd.Parameters.AddWithValue("$old", game.Path); cmd.Parameters.AddWithValue("$canonical", Key(game.Path)); cmd.Parameters.AddWithValue("$keep", plan.Keeper.Path); cmd.Parameters.AddWithValue("$length", plan.Match.Length); cmd.Parameters.AddWithValue("$stamp", File.GetLastWriteTimeUtc(game.Path).ToString("O")); cmd.Parameters.AddWithValue("$keepStamp", File.GetLastWriteTimeUtc(plan.Keeper.Path).ToString("O")); cmd.ExecuteNonQuery();
                    }
                    using (var cmd = db.CreateCommand())
                    {
                        cmd.Transaction = tx; cmd.CommandText = "UPDATE games SET favorite=$f,lastPlayed=$last,playCount=$count,addedAt=$added,customized=1 WHERE path=$p COLLATE NOCASE";
                        cmd.Parameters.AddWithValue("$p", plan.Keeper.Path); cmd.Parameters.AddWithValue("$f", plan.Combined.Game.Favorite ? 1 : 0); cmd.Parameters.AddWithValue("$last", (object?)plan.Combined.Game.LastPlayed?.ToString("O") ?? DBNull.Value); cmd.Parameters.AddWithValue("$count", plan.Combined.Game.PlayCount); cmd.Parameters.AddWithValue("$added", (object?)plan.Combined.AddedAt ?? DBNull.Value); cmd.ExecuteNonQuery();
                    }
                    token.ThrowIfCancellationRequested(); AtomicFile.WriteText(Path.Combine(data, "settings.json"), plan.NewSettings); tx.Commit();
                }
                File.Delete(journalFile); return new(plan.Match.Games.Count - 1, backup, id);
            }
            catch { RecoverCore(data); throw; }
        }
        finally { foreach (var stream in handles) stream.Dispose(); }
    }
    public static void Recover(string data)
    {
        if (!File.Exists(Path.Combine(data, JournalName))) return;
        using var operation = Lock(data); RecoverCore(data);
    }
    private static void RecoverCore(string data)
    {
        var file = Path.Combine(data, JournalName); if (!File.Exists(file)) return; Backups.NoLinks(file);
        if (new FileInfo(file).Length > 40 * 1024 * 1024) throw new IOException("Ausstehende Zusammenführung ist beschädigt; Sicherung im Datenordner prüfen.");
        Journal journal;
        try
        {
            journal = JsonSerializer.Deserialize<Journal>(File.ReadAllText(file), SettingsStore.JsonOptions) ?? throw new JsonException();
            ValidateMatch(journal.Plan.Match);
            if (!Guid.TryParseExact(journal.Id, "N", out _) || journal.Plan.OriginalRows.Count != journal.Plan.Match.Games.Count || !journal.Plan.OriginalRows.Select(r => r.Game).SequenceEqual(journal.Plan.Match.Games) || !journal.Plan.Match.Games.Contains(journal.Plan.Keeper) || Combine(journal.Plan.OriginalRows, journal.Plan.Keeper) != journal.Plan.Combined || JsonSerializer.Deserialize<AppSettings>(journal.Plan.OldSettings) == null || JsonSerializer.Deserialize<AppSettings>(journal.Plan.NewSettings) == null) throw new JsonException();
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or NullReferenceException or InvalidOperationException or FormatException) { throw new IOException("Ausstehende Zusammenführung ist ungültig. Keine Einstellungen überschrieben; Sicherung prüfen.", ex); }
        using var db = Open(data); var plan = journal.Plan; using var marker = db.CreateCommand(); marker.CommandText = "SELECT count(*) FROM merge_history WHERE id=$id"; marker.Parameters.AddWithValue("$id", journal.Id); var committed = Convert.ToInt32(marker.ExecuteScalar()) == 1;
        foreach (var row in plan.OriginalRows)
        {
            var expected = committed ? (Same(row.Game.Path, plan.Keeper.Path) ? plan.Combined : null) : row;
            if (Read(db, null, row.Game.Path) != expected) throw new IOException("Ausstehende Zusammenführung passt nicht zur Bibliothek. Sicherung im Datenordner prüfen.");
            if (committed && expected == null)
            {
                using var alias = db.CreateCommand(); alias.CommandText = "SELECT target FROM merged_paths WHERE path=$p COLLATE NOCASE"; alias.Parameters.AddWithValue("$p", Key(row.Game.Path));
                if (alias.ExecuteScalar() is not string target || !Same(target, plan.Keeper.Path)) throw new IOException("Ausstehende Zusammenführung enthält widersprüchliche Dateiverweise.");
            }
        }
        var settingsFile = Path.Combine(data, "settings.json"); var expectedSettings = committed ? plan.NewSettings : plan.OldSettings;
        Backups.NoLinks(settingsFile);
        if (!File.Exists(settingsFile) || File.ReadAllText(settingsFile) != expectedSettings) AtomicFile.WriteText(settingsFile, expectedSettings);
        File.Delete(file);
    }
    internal static void RelocateAliases(SqliteConnection db, SqliteTransaction tx, string oldRoot, string newRoot, IReadOnlyDictionary<string, string> targets)
    {
        var aliases = new List<(string Path, string Target)>(); using (var read = db.CreateCommand()) { read.Transaction = tx; read.CommandText = "SELECT path,target FROM merged_paths"; using var r = read.ExecuteReader(); while (r.Read()) aliases.Add((r.GetString(0), r.GetString(1))); }
        foreach (var alias in aliases)
        {
            if (!targets.TryGetValue(Key(alias.Target), out var target)) continue;
            var path = Backups.Inside(alias.Path, oldRoot) ? Path.GetFullPath(Path.Combine(newRoot, Path.GetRelativePath(oldRoot, alias.Path))) : alias.Path;
            if (!Same(path, alias.Path) && Read(db, tx, path) != null) throw new IOException("Am Ziel eines zusammengeführten Pfades existiert bereits ein Bibliothekseintrag. Keine Zuordnung geändert.");
            using var cmd = db.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = "UPDATE merged_paths SET path=$new,target=$target WHERE path=$old COLLATE NOCASE"; cmd.Parameters.AddWithValue("$new", path); cmd.Parameters.AddWithValue("$target", target); cmd.Parameters.AddWithValue("$old", alias.Path); cmd.ExecuteNonQuery();
        }
    }
}
