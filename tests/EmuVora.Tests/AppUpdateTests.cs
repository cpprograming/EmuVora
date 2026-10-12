using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;

public class AppUpdateTests
{
    private static readonly byte[] Binary=Encoding.UTF8.GetBytes("MZverified-test-setup");
    private static AppRelease Release=>new(new Version(0,7,99),new("https://github.com/cpprograming/EmuVora/releases/download/v0.7.99/EmuVora-Setup.exe"),Convert.ToHexString(SHA256.HashData(Binary)),Binary.Length,"Release notes");
    private sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> reply):HttpMessageHandler
    {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>Task.FromResult(reply(request));}
    private static string Metadata(string? digest=null,string? url=null,bool preview=false)=>JsonSerializer.Serialize(new{tag_name="v0.7.99",draft=false,prerelease=preview,body="Release notes",assets=new[]{new{name="EmuVora-Setup.exe",size=Binary.Length,digest=digest??"sha256:"+Release.Sha256,browser_download_url=url??Release.Download.ToString()}}});
    [Fact] public async Task DetectsOnlyNewStableOfficialRelease()
    {
        using var http=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new StringContent(Metadata())}));var updates=new AppUpdates(http);
        Assert.Equal(Release.Version,(await updates.CheckAsync(new(0,7,1)))!.Version);
        Assert.Null(await updates.CheckAsync(new(0,7,99)));Assert.Null(await updates.CheckAsync(new(0,8,0)));
        using var preview=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new StringContent(Metadata(preview:true))}));Assert.Null(await new AppUpdates(preview).CheckAsync(new(0,7,1)));
    }
    [Theory][InlineData("sha256:bad",null)][InlineData(null,"https://example.com/EmuVora-Setup.exe")]
    public async Task RejectsUnverifiedOrForeignRelease(string? hash,string? url)
    {
        using var http=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new StringContent(Metadata(hash,url))}));await Assert.ThrowsAsync<InvalidDataException>(()=>new AppUpdates(http).CheckAsync(new(0,7,1)));
    }
    [Fact] public async Task NoPublicReleaseIsNotAnError()
    {using var http=new HttpClient(new Handler(_=>new(HttpStatusCode.NotFound)));Assert.Null(await new AppUpdates(http).CheckAsync(new(0,7,1)));}
    [Fact] public async Task ChecksDownloadAndAgainBeforeInstallation()
    {
        var directory=Path.Combine(Path.GetTempPath(),"EmuVora-update-test-"+Guid.NewGuid());
        try
        {
            using var http=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new ByteArrayContent(Binary)}));
            var file=await new AppUpdates(http).DownloadAsync(Release,directory);await AppUpdates.VerifyAsync(file,Release);
            await File.WriteAllBytesAsync(file,Encoding.UTF8.GetBytes("MZtampered-test-setup"));await Assert.ThrowsAsync<InvalidDataException>(()=>AppUpdates.VerifyAsync(file,Release));
        }
        finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }
    [Fact] public async Task FailedAndCancelledDownloadsLeaveNoExecutable()
    {
        var directory=Path.Combine(Path.GetTempPath(),"EmuVora-update-test-"+Guid.NewGuid());
        try
        {
            using var http=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new ByteArrayContent(Binary)}));
            await Assert.ThrowsAsync<InvalidDataException>(()=>new AppUpdates(http).DownloadAsync(Release with{Sha256=new string('0',64)},directory));Assert.Empty(Directory.GetFiles(directory));
            using var cancel=new CancellationTokenSource();cancel.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>new AppUpdates(http).DownloadAsync(Release,directory,token:cancel.Token));Assert.Empty(Directory.GetFiles(directory));
        }
        finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }
    [Fact] public void SetupArgumentsPreservePathsAsSeparateArguments()
    {
        var root=Path.Combine(Path.GetTempPath(),"EmuVora space & test");var setup=Path.Combine(root,"Setup.exe");
        var info=AppUpdates.SetupStartInfo(setup,root,Path.Combine(root,"Separate data"),1234);
        Assert.False(info.UseShellExecute);Assert.Equal(new[]{"--install-dir",root,"--data-dir",Path.Combine(root,"Separate data"),"--wait-for-pid","1234","--no-shortcuts"},info.ArgumentList);
    }
}
