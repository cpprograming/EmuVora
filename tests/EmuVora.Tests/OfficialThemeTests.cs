using Avalonia.Headless.XUnit;
using EmuVora.App;
using EmuVora.Core;
using Xunit;
namespace EmuVora.Tests;

public class OfficialThemeTests
{
    [AvaloniaFact][Trait("Category","Network")]
    public async Task OriginalRetroPieCarbonThemeLoadsItsSharedXmlAndSvgLogo()
    {
        // Fetch into test temp storage only. CC-BY-NC-SA theme assets are not redistributed.
        using var dir=new TestDirectory();using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(45)};
        var root=System.IO.Path.Combine(dir.Path,"carbon");
        foreach(var relative in new[]{"nes/theme.xml","carbon.xml","nes/art/system.svg","art/carbon_fiber.png"})
        {
            var target=System.IO.Path.Combine(root,relative);Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)!);
            var bytes=await client.GetByteArrayAsync("https://raw.githubusercontent.com/RetroPie/es-theme-carbon/master/"+relative);await System.IO.File.WriteAllBytesAsync(target,bytes);
        }
        var theme=EmulationStationTheme.Load(root,"NES");
        Assert.Equal("969696",Assert.Single(theme.Elements,e=>e.Name=="gamelist").Properties["primaryColor"]);
        using var logo=ThemeArtwork.Load(Assert.Single(theme.Elements,e=>e.Name=="logo").Properties["path"]);Assert.True(logo.PixelSize.Width>0);
        using var background=ThemeArtwork.Load(Assert.Single(theme.Elements,e=>e.Name=="background").Properties["path"]);Assert.True(background.PixelSize.Height>0);
    }
}
