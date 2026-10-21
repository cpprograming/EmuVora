using System.Net;
using System.Text;
using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;

public class CoverRecoveryTests
{
    [Fact] public void CollectionPrefixesAndReleaseSuffixesUseWholeTitleFallbacksOnly()
    {
        string[] names=["Moon Rally (Europe).png","Moon Rally 2 (USA).png","007 Moon Rally (USA).png","1942 (USA).png"];
        Assert.Equal(names[0],CoverCatalog.Match("001 Moon Rally",names));
        Assert.Equal(names[1],CoverCatalog.Match("00042_Moon_Rally_2_USA_UMD_PSP-TEAM",names));
        Assert.Equal(names[2],CoverCatalog.Match("007 Moon Rally",names));
        Assert.Equal(names[3],CoverCatalog.Match("1942",names));
        Assert.Null(CoverCatalog.Match("001 Moon",names));
        Assert.Null(CoverCatalog.Match("002 Moon Rally 3",names));
        Assert.Null(CoverCatalog.Match("2 Moon Rally",names));
    }

    [Fact] public void CompleteWordSetsAllowRomanNumeralsAndReorderingButRejectAmbiguity()
    {
        string[] names=["Pocket Land - Super Pocket Land 3 (USA).png","Pocket Land - Super Pocket Land 3 (Europe).png","Crystal Quest III (Europe).png"];
        Assert.Equal(names[1],CoverCatalog.Match("003 Super Pocket Land 3 - Pocket Land",names));
        Assert.Equal(names[0],CoverCatalog.Match("003 Super Pocket Land 3 - Pocket Land (USA)",names));
        Assert.Equal(names[2],CoverCatalog.Match("Crystal Quest 3",names));
        Assert.Null(CoverCatalog.Match("Crystal Quest 2",names));
        Assert.Null(CoverCatalog.Match("Crystal",names));
        Assert.Null(CoverCatalog.Match("003 Star II Quest",["Star Quest 2 (USA).png","Quest Star 2 (Europe).png"]));
    }

    [Fact] public void ExplicitRegionalAliasesMatchWholeTitlesWithoutSplittingCompilations()
    {
        string[] names=["Moon Pilot (USA).png","Lunar Pilot (Europe).png"];
        Assert.Equal(names[1],CoverCatalog.Match("Moon Pilot ~ Lunar Pilot (USA, Europe)",names));
        Assert.Null(CoverCatalog.Match("Moon Pilot + Lunar Pilot",names));
        Assert.Null(CoverCatalog.Match("Moon Pilot & Lunar Pilot",names));
        Assert.Null(CoverCatalog.Match("Moon ~ Lunar",names));
    }

    private sealed class TruncatedSource(bool subtreeTruncated=false):HttpMessageHandler
    {
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Requests++;
            var url=request.RequestUri!;
            if(url.Host=="raw.githubusercontent.com")return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jX1EAAAAASUVORK5CYII="))});
            var json=url.Query.Contains("recursive")?"{\"truncated\":true,\"tree\":[]}":url.AbsolutePath.EndsWith("/master")?"{\"truncated\":false,\"tree\":[{\"type\":\"tree\",\"path\":\"Named_Boxarts\",\"sha\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"}]}":"{\"truncated\":"+(subtreeTruncated?"true":"false")+",\"tree\":[{\"type\":\"blob\",\"path\":\"Moon Rally (Europe).png\"}]}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(json,Encoding.UTF8,"application/json")});
        }
    }

    [Fact] public async Task TruncatedRecursiveCatalogUsesCompleteBoxartSubtreeAndCachesIt()
    {
        using var dir=new TestDirectory();var handler=new TruncatedSource();using var http=new HttpClient(handler);
        var source=new CoverCatalog(http,Path.Combine(dir.Path,"cache"));
        Assert.NotNull(await source.DownloadAsync(new("001 Moon Rally","Commodore 64","fixture"),dir.Path));
        Assert.Equal(4,handler.Requests);
        Assert.NotNull(await new CoverCatalog(http,Path.Combine(dir.Path,"cache")).DownloadAsync(new("Moon Rally","Commodore 64","fixture"),dir.Path));
        Assert.Equal(5,handler.Requests);
    }

    [Fact] public async Task AnIncompleteBoxartSubtreeIsNeverSavedAsACompleteCache()
    {
        using var dir=new TestDirectory();using var http=new HttpClient(new TruncatedSource(true));
        await Assert.ThrowsAsync<IOException>(()=>new CoverCatalog(http,Path.Combine(dir.Path,"cache")).DownloadAsync(new("Moon Rally","Commodore 64","fixture"),dir.Path));
        Assert.False(Directory.Exists(Path.Combine(dir.Path,"cache")));
    }
}
