using System.Net;
using System.Text;
using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;
public class CoverDisplayTests
{
    [Fact] public void RegionalVariantsMatchWholeTitlesWithoutConfusingSequels()
    {
        string[] names=["Super Mario World (USA).png","Super Mario World (Europe).png","Super Mario World 2 - Yoshi's Island (Europe).png"];
        Assert.Equal(names[1],CoverCatalog.Match("Super Mario World [!].sfc".Replace(".sfc",""),names));
        Assert.Equal(names[0],CoverCatalog.Match("Super Mario World (USA)",names));Assert.Null(CoverCatalog.Match("Super Mario",names));
        Assert.Equal(names[2],CoverCatalog.Match("Super Mario World 2: Yoshi’s Island",names));
    }
    private sealed class Source:HttpMessageHandler
    {
        public int IndexRequests,ImageRequests;public bool RateLimit;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            if(request.RequestUri!.Host=="api.github.com")
            {
                IndexRequests++;if(RateLimit)return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"truncated\":false,\"tree\":[{\"type\":\"blob\",\"path\":\"Named_Boxarts/Mario (Europe).png\"},{\"type\":\"blob\",\"path\":\"Named_Boxarts/Zelda (USA).png\"}]}",Encoding.UTF8,"application/json")});
            }
            ImageRequests++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jX1EAAAAASUVORK5CYII="))});
        }
    }
    [Fact] public async Task BatchSkipsOwnCoversCachesTheIndexAndKeepsResultsOnCancellation()
    {
        using var dir=new TestDirectory();var handler=new Source();using var http=new HttpClient(handler);var covers=new Dictionary<string,string>{{"owned",dir.File("own.png","own cover")}};
        var games=new[]{new Game("Mario","SNES","mario"),new Game("Zelda","SNES","zelda"),new Game("Owned","SNES","owned"),new Game("Unknown","SNES","unknown")};
        var source=new CoverCatalog(http,Path.Combine(dir.Path,"cache"));var result=await source.DownloadMissingAsync(games,covers,Path.Combine(dir.Path,"covers"),(game,file)=>covers[game.Path]=file);
        Assert.Equal(2,result.Found);Assert.Equal(1,result.Missing);Assert.Equal(0,result.Failed);Assert.Equal(1,handler.IndexRequests);Assert.Equal(2,handler.ImageRequests);Assert.Equal("own cover",File.ReadAllText(covers["owned"]));
        await new CoverCatalog(http,Path.Combine(dir.Path,"cache")).DownloadAsync(games[0],Path.Combine(dir.Path,"covers"));Assert.Equal(1,handler.IndexRequests);
        covers.Clear();using var cancellation=new CancellationTokenSource();await Assert.ThrowsAsync<OperationCanceledException>(()=>source.DownloadMissingAsync(games,covers,Path.Combine(dir.Path,"cancelled"),(game,file)=>{covers[game.Path]=file;cancellation.Cancel();},token:cancellation.Token));
        Assert.True(File.Exists(covers["mario"]));Assert.False(covers.ContainsKey("zelda"));Assert.Empty(Directory.GetFiles(Path.Combine(dir.Path,"cancelled"),"*.new"));
    }
    [Fact] public async Task RateLimitIsReportedOncePerSourceInsteadOfRetriedForEveryGame()
    {
        using var dir=new TestDirectory();var handler=new Source{RateLimit=true};using var http=new HttpClient(handler);var source=new CoverCatalog(http,Path.Combine(dir.Path,"cache"));
        var result=await source.DownloadMissingAsync([new("Mario","SNES","1"),new("Zelda","SNES","2")],[],dir.Path,(_,_)=>{});
        Assert.Equal(2,result.Failed);Assert.Equal(1,handler.IndexRequests);Assert.All(result.Report,r=>Assert.Contains("Coverlimit",r));
    }
    [Theory][InlineData(1080,480,3,3)][InlineData(2160,480,3,3)][InlineData(2160,480,6,5)][InlineData(600,272,3,3)][InlineData(0,480,3,1)]
    public void InternalResolutionFollowsMonitorHeightAndPerformanceLimit(int height,int native,int maximum,int expected)=>Assert.Equal(expected,DisplayProfiles.RenderScale(height,native,maximum));
    [Fact] public void PixelAndFitProfilesKeepOriginalAspectAndDoNotLoseControllerOwnership()
    {
        using var dir=new TestDirectory();var executable=dir.File("retroarch/retroarch.exe");var data=Path.Combine(dir.Path,"data");var settings=new AppSettings();
        EmulatorSetup.Configure("retroarch",executable,data,firstController:1);DisplayProfiles.Apply("retroarch",executable,"SNES",new("Test",1920,1080,true),settings);
        var file=Path.Combine(Path.GetDirectoryName(executable)!,"retroarch.cfg");var text=File.ReadAllText(file);Assert.Contains("video_scale_integer = true",text);Assert.Contains("video_smooth = false",text);Assert.Contains("video_fullscreen_x = 0",text);Assert.Contains("video_force_aspect = true",text);
        File.WriteAllText(file,text.Replace("input_player1_a_btn = \"1\"","input_player1_a_btn = \"custom\""));
        EmulatorSetup.Configure("retroarch",executable,data,firstController:2);settings.DisplayMode="Fit";DisplayProfiles.Apply("retroarch",executable,"SNES",new("Test",2560,1440,true),settings);
        text=File.ReadAllText(file);Assert.Contains("video_scale_integer = false",text);Assert.Contains("input_player1_joypad_index = \"2\"",text);Assert.Contains("input_player1_a_btn = \"custom\"",text);
        File.WriteAllText(file,text.Replace("video_smooth = false","video_smooth = true"));DisplayProfiles.Apply("retroarch",executable,"SNES",null,settings);Assert.Contains("video_smooth = true",File.ReadAllText(file));settings.AutomaticDisplay=false;var before=File.ReadAllText(file);DisplayProfiles.Apply("retroarch",executable,"SNES",null,settings,true);Assert.Equal(before,File.ReadAllText(file));
    }
    [Fact] public void NativeProfilesPreserveGameAspectAndApplyTheScaleCap()
    {
        using var dir=new TestDirectory();var settings=new AppSettings{MaximumRenderScale=2};var monitor=new DisplayInfo("4K",3840,2160,true);
        foreach(var id in new[]{"duckstation","pcsx2","dolphin","ppsspp"}){var executable=dir.File(id+"/program.exe");DisplayProfiles.Apply(id,executable,"PS1",monitor,settings);}
        var duck=File.ReadAllText(Path.Combine(dir.Path,"duckstation/settings.ini"));Assert.Contains("AspectRatio = Auto (Game Native)",duck);Assert.Contains("ResolutionScale = 2",duck);
        var ps2=File.ReadAllText(Path.Combine(dir.Path,"pcsx2/inis/PCSX2.ini"));Assert.Contains("AspectRatio = Auto 4:3/3:2",ps2);Assert.Contains("upscale_multiplier = 2",ps2);
        var dolphin=File.ReadAllText(Path.Combine(dir.Path,"dolphin/User/Config/GFX.ini"));Assert.Contains("AspectRatio = 0",dolphin);Assert.Contains("InternalResolution = 2",dolphin);
        var psp=File.ReadAllText(Path.Combine(dir.Path,"ppsspp/memstick/PSP/SYSTEM/ppsspp.ini"));Assert.Contains("[DisplayLayout.Landscape]",psp);Assert.Contains("DisplayStretch = False",psp);Assert.Contains("BufferFiltering = 2",psp);
    }
    [Fact][Trait("Category","Network")] public async Task RealCoverIndexResolvesARegionalTitleAndDownloadsPng()
    {
        using var dir=new TestDirectory();using var http=new HttpClient{Timeout=TimeSpan.FromSeconds(40)};var source=new CoverCatalog(http,Path.Combine(dir.Path,"cache"));
        var file=await source.DownloadAsync(new("Super Mario World","SNES","game.sfc"),Path.Combine(dir.Path,"covers"));Assert.NotNull(file);Assert.True(new FileInfo(file!).Length>1000);
    }
}
