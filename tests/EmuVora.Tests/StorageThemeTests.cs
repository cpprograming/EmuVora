using EmuVora.Core;
using EmuVora.App;
using Xunit;
using Avalonia.Headless.XUnit;
namespace EmuVora.Tests;

public class StorageThemeTests
{
    [Fact] public void SelectedLocationPersistsAndMissingDriveDoesNotCreateReplacementLibrary()
    {
        using var dir=new TestDirectory();var location=new DataLocation(dir.File("bootstrap/location.json",""));System.IO.File.Delete(dir.File("bootstrap/location.json"));Assert.Null(location.Read());
        var target=System.IO.Path.Combine(dir.Path,"Selected disk","EmuVora data");location.Select(target);Assert.Equal(target,location.Read());DataLocation.CheckAvailable(target);
        Directory.Delete(target);Assert.Throws<IOException>(()=>DataLocation.CheckAvailable(target));Assert.False(Directory.Exists(target));Assert.Equal(target,location.Read());
        Assert.Throws<IOException>(()=>location.Select("relative folder"));Assert.Throws<IOException>(()=>location.Select(System.IO.Path.GetPathRoot(dir.Path)!));Assert.Equal(target,location.Read());
    }
    [AvaloniaFact] public void IncludesVariablesAndOverridesResolveInsideCopiedThemeSet()
    {
        using var dir=new TestDirectory();var source=System.IO.Path.Combine(dir.Path,"Original theme");
        dir.File("Original theme/common.xml","<theme><formatVersion>3</formatVersion><view name='basic,detailed'><textlist name='gamelist'><primaryColor>00FF00</primaryColor><fontSize>0.03</fontSize></textlist></view></theme>");
        dir.File("Original theme/nes/theme.xml","<theme><formatVersion>3</formatVersion><variables><art>../images/${system}.svg</art></variables><include>../common.xml</include><view name='detailed'><textlist name='gamelist'><primaryColor>FF0000</primaryColor></textlist><image name='logo'><path>${art}</path></image></view></theme>");
        dir.File("Original theme/images/nes.svg","<svg xmlns='http://www.w3.org/2000/svg' width='120' height='60'><rect width='120' height='60' fill='#ff0000'/></svg>");
        var imported=EmulationStationTheme.Import(source,System.IO.Path.Combine(dir.Path,"data"));Directory.Delete(source,true);
        var theme=EmulationStationTheme.Load(imported,"NES");var list=Assert.Single(theme.Elements,e=>e.Type=="textlist");Assert.Equal("FF0000",list.Properties["primaryColor"]);Assert.Equal("0.03",list.Properties["fontSize"]);
        var logo=Assert.Single(theme.Elements,e=>e.Type=="image");Assert.StartsWith(imported,logo.Properties["path"]);using var image=ThemeArtwork.Load(logo.Properties["path"]);Assert.True(image.PixelSize.Width>0);
        Assert.NotEmpty(EmulationStationTheme.Load(imported,"SNES").Warnings);
    }
    [Theory][InlineData("<include>../outside.xml</include>")][InlineData("<include>theme.xml</include>")][InlineData("<view name='detailed'><image name='logo'><path>https://example.com/logo.svg</path></image></view>")]
    public void ThemeCannotEscapeRootOrRecurseForever(string content)
    {
        using var dir=new TestDirectory();dir.File("theme/theme.xml","<theme><formatVersion>3</formatVersion>"+content+"</theme>");Assert.Throws<IOException>(()=>EmulationStationTheme.Load(System.IO.Path.Combine(dir.Path,"theme"),"NES"));
    }
    [Fact] public void XmlEntitiesAndEsDeAreRejectedExplicitly()
    {
        using var dir=new TestDirectory();dir.File("theme/theme.xml","<!DOCTYPE theme [<!ENTITY attack SYSTEM 'file:///secret'>]><theme>&attack;</theme>");Assert.Throws<System.Xml.XmlException>(()=>EmulationStationTheme.Load(System.IO.Path.Combine(dir.Path,"theme"),"NES"));
        dir.File("theme/capabilities.xml","<themeCapabilities/>");Assert.Contains("ES-DE",Assert.Throws<IOException>(()=>EmulationStationTheme.Load(System.IO.Path.Combine(dir.Path,"theme"),"NES")).Message);
    }
    [Fact] public void SvgDoesNotFetchExternalImages()
    {
        using var dir=new TestDirectory();var svg=dir.File("unsafe.svg","<svg xmlns='http://www.w3.org/2000/svg' width='120' height='60'><image href='https://example.com/x.png'/></svg>");Assert.Throws<IOException>(()=>ThemeArtwork.Load(svg));
    }
    [AvaloniaFact] public void StandardSvgDoctypeIsIgnoredWithoutResolvingExternalDtd()
    {
        using var dir=new TestDirectory();var svg=dir.File("standard.svg","<!DOCTYPE svg PUBLIC '-//W3C//DTD SVG 1.1//EN' 'https://invalid.example/unavailable.dtd' [<!ENTITY ns_extend 'http://ns.adobe.com/Extensibility/1.0/'>]><svg xmlns='http://www.w3.org/2000/svg' xmlns:x='&ns_extend;' width='120' height='60'><rect width='120' height='60' fill='#ff0000'/></svg>");using var image=ThemeArtwork.Load(svg);Assert.True(image.PixelSize.Width>0);
    }
}
