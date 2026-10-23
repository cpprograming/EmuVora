using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;

public class LibraryAuditTests
{
    private static Game Entry(string title, string file, string system = "PS1") => new(title, system, Path.GetFullPath(file));

    [Fact]
    public void EqualNamesAreCandidatesButDifferentSystemsAndEditionsStaySeparate()
    {
        var groups = LibraryAudit.Inspect([Entry("Game", "a.chd"), Entry("game", "b.chd"), Entry("Game (USA)", "c.chd"), Entry("Game", "d.chd", "PS2")]);
        Assert.Single(groups);
        Assert.Equal(2, groups[0].Games.Count);
        Assert.Contains("ungeprüft", groups[0].Kind);
    }

    [Fact]
    public void DiscOrderIsNumericAndRegionsAndFoldersRemainSeparate()
    {
        var groups = LibraryAudit.Inspect([Entry("Game (USA) (Disc 2)", "a/2.chd"), Entry("Game (USA) (Disc 1)", "a/1.chd"), Entry("Game (Europe) (Disc 3)", "a/3.chd"), Entry("Game (USA) (Disc 4)", "b/4.chd")]);
        var group = Assert.Single(groups);
        Assert.Equal("Game (USA)", group.Title);
        Assert.EndsWith("1.chd", group.Games[0].Path);
        Assert.Equal(2, group.Games.Count);
    }

    [Fact]
    public void RepeatedPathsAndUnnumberedTitlesAreNotDiscSets()
    {
        var game = Entry("Game (Disc 1)", "a.chd");
        Assert.Empty(LibraryAudit.Inspect([game, game, Entry("Game 2", "b.chd")]));
    }

    [Fact]
    public void AuditLeavesFilesAndLibraryMetadataUnchanged()
    {
        using var dir = new TestDirectory();
        var first = dir.File("disc1.chd", "first");
        var second = dir.File("disc2.chd", "second");
        var store = new LibraryStore(Path.Combine(dir.Path, "library.db"));
        store.Upsert([Entry("Game [CD 01]", first), Entry("Game [CD 02]", second)]);
        var games = store.GetAll().ToArray();
        Assert.Single(LibraryAudit.Inspect(games));
        Assert.Equal(games, store.GetAll());
        Assert.Equal("first", File.ReadAllText(first));
        Assert.Equal("second", File.ReadAllText(second));
    }

    [Fact]
    public void CancelledAuditStopsBeforeProcessing()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => LibraryAudit.Inspect([Entry("Game", "a.chd")], cancellation.Token));
    }
}
