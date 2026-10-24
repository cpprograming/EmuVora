using EmuVora.Core;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using Xunit;

namespace EmuVora.Tests;

public class DuplicateMergeTests
{
    internal static AppSettings Options() => new() { OnboardingCompleted = true, CheckAppUpdatesOnStartup = false, CheckUpdatesOnStartup = false, AutoDownloadCovers = false };
    internal static async Task<(string Data, LibraryStore Store, ContentMatch Match)> Pair(TestDirectory dir)
    {
        var data = Path.Combine(dir.Path, "data"); Directory.CreateDirectory(data); new SettingsStore(Path.Combine(data, "settings.json")).Save(Options());
        var store = new LibraryStore(Path.Combine(data, "library.db"));
        store.Upsert([new("First title", "NES", Path.GetFullPath(dir.File("old/a.nes", "same bytes"))), new("Other title", "NES", Path.GetFullPath(dir.File("old/b.nes", "same bytes")))]);
        return (data, store, Assert.Single((await LibraryContent.CompareAsync(store.GetAll())).Matches));
    }
    private static void Sql(string data, string sql, string? path = null)
    {
        using var db = new SqliteConnection($"Data Source={Path.Combine(data, "library.db")}"); db.Open(); using var cmd = db.CreateCommand(); cmd.CommandText = sql; if (path != null) cmd.Parameters.AddWithValue("$p", path); cmd.ExecuteNonQuery();
    }
    [Fact] public async Task MergeUnionsMetadataKeepsChosenSettingsAndArchivesOriginals()
    {
        using var dir = new TestDirectory(); var (data, store, original) = await Pair(dir); var a = original.Games[0]; var b = original.Games[1];
        store.Restore([a with { PlayCount = 2, LastPlayed = DateTimeOffset.Parse("2025-01-01Z") }, b with { Favorite = true, PlayCount = 3, LastPlayed = DateTimeOffset.Parse("2026-01-01Z") }]);
        Sql(data, "UPDATE games SET addedAt='2024-01-01T00:00:00Z',customized=0 WHERE path=$p", a.Path); Sql(data, "UPDATE games SET addedAt='2023-01-01T00:00:00Z' WHERE path=$p", b.Path);
        var settings = Options(); var coverA = dir.File("data/covers/a.png", "cover a"); var coverB = dir.File("data/covers/b.png", "cover b"); settings.Covers[a.Path] = coverA; settings.Covers[b.Path] = coverB; settings.ProtectedCovers.Add(b.Path); settings.GameAspectRatios[a.Path] = "4:3"; settings.GameAspectRatios[b.Path] = "16:9"; settings.Collections["Couch"] = [b.Path]; settings.Collections["Other"] = [a.Path]; new SettingsStore(Path.Combine(data, "settings.json")).Save(settings);
        var match = Assert.Single((await LibraryContent.CompareAsync(store.GetAll())).Matches); var plan = DuplicateMerge.Preview(data, match, a.Path, b.Path, b.Path); var result = await DuplicateMerge.ApplyAsync(data, plan);
        var kept = Assert.Single(store.GetAll()); Assert.Equal(a.Title, kept.Title); Assert.Equal(a.Path, kept.Path); Assert.True(kept.Favorite); Assert.Equal(5, kept.PlayCount); Assert.Equal(DateTimeOffset.Parse("2026-01-01Z"), kept.LastPlayed); Assert.Equal(DateTimeOffset.Parse("2023-01-01Z"), store.GetAddedTimes()[a.Path]); Assert.Empty(store.RecognitionCandidates("NES"));
        var loaded = new SettingsStore(Path.Combine(data, "settings.json")).Load(); Assert.Equal(coverB, loaded.Covers[a.Path]); Assert.Contains(a.Path, loaded.ProtectedCovers); Assert.Equal("16:9", loaded.GameAspectRatios[a.Path]); Assert.All(loaded.Collections.Values, members => Assert.Contains(a.Path, members)); Assert.Equal("cover a", File.ReadAllText(coverA)); Assert.Equal("cover b", File.ReadAllText(coverB)); Assert.True(File.Exists(b.Path));
        Assert.Equal(2, new LibraryStore(Path.Combine(result.BackupDirectory, "library.db")).GetAll().Count); Assert.Equal(plan.OldSettings, File.ReadAllText(Path.Combine(result.BackupDirectory, "settings.json"))); Assert.Equal(1, result.Removed);
        using var db = new SqliteConnection($"Data Source={Path.Combine(data, "library.db")}"); db.Open(); using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT originalRows FROM merge_history"; Assert.Equal(2, JsonSerializer.Deserialize<MergeRow[]>((string)cmd.ExecuteScalar()!)!.Length);
    }
    [Theory] [InlineData("rom")] [InlineData("metadata")] [InlineData("settings")] [InlineData("missing")]
    public async Task ChangesSincePreviewAbortWithoutModifyingMetadata(string change)
    {
        using var dir = new TestDirectory(); var (data, store, match) = await Pair(dir); var plan = DuplicateMerge.Preview(data, match, match.Games[0].Path);
        if (change == "rom") File.WriteAllText(match.Games[1].Path, "other data");
        if (change == "missing") File.Delete(match.Games[1].Path);
        if (change == "metadata") store.Update(match.Games[1] with { Favorite = true });
        if (change == "settings") { var settings = Options(); settings.Theme = "Light"; new SettingsStore(Path.Combine(data, "settings.json")).Save(settings); }
        var before = store.GetAll().ToArray(); var settingsBefore = File.ReadAllText(Path.Combine(data, "settings.json"));
        await Assert.ThrowsAnyAsync<IOException>(() => DuplicateMerge.ApplyAsync(data, plan)); Assert.Equal(before, store.GetAll()); Assert.Equal(settingsBefore, File.ReadAllText(Path.Combine(data, "settings.json"))); Assert.False(File.Exists(Path.Combine(data, "duplicate-merge-pending.json")));
    }
    [Fact] public async Task RescanSuppressesUnchangedAliasesAndReimportsChangedFiles()
    {
        using var dir = new TestDirectory(); var (data, store, match) = await Pair(dir); await DuplicateMerge.ApplyAsync(data, DuplicateMerge.Preview(data, match, match.Games[0].Path));
        store.Upsert(RomScanner.Scan(Path.Combine(dir.Path, "old"))); Assert.Single(store.GetAll());
        File.AppendAllText(match.Games[1].Path, "changed"); store.Upsert(RomScanner.Scan(Path.Combine(dir.Path, "old"))); Assert.Equal(2, store.GetAll().Count);
    }
    [Theory] [InlineData(false)] [InlineData(true)] public async Task ChangedOrMissingKeeperDoesNotHideAnAvailableCopy(bool missing)
    {
        using var dir = new TestDirectory(); var (data, store, match) = await Pair(dir); await DuplicateMerge.ApplyAsync(data, DuplicateMerge.Preview(data, match, match.Games[0].Path));
        if (missing) File.Delete(match.Games[0].Path); else File.AppendAllText(match.Games[0].Path, "different game");
        store.Upsert(RomScanner.Scan(Path.Combine(dir.Path, "old"))); Assert.Equal(2, store.GetAll().Count);
    }
    [Fact] public async Task ChainedMergesKeepEarlierAliasesAndExplicitRestoreCanReintroduceOne()
    {
        using var dir = new TestDirectory(); var (data, store, match) = await Pair(dir); await DuplicateMerge.ApplyAsync(data, DuplicateMerge.Preview(data, match, match.Games[0].Path));
        var third = new Game("Third", "NES", Path.GetFullPath(dir.File("old/c.nes", "same bytes"))); store.Upsert([third]); var second = Assert.Single((await LibraryContent.CompareAsync(store.GetAll())).Matches); await DuplicateMerge.ApplyAsync(data, DuplicateMerge.Preview(data, second, third.Path));
        store.Upsert(RomScanner.Scan(Path.Combine(dir.Path, "old"))); Assert.Equal(third.Path, Assert.Single(store.GetAll()).Path);
        store.Restore([match.Games[1] with { Favorite = true }]); Assert.Equal(2, store.GetAll().Count); Assert.True(store.GetAll().Single(g => g.Path == match.Games[1].Path).Favorite);
    }
    [Fact] public async Task RemovingKeeperAllowsOriginalFilesToBeImportedAgain()
    {
        using var dir = new TestDirectory(); var (data, store, match) = await Pair(dir); await DuplicateMerge.ApplyAsync(data, DuplicateMerge.Preview(data, match, match.Games[0].Path)); store.Remove(Assert.Single(store.GetAll()));
        store.Upsert(RomScanner.Scan(Path.Combine(dir.Path, "old"))); Assert.Equal(2, store.GetAll().Count);
    }
    [Fact] public async Task FolderRelocationRetainsSuppressionOfMergedPaths()
    {
        using var dir = new TestDirectory(); var (data, store, match) = await Pair(dir); await DuplicateMerge.ApplyAsync(data, DuplicateMerge.Preview(data, match, match.Games[0].Path)); var old = Path.Combine(dir.Path, "old"); var target = Path.Combine(dir.Path, "new"); Directory.Move(old, target);
        RomRelocation.Apply(data, RomRelocation.Preview(store.GetAll(), old, target)); store.Upsert(RomScanner.Scan(target)); Assert.Single(store.GetAll());
    }
    [Fact] public async Task WriteFailureRollsBackDatabaseAndSettingsAndRecoversJournal()
    {
        using var dir = new TestDirectory(); var (data, store, match) = await Pair(dir); var plan = DuplicateMerge.Preview(data, match, match.Games[0].Path); var settings = Path.Combine(data, "settings.json"); File.Delete(settings + ".bak"); Directory.CreateDirectory(settings + ".bak");
        await Assert.ThrowsAnyAsync<IOException>(() => DuplicateMerge.ApplyAsync(data, plan)); Assert.Equal(2, store.GetAll().Count); Assert.Equal(plan.OldSettings, File.ReadAllText(settings)); Assert.False(File.Exists(Path.Combine(data, "duplicate-merge-pending.json")));
    }
    [Fact] public async Task RelocationAliasCollisionRollsBackAllPathChanges()
    {
        using var dir = new TestDirectory(); var (data, store, match) = await Pair(dir); await DuplicateMerge.ApplyAsync(data, DuplicateMerge.Preview(data, match, match.Games[0].Path)); var old = Path.Combine(dir.Path, "old"); var target = Path.Combine(dir.Path, "new"); Directory.Move(old, target);
        store.Upsert([new("Already imported", "NES", Path.Combine(target, "b.nes"))]); var before = store.GetAll().ToArray(); var plan = RomRelocation.Preview(before, old, target); Assert.Single(plan.Suggestions);
        Assert.Throws<IOException>(() => RomRelocation.Apply(data, plan)); Assert.Equal(before, store.GetAll());
    }
    [Theory] [InlineData(false)] [InlineData(true)] public async Task InterruptedMergeRecoversOldOrCommittedSettings(bool committed)
    {
        using var dir = new TestDirectory(); var (data, store, match) = await Pair(dir); var settings = Options(); settings.Collections["Couch"] = [match.Games[1].Path]; new SettingsStore(Path.Combine(data, "settings.json")).Save(settings); var plan = DuplicateMerge.Preview(data, match, match.Games[0].Path); var id = Guid.NewGuid().ToString("N");
        if (committed) id = (await DuplicateMerge.ApplyAsync(data, plan)).OperationId;
        File.WriteAllText(Path.Combine(data, "settings.json"), committed ? plan.OldSettings : plan.NewSettings); File.WriteAllText(Path.Combine(data, "duplicate-merge-pending.json"), JsonSerializer.Serialize(new { Id = id, Plan = plan }, SettingsStore.JsonOptions));
        _ = new SettingsStore(Path.Combine(data, "settings.json")).Load(); Assert.Equal(committed ? plan.NewSettings : plan.OldSettings, File.ReadAllText(Path.Combine(data, "settings.json"))); Assert.Equal(committed ? 1 : 2, store.GetAll().Count); Assert.False(File.Exists(Path.Combine(data, "duplicate-merge-pending.json")));
    }
    [Fact] public async Task InconsistentRecoveryBlocksLoadingWithoutOverwritingSettings()
    {
        using var dir = new TestDirectory(); var (data, store, match) = await Pair(dir); var plan = DuplicateMerge.Preview(data, match, match.Games[0].Path); File.WriteAllText(Path.Combine(data, "duplicate-merge-pending.json"), JsonSerializer.Serialize(new { Id = Guid.NewGuid().ToString("N"), Plan = plan }, SettingsStore.JsonOptions)); store.Remove(match.Games[1]);
        Assert.Throws<IOException>(() => new SettingsStore(Path.Combine(data, "settings.json")).Load()); Assert.Equal(plan.OldSettings, File.ReadAllText(Path.Combine(data, "settings.json")));
    }
    [Fact] public async Task CancellationAndActiveLaunchDoNotChangeRows()
    {
        using var dir = new TestDirectory(); var (data, store, match) = await Pair(dir); var plan = DuplicateMerge.Preview(data, match, match.Games[0].Path); using var token = new CancellationTokenSource(); token.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DuplicateMerge.ApplyAsync(data, plan, token.Token));
        using (var handle = new FileStream(Path.Combine(data, ".game-launch.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) await Assert.ThrowsAnyAsync<IOException>(() => DuplicateMerge.ApplyAsync(data, plan)); Assert.Equal(2, store.GetAll().Count);
    }
    [Fact] public async Task ReferenceFilesMixedSystemsAndCountOverflowAreRejected()
    {
        using var dir = new TestDirectory(); var (data, store, match) = await Pair(dir);
        Assert.Throws<IOException>(() => DuplicateMerge.Preview(data, match with { Games = [match.Games[0], match.Games[1] with { System = "SNES" }] }, match.Games[0].Path));
        Assert.Throws<IOException>(() => DuplicateMerge.Preview(data, match with { Games = [match.Games[0], match.Games[1] with { Path = Path.Combine(dir.Path, "disc.cue") }] }, match.Games[0].Path));
        store.Restore(match.Games.Select(g => g with { PlayCount = int.MaxValue })); var current = Assert.Single((await LibraryContent.CompareAsync(store.GetAll())).Matches); Assert.Throws<IOException>(() => DuplicateMerge.Preview(data, current, current.Games[0].Path));
    }
    [Fact] public async Task FullBackupRetainsAliasesAndOriginalMetadataArchive()
    {
        using var dir = new TestDirectory(); var (data, store, match) = await Pair(dir); var settings = Options(); settings.GameAspectRatios[match.Games[1].Path] = "16:9"; new SettingsStore(Path.Combine(data, "settings.json")).Save(settings);
        await DuplicateMerge.ApplyAsync(data, DuplicateMerge.Preview(data, match, match.Games[0].Path)); var zip = Path.Combine(dir.Path, "backup.zip"); await Backups.CreateAsync(data, zip); var restored = await Backups.RestoreAsync(zip, dir.Path); var restoredStore = new LibraryStore(Path.Combine(restored, "library.db")); restoredStore.Upsert(RomScanner.Scan(Path.Combine(dir.Path, "old"))); Assert.Single(restoredStore.GetAll());
        using var db = new SqliteConnection($"Data Source={Path.Combine(restored, "library.db")}"); db.Open(); using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT originalSettings FROM merge_history"; var archived = JsonDocument.Parse((string)cmd.ExecuteScalar()!); Assert.Equal("16:9", archived.RootElement.GetProperty("GameAspectRatios").GetProperty(match.Games[1].Path).GetString());
    }
}
