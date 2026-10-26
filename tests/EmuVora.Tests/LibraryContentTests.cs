using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;

public class LibraryContentTests
{
    private static Game GameFile(TestDirectory dir, string file, string content, string title = "Game", string system = "PS1") => new(title, system, Path.GetFullPath(dir.File(file, content)));
    private static LibraryAuditGroup Discs(params Game[] games) => LibraryAudit.Inspect(games).Single(g => g.Kind == "Mehrdisc-Kandidat");
    private static LibraryAuditGroup Pair(TestDirectory dir) => Discs(GameFile(dir, "PS1/Game (Disc 2).chd", "disc two", "Game (Disc 2)"), GameFile(dir, "PS1/Game (Disc 1).chd", "disc one", "Game (Disc 1)"));

    [Fact] public async Task HashesSeparateSameSizeDifferentBytesAndPreserveAllFiles()
    {
        using var dir = new TestDirectory();
        var games = new[] { GameFile(dir, "a.chd", "abc"), GameFile(dir, "b.chd", "abc"), GameFile(dir, "c.chd", "xyz") };
        var result = await LibraryContent.CompareAsync(games.Concat([games[0]]));
        Assert.Equal(3, result.CheckedFiles); var match = Assert.Single(result.Matches); Assert.Equal(2, match.Games.Count); Assert.Equal(3, match.Length); Assert.Equal(64, match.Sha256.Length);
        Assert.Empty(result.Notes); Assert.All(games, game => Assert.True(File.Exists(game.Path)));
    }
    [Fact] public async Task IndexFilesMissingLockedAndEmptyFilesAreReportedWithoutFalseMatches()
    {
        using var dir = new TestDirectory(); var locked = GameFile(dir, "locked.chd", "locked");
        using var handle = File.Open(locked.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var result = await LibraryContent.CompareAsync([GameFile(dir, "a.cue", "FILE disc.bin BINARY"), GameFile(dir, "b.m3u", "disc.chd"), GameFile(dir, "empty.chd", ""), new("Missing", "PS1", Path.Combine(dir.Path, "missing.chd")), locked]);
        Assert.Equal(0, result.CheckedFiles); Assert.Empty(result.Matches); Assert.Equal(5, result.Notes.Count);
    }
    [Fact] public async Task HashCancellationPropagates()
    {
        using var dir = new TestDirectory(); using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => LibraryContent.CompareAsync([GameFile(dir, "a.chd", "data")], cancellation.Token));
    }
    [Fact] public void PlaylistIsRelativeOrderedAndNeverOverwrites()
    {
        using var dir = new TestDirectory(); var group = Pair(dir); var plan = LibraryContent.PreviewPlaylist(group);
        Assert.False(File.Exists(plan.Path)); Assert.Equal("Game (Disc 1).chd\nGame (Disc 2).chd\n", plan.Content);
        var game = LibraryContent.CreatePlaylist(plan); Assert.Equal("PS1", game.System); Assert.Equal("Game", game.Title); Assert.Equal(plan.Content, File.ReadAllText(game.Path));
        Assert.Throws<IOException>(() => LibraryContent.CreatePlaylist(plan)); Assert.Equal(plan.Content, File.ReadAllText(game.Path));
        Assert.All(group.Games, disc => Assert.True(File.Exists(disc.Path))); Assert.Single(RomScanner.Scan(dir.Path));
    }
    [Theory] [InlineData(2, 3)] [InlineData(1, 3)] [InlineData(1, 1)]
    public void MissingOrRepeatedDiscNumbersAreRejected(int first, int second)
    {
        using var dir = new TestDirectory(); var a = GameFile(dir, "a.chd", "a", $"Game (Disc {first})"); var b = GameFile(dir, "b.chd", "b", $"Game (Disc {second})");
        var group = new LibraryAuditGroup("Mehrdisc-Kandidat", "Game", "PS1", [a, b]); Assert.Throws<IOException>(() => LibraryContent.PreviewPlaylist(group));
    }
    [Theory] [InlineData("PS2", ".chd")] [InlineData("PS1", ".bin")] [InlineData("PS1", ".iso")]
    public void UnsupportedSystemsAndRawTracksAreRejected(string system, string extension)
    {
        using var dir = new TestDirectory(); var a = GameFile(dir, "a" + extension, "a", "Game (Disc 1)", system); var b = GameFile(dir, "b" + extension, "b", "Game (Disc 2)", system);
        Assert.Throws<IOException>(() => LibraryContent.PreviewPlaylist(Discs(a, b)));
    }
    [Fact] public void ChangedDiscAndCancellationLeaveNoPlaylistOrTemporaryFile()
    {
        using var dir = new TestDirectory(); var group = Pair(dir); var plan = LibraryContent.PreviewPlaylist(group);
        File.AppendAllText(group.Games[0].Path, "changed"); Assert.Throws<IOException>(() => LibraryContent.CreatePlaylist(plan)); Assert.False(File.Exists(plan.Path));
        plan = LibraryContent.PreviewPlaylist(group); using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => LibraryContent.CreatePlaylist(plan, cancellation.Token)); Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(plan.Path)!, "*.tmp"));
    }
    [Fact] public void CueDependenciesAreCheckedAndChangedTracksRejected()
    {
        using var dir = new TestDirectory(); var a = GameFile(dir, "PS1/Game (Disc 1).cue", "FILE \"one.bin\" BINARY", "Game (Disc 1)"); var b = GameFile(dir, "PS1/Game (Disc 2).cue", "FILE \"two.bin\" BINARY", "Game (Disc 2)");
        var group = Discs(a, b); Assert.ThrowsAny<IOException>(() => LibraryContent.PreviewPlaylist(group));
        dir.File("PS1/one.bin", "track one"); var track = dir.File("PS1/two.bin", "track two"); var plan = LibraryContent.PreviewPlaylist(group); Assert.Equal(4, plan.Files.Count);
        File.AppendAllText(track, "changed"); Assert.Throws<IOException>(() => LibraryContent.CreatePlaylist(plan));
        plan = LibraryContent.PreviewPlaylist(group); Assert.True(File.Exists(LibraryContent.CreatePlaylist(plan).Path));
    }
    [Theory] [InlineData("FILE ../outside.bin BINARY")] [InlineData("REM no files")] [InlineData("FILE C:\\outside.bin BINARY")]
    public void InvalidCueReferencesAreRejected(string cue)
    {
        using var dir = new TestDirectory(); dir.File("outside.bin", "outside");
        var a = GameFile(dir, "PS1/a.cue", cue, "Game (Disc 1)"); var b = GameFile(dir, "PS1/b.chd", "b", "Game (Disc 2)");
        Assert.Throws<IOException>(() => LibraryContent.PreviewPlaylist(Discs(a, b)));
    }
    [Fact] public void IncompleteTotalDiscCountIsRejected()
    {
        using var dir = new TestDirectory(); var a = GameFile(dir, "a.chd", "a", "Game (Disc 1 of 3)"); var b = GameFile(dir, "b.chd", "b", "Game (Disc 2 of 3)");
        Assert.Throws<IOException>(() => LibraryContent.PreviewPlaylist(Discs(a, b)));
    }
    [Theory] [InlineData("Saturn", ".chd")] [InlineData("Saturn", ".cue")] [InlineData("Dreamcast", ".chd")] [InlineData("Dreamcast", ".cue")] [InlineData("Mega-CD", ".chd")] [InlineData("Mega-CD", ".cue")]
    public void AdditionalSystemsKeepTheirPlatformAndValidateCueTracks(string system, string extension)
    {
        using var dir = new TestDirectory(); var a = GameFile(dir, system + "/Game (Disc 1)" + extension, extension == ".cue" ? "FILE one.bin BINARY" : "disc one", "Game (Disc 1)", system); var b = GameFile(dir, system + "/Game (Disc 2)" + extension, extension == ".cue" ? "FILE two.bin BINARY" : "disc two", "Game (Disc 2)", system); var group = Discs(a, b);
        if (extension == ".cue") { Assert.ThrowsAny<IOException>(() => LibraryContent.PreviewPlaylist(group)); dir.File(system + "/one.bin", "track one"); dir.File(system + "/two.bin", "track two"); }
        var plan = LibraryContent.PreviewPlaylist(group); var game = LibraryContent.CreatePlaylist(plan); Assert.Equal(system, game.System); Assert.Equal(system, Assert.Single(RomScanner.Scan(Path.Combine(dir.Path, system))).System); Assert.True(File.Exists(a.Path)); Assert.True(File.Exists(b.Path)); Assert.Contains("Disc 1", File.ReadAllLines(game.Path)[0]);
    }
    [Theory] [InlineData("PS1", ".gdi")] [InlineData("Dreamcast", ".cdi")] [InlineData("Saturn", ".bin")] [InlineData("PC-FX", ".chd")] [InlineData("PC Engine CD", ".cue")]
    public void OtherFormatsAndUnverifiedSystemsRemainRejected(string system, string extension)
    {
        using var dir = new TestDirectory(); var a = GameFile(dir, "a" + extension, "one", "Game (Disc 1)", system); var b = GameFile(dir, "b" + extension, "two", "Game (Disc 2)", system); Assert.Throws<IOException>(() => LibraryContent.PreviewPlaylist(Discs(a, b)));
    }
}
