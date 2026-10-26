using EmuVora.Core;
using Xunit;
namespace EmuVora.Tests;
public class GdiTests
{
    private static string Manifest="3\n1 0 4 2352 \"track 1.iso\" 0\n2 450 0 2352 audio.raw 0\n3 45000 4 2048 data.bin 0\n";
    internal static Game Disc(TestDirectory dir,int number)
    {
        var folder=$"Dreamcast/Game (Disc {number})/";dir.File(folder+"track 1.iso","synthetic first track");dir.File(folder+"audio.raw","synthetic audio");dir.File(folder+"data.bin","synthetic data");return new($"Game (Disc {number})","Dreamcast",Path.GetFullPath(dir.File(folder+$"Game (Disc {number}).gdi",Manifest)));
    }
    private static LibraryAuditGroup Group(params Game[] games)=>Assert.Single(LibraryAudit.Inspect(games),g=>g.Kind=="Mehrdisc-Kandidat");
    [Fact] public void GdiSiblingDiscsProduceRelativePlaylistWithoutTrackCardsOrMetadataChanges()
    {
        using var dir=new TestDirectory();var a=Disc(dir,1);var b=Disc(dir,2);var before=Directory.GetFiles(dir.Path,"*",SearchOption.AllDirectories).ToDictionary(p=>p,File.ReadAllBytes);Assert.Equal(2,RomScanner.Scan(dir.Path).Count);var plan=LibraryContent.PreviewPlaylist(Group(b,a));Assert.Equal(8,plan.Files.Count);Assert.Equal("Game (Disc 1)/Game (Disc 1).gdi\nGame (Disc 2)/Game (Disc 2).gdi\n",plan.Content);
        var game=LibraryContent.CreatePlaylist(plan);Assert.Equal("Dreamcast",game.System);Assert.Equal(game.Path,Assert.Single(RomScanner.Scan(dir.Path)).Path);Assert.Throws<IOException>(()=>LibraryContent.CreatePlaylist(plan));foreach(var(path,bytes)in before)Assert.Equal(bytes,File.ReadAllBytes(path));
    }
    [Theory][InlineData("count")][InlineData("duplicate")][InlineData("type")][InlineData("sector")][InlineData("fad")][InlineData("offset")][InlineData("missing")][InlineData("escape")][InlineData("absolute")][InlineData("extra")]
    public void MalformedAndUnsafeTrackReferencesAreRejected(string kind)
    {
        using var dir=new TestDirectory();var a=Disc(dir,1);var text=kind switch{"count"=>Manifest.Replace("3\n","4\n"),"duplicate"=>Manifest.Replace("2 450","1 450"),"type"=>Manifest.Replace("1 0 4","1 0 0"),"sector"=>Manifest.Replace("2048","2448"),"fad"=>Manifest.Replace("45000","44999"),"offset"=>Manifest.Replace("data.bin 0","data.bin 9999"),"missing"=>Manifest.Replace("data.bin","absent.bin"),"escape"=>Manifest.Replace("data.bin","../../outside.bin"),"absolute"=>Manifest.Replace("data.bin","C:/outside.bin"),_=>Manifest+"4 45001 4 2048 data.bin 0\n"};dir.File("outside.bin","unrelated");File.WriteAllText(a.Path,text);Assert.ThrowsAny<IOException>(()=>GdiManifest.Read(a.Path));Assert.False(File.Exists(Path.Combine(dir.Path,"Dreamcast/Game.m3u")));
    }
    [Fact] public void ChangedTrackOrDescriptorStopsCreationAndCancellationWritesNothing()
    {
        using var dir=new TestDirectory();var a=Disc(dir,1);var b=Disc(dir,2);var group=Group(a,b);var plan=LibraryContent.PreviewPlaylist(group);File.AppendAllText(Path.Combine(Path.GetDirectoryName(b.Path)!,"data.bin"),"changed");Assert.Throws<IOException>(()=>LibraryContent.CreatePlaylist(plan));plan=LibraryContent.PreviewPlaylist(group);File.AppendAllText(a.Path,"\n");Assert.Throws<IOException>(()=>LibraryContent.CreatePlaylist(plan));plan=LibraryContent.PreviewPlaylist(group);using var source=new CancellationTokenSource();source.Cancel();Assert.ThrowsAny<OperationCanceledException>(()=>LibraryContent.CreatePlaylist(plan,source.Token));Assert.False(File.Exists(plan.Path));Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(plan.Path)!,"*.tmp"));
    }
    [Fact] public void SiblingFoldersMustMatchExplicitDiscNamesAndRegionsRemainSeparate()
    {
        using var dir=new TestDirectory();var a=Disc(dir,1);var b=Disc(dir,2);var renamed=Path.Combine(dir.Path,"Dreamcast/Unrelated");Directory.Move(Path.GetDirectoryName(b.Path)!,renamed);b=b with{Path=Path.Combine(renamed,Path.GetFileName(b.Path))};Assert.DoesNotContain(LibraryAudit.Inspect([a,b]),g=>g.Kind=="Mehrdisc-Kandidat");Assert.Throws<IOException>(()=>LibraryContent.PreviewPlaylist(new("Mehrdisc-Kandidat","Game","Dreamcast",[a,b])));
    }
    [Fact] public void Utf8BomIsAcceptedAndMissingGdiTracksBlockRomRelocation()
    {
        using var dir=new TestDirectory();var a=Disc(dir,1);File.WriteAllText(a.Path,"\uFEFF"+Manifest);Assert.Equal(3,GdiManifest.Read(a.Path).Count);
        var old=Path.Combine(dir.Path,"Old");var next=Path.Combine(dir.Path,"New");Directory.CreateDirectory(old);Directory.CreateDirectory(next);var oldGdi=Path.Combine(old,"Disc.gdi");var newGdi=Path.Combine(next,"Disc.gdi");File.WriteAllText(oldGdi,Manifest);File.WriteAllText(newGdi,Manifest);var stamp=File.GetLastWriteTimeUtc(oldGdi);File.SetLastWriteTimeUtc(newGdi,stamp);var game=new Game("Game","Dreamcast",oldGdi);File.Delete(oldGdi);var plan=RomRelocation.Preview([game],old,next);Assert.Empty(plan.Suggestions);Assert.NotEmpty(plan.Notes);
    }
}
