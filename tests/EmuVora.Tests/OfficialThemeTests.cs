using Avalonia.Headless.XUnit;
using EmuVora.App;
using EmuVora.Core;
using Xunit;
namespace EmuVora.Tests;

public class OfficialThemeTests
{
    [AvaloniaFact][Trait("Category","Network")]
    public async Task AlekfullEsDeTemplateAdaptsItsDefaultVariantAndDarkColors()
    {
        using var dir=new TestDirectory();using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(45)};var root=Path.Combine(dir.Path,"alekfull");Directory.CreateDirectory(root);
        // Pinned upstream XML is fetched only into temporary test storage; no theme assets are redistributed.
        foreach(var file in new[]{"theme.xml","capabilities.xml","colors.xml","aspect-ratio-16-9.xml"})File.WriteAllBytes(Path.Combine(root,file),await client.GetByteArrayAsync("https://raw.githubusercontent.com/anthonycaccese/alekfull-nx-es-de/645b2bcc2248e93af8f0f4461a6ee7367fb89365/"+file));
        var options=EmulationStationTheme.InspectOptions(root);Assert.Equal(4,options.Variants.Count);Assert.Equal(4,options.ColorSchemes.Count);Assert.Equal(4,options.AspectRatios.Count);Assert.DoesNotContain(options.Variants,o=>o.Id=="basic");
        foreach(var system in new[]{"PS1","GameCube","GBA"})
        {
            var theme=EmulationStationTheme.Load(root,system);Assert.Equal("777777",theme.Elements.Single(e=>e.Name=="gamelist").Properties["primaryColor"]);Assert.Contains(theme.Elements,e=>e.Name=="logo"&&e.Properties.ContainsKey("path"));Assert.Contains(theme.Warnings,w=>w.Contains("ES-DE an natives"));
            var light=EmulationStationTheme.Load(root,system,selection:new("gamelist-metadata-on-boxart","light-original","16:9"));Assert.Equal("a0a0a0",light.Elements.Single(e=>e.Name=="gamelist").Properties["primaryColor"]);
            var layout=ThemeLayout.From(theme);Assert.True(layout.Usable);Assert.Equal(.082291666666667,layout.Elements["gamelist"].Left,10);Assert.Equal(.348958333333333,layout.Elements["gamelist"].Width,10);Assert.Contains(theme.Elements,e=>e.Name=="md_image"&&e.Type=="image");
        }
    }
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
