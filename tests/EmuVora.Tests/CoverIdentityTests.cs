using System.IO.Compression;
using System.Net;
using System.Text;
using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;

public class CoverIdentityTests
{
    [Fact] public void SemanticParenthesesSurviveAndRegionMetadataStillRanksCovers()
    {
        string[] names=["Crystal Quest - Lost Chapter (Europe).png","Crystal Quest - Lost Chapter (USA).png","Crystal Quest (Europe).png"];
        Assert.Equal(names[1],CoverCatalog.Match("Crystal Quest (Lost Chapter) (USA)",names));
        Assert.Equal(names[0],CoverCatalog.Match("Crystal Quest (Lost Chapter) (BR) [!]",names));
        Assert.Null(CoverCatalog.Match("Crystal Quest (Other Chapter)",names));
        Assert.Null(CoverCatalog.Match("World Rally (2004)",["World Rally (2000).png"]));
        Assert.Equal("Quest III - Lost Chapter (USA).png",CoverCatalog.Match("Quest 3 (Lost Chapter) (USA)",["Quest III - Lost Chapter (USA).png"]));
    }
    [Fact] public void KnownAliasesAreScopedToTheirPlatformAndExactEditionsWin()
    {
        string[] names=["Mega Man 8 (USA).png","Mega Man 9 (USA).png"];
        Assert.Equal(names[0],CoverCatalog.Match("Mega Man 8 - Anniversary Collector's Edition (USA)",names,system:"Saturn"));
        Assert.Null(CoverCatalog.Match("Mega Man 8 - Anniversary Collector's Edition",names,system:"NES"));
        Assert.Null(CoverCatalog.Match("Mega Man 9 - Anniversary Collector's Edition",names,system:"Saturn"));
        var exact="Mega Man 8 - Anniversary Collector's Edition (USA).png";
        Assert.Equal(exact,CoverCatalog.Match("Mega Man 8 - Anniversary Collector's Edition (USA)",[names[0],exact],system:"Saturn"));
        Assert.Equal("Harvest Moon GB (Europe).png",CoverCatalog.Match("043 Harvest Moon",["Harvest Moon GB (Europe).png"],system:"GB"));
        Assert.Equal("Spider-Man - X-Men - Arcade's Revenge (USA, Europe).png",CoverCatalog.Match("049 Spider-Man and the X-Men-Arcade's Revenge",["Spider-Man - X-Men - Arcade's Revenge (USA, Europe).png"],system:"GB"));
    }
    private static byte[] Rom(byte flag)
    {
        var bytes=new byte[0x150];Convert.FromHexString("CEED6666CC0D000B03730083000C000D0008111F8889000EDCCC6EE6DDDDD999BBBB67636E0EECCCDDDC999FBBB9333E").CopyTo(bytes,0x104);
        bytes[0x143]=flag;bytes[0x14D]=unchecked((byte)(-bytes.Skip(0x134).Take(25).Sum(b=>(int)b)-25));return bytes;
    }
    [Theory][InlineData(0,"GB")][InlineData(0x80,"GBC")][InlineData(0xC0,"GBC")]
    public async Task ValidatedHeaderSelectsCatalogDespiteMisleadingFileExtension(int flag,string expected)
    {
        using var dir=new TestDirectory();var file=dir.File("fixture.gbc");File.WriteAllBytes(file,Rom((byte)flag));Assert.Equal(expected,await GameBoyCoverSystem.DetectAsync(file));
    }
    [Fact] public async Task InvalidHeadersAndAmbiguousArchivesNeverOverrideThePlatform()
    {
        using var dir=new TestDirectory();var file=dir.File("fixture.gb");var rom=Rom(0x80);rom[0x14D]^=1;File.WriteAllBytes(file,rom);Assert.Null(await GameBoyCoverSystem.DetectAsync(file));
        rom=Rom(0x80);rom[0x104]=0;File.WriteAllBytes(file,rom);Assert.Null(await GameBoyCoverSystem.DetectAsync(file));
        var zip=dir.File("fixture.zip");
        using(var archive=ZipFile.Open(zip,ZipArchiveMode.Update)){using var stream=archive.CreateEntry("fixture.gb").Open();stream.Write(Rom(0x80));}
        Assert.Equal("GBC",await GameBoyCoverSystem.DetectAsync(zip));
        using(var archive=ZipFile.Open(zip,ZipArchiveMode.Update)){using var stream=archive.CreateEntry("other.gb").Open();stream.Write(Rom(0));}
        Assert.Null(await GameBoyCoverSystem.DetectAsync(zip));
        Assert.Null(await GameBoyCoverSystem.DetectAsync(dir.File("short.gb","short")));
        using var cancel=new CancellationTokenSource();cancel.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>GameBoyCoverSystem.DetectAsync(file,cancel.Token));
    }
    private sealed class Source:HttpMessageHandler
    {
        public List<Uri> Requests=[];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Requests.Add(request.RequestUri!);
            if(request.RequestUri!.Host=="api.github.com")
            {
                var title=request.RequestUri.AbsolutePath.Contains("Game_Boy_Color")?"Tetris DX":"Tetris";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"truncated\":false,\"tree\":[{\"type\":\"blob\",\"path\":\"Named_Boxarts/"+title+" (World).png\"}]}",Encoding.UTF8,"application/json")});
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jX1EAAAAASUVORK5CYII="))});
        }
    }
    [Fact] public async Task ColorCartridgeUsesColorArtworkWithoutChangingSavedGameIdentity()
    {
        using var dir=new TestDirectory();var file=dir.File("fixture.gb");File.WriteAllBytes(file,Rom(0x80));var game=new Game("038 Tetris DX","GB",file);
        var handler=new Source();using var http=new HttpClient(handler);var result=await new CoverCatalog(http,Path.Combine(dir.Path,"cache")).DownloadAsync(game,Path.Combine(dir.Path,"covers"));
        Assert.NotNull(result);Assert.Equal(LibraryIdentity.GameId(game)+".png",Path.GetFileName(result));Assert.All(handler.Requests,u=>Assert.Contains("Nintendo_-_Game_Boy_Color",u.AbsolutePath));
        Assert.Equal("GB",game.System);Assert.Equal("038 Tetris DX",game.Title);Assert.Equal(Rom(0x80),File.ReadAllBytes(file));
    }
    [Fact] public async Task UnverifiedCartridgesNeverGuessAnotherPlatformsCover()
    {
        using var dir=new TestDirectory();var file=dir.File("fixture.gb","not a cartridge");var handler=new Source();using var http=new HttpClient(handler);
        Assert.Null(await new CoverCatalog(http,Path.Combine(dir.Path,"cache")).DownloadAsync(new("Tetris DX","GB",file),dir.Path));
        var request=Assert.Single(handler.Requests);Assert.DoesNotContain("Game_Boy_Color",request.AbsolutePath);
    }
}
