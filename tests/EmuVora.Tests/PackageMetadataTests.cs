using System.Net;
using System.Text.Json;
using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;
public class PackageMetadataTests
{
    private sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> reply):HttpMessageHandler
    {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>Task.FromResult(reply(request));}
    [Fact] public async Task MetadataTokenIsNeverSentToDownloadHostsAndLatestDigestIsResolved()
    {
        var apiCalls=0;var downloadCalls=0;
        var package=new EmulatorPackage("duckstation","old","https://github.com/stenzek/duckstation/releases/download/latest/duckstation-windows-x64-release.zip",new string('0',64),"duckstation.exe");
        using var handler=new Handler(request=>
        {
            if(request.RequestUri!.Host=="api.github.com")
            {
                apiCalls++;Assert.Equal("Bearer",request.Headers.Authorization?.Scheme);Assert.Equal("synthetic-test-token",request.Headers.Authorization?.Parameter);
                return new(HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(new{tag_name="latest",assets=new[]{new{name="duckstation-windows-x64-release.zip",id=42,browser_download_url=package.Url,digest="sha256:"+new string('a',64)}}}))};
            }
            downloadCalls++;Assert.Null(request.Headers.Authorization);return new(HttpStatusCode.OK){Content=new ByteArrayContent([1,2,3])};
        });
        using var manager=new PackageManager(handler,"synthetic-test-token");
        var catalog=await manager.ResolveCurrentReleasesAsync(new(1,DateTimeOffset.UtcNow,[package]));
        Assert.Equal("latest-42",catalog.Packages[0].Version);Assert.Equal(new string('a',64),catalog.Packages[0].Sha256);
        using var dir=new TestDirectory();await Assert.ThrowsAsync<InvalidDataException>(()=>manager.InstallAsync(catalog.Packages[0],dir.Path));
        Assert.Equal(1,apiCalls);Assert.Equal(1,downloadCalls);
    }
}
