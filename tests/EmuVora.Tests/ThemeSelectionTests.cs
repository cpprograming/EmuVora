using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;

public class ThemeSelectionTests
{
    public static string Fixture(TestDirectory dir)
    {
        dir.File("theme/capabilities.xml", "<themeCapabilities><variant name='first'><label>Erste Variante</label></variant><variant name='second'><label>Zweite Variante</label></variant><variant name='hidden'><selectable>false</selectable></variant><colorScheme name='dark'/><colorScheme name='light'/><aspectRatio>16:9</aspectRatio><aspectRatio>4:3</aspectRatio></themeCapabilities>");
        dir.File("theme/theme.xml", "<theme><colorScheme name='dark'><variables><fg>EEEEEE</fg><bg>172338</bg></variables></colorScheme><colorScheme name='light'><variables><fg>111111</fg><bg>F5F7F8</bg></variables></colorScheme><view name='gamelist'><image name='background'><color>${bg}</color></image><textlist name='game-list'><primaryColor>${fg}</primaryColor></textlist><image name='system-logo'><path>logo.svg</path></image></view><variant name='first'><view name='gamelist'><text name='game-name'><color>00AA00</color></text></view></variant><variant name='second'><view name='gamelist'><text name='game-name'><color>AA0000</color></text></view></variant><aspectRatio name='16:9'><include>wide.xml</include></aspectRatio><aspectRatio name='4:3'><include>standard.xml</include></aspectRatio></theme>");
        dir.File("theme/wide.xml", "<theme><view name='gamelist'><text name='description'><color>FFFF00</color></text></view></theme>");
        dir.File("theme/standard.xml", "<theme><view name='gamelist'><text name='description'><color>00FFFF</color></text></view></theme>");
        dir.File("theme/logo.svg", "<svg xmlns='http://www.w3.org/2000/svg' width='160' height='50'><rect width='160' height='50' fill='#33aa99'/></svg>");
        dir.File("theme/LICENSE.txt", "Synthetic fixture, no upstream assets");
        return Path.Combine(dir.Path, "theme");
    }
    [Fact] public void DeclaredOptionsExcludeHiddenAndKeepLabelsAndDefaults()
    {
        using var dir = new TestDirectory(); var root = Fixture(dir); var options = EmulationStationTheme.InspectOptions(root);
        Assert.Equal(new[] { "first", "second" }, options.Variants.Select(o => o.Id)); Assert.Equal("Erste Variante", options.Variants[0].Label);
        Assert.Equal(new("first", "dark", "16:9"), options.Default); Assert.Equal("light", EmulationStationTheme.InspectOptions(root, false).Default.ColorScheme);
        var theme = EmulationStationTheme.Load(root, "GBA", selection: new("second", "light", "4:3"));
        Assert.Equal("111111", theme.Elements.Single(e => e.Name == "gamelist").Properties["primaryColor"]);
        Assert.Equal("AA0000", theme.Elements.Single(e => e.Name == "md_name").Properties["color"]);
        Assert.Equal("00FFFF", theme.Elements.Single(e => e.Name == "description").Properties["color"]);
    }
    [Theory]
    [InlineData("hidden", "dark", "16:9")][InlineData("unknown", "dark", "16:9")]
    [InlineData("first", "unknown", "16:9")][InlineData("first", "dark", "21:9")]
    public void InvalidSelectionsAreRejected(string variant, string scheme, string aspect)
    {
        using var dir = new TestDirectory(); var root = Fixture(dir);
        Assert.Throws<IOException>(() => EmulationStationTheme.PreviewImport(root, "NES", selection: new(variant, scheme, aspect)));
        Assert.False(Directory.Exists(Path.Combine(dir.Path, "data")));
    }
    [Fact] public void ReviewIsReadOnlyAndImportPersistsSelectionAndLicenses()
    {
        using var dir = new TestDirectory(); var root = Fixture(dir); var before = Directory.GetFiles(root).ToDictionary(f => Path.GetFileName(f)!, File.ReadAllBytes);
        var selected = new ThemeSelection("second", "light", "4:3"); var plan = EmulationStationTheme.PreviewImport(root, "GBA", selection: selected);
        Assert.False(Directory.Exists(Path.Combine(dir.Path, "data")));
        var imported = EmulationStationTheme.ApplyImport(plan, Path.Combine(dir.Path, "data"));
        Assert.Equal("111111", EmulationStationTheme.Load(imported, "GBA").Elements.Single(e => e.Name == "gamelist").Properties["primaryColor"]);
        Assert.Equal("AA0000", EmulationStationTheme.Load(imported, "PS1", false).Elements.Single(e => e.Name == "md_name").Properties["color"]);
        Assert.Equal("Synthetic fixture, no upstream assets", File.ReadAllText(Path.Combine(imported, "LICENSE.txt")));
        Assert.All(before, entry => Assert.Equal(entry.Value, File.ReadAllBytes(Path.Combine(root, entry.Key!))));
        Assert.False(File.Exists(Path.Combine(root, "emuvora-theme-options.json"))); Assert.Contains("second", File.ReadAllText(Path.Combine(imported, "emuvora-import-report.txt")));
    }
    [Theory][InlineData("same-size")][InlineData("add")][InlineData("remove")]
    public void ChangedSourcesCannotApplyAnOldPreview(string change)
    {
        using var dir = new TestDirectory(); var root = Fixture(dir); var plan = EmulationStationTheme.PreviewImport(root, "NES");
        if (change == "add") dir.File("theme/new.txt", "new");
        else if (change == "remove") File.Delete(Path.Combine(root, "LICENSE.txt"));
        else { var file = Path.Combine(root, "theme.xml"); var stamp = File.GetLastWriteTimeUtc(file); File.WriteAllText(file, File.ReadAllText(file).Replace("EEEEEE", "DDDDDD")); File.SetLastWriteTimeUtc(file, stamp); }
        Assert.Throws<IOException>(() => EmulationStationTheme.ApplyImport(plan, Path.Combine(dir.Path, "data"))); Assert.False(Directory.Exists(Path.Combine(dir.Path, "data")));
    }
    [Fact] public void CancellationAndUnsafeSelectedVariantDoNotCreateAnImport()
    {
        using var dir = new TestDirectory(); var root = Fixture(dir); var plan = EmulationStationTheme.PreviewImport(root, "NES");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => EmulationStationTheme.ApplyImport(plan, Path.Combine(dir.Path, "data"), cancellation.Token));
        var file = Path.Combine(root, "theme.xml"); File.WriteAllText(file, File.ReadAllText(file).Replace("<variant name='second'>", "<variant name='second'><include>../outside.xml</include>"));
        Assert.Throws<IOException>(() => EmulationStationTheme.PreviewImport(root, "NES", selection: new("second", "dark", "16:9")));
        Assert.False(Directory.Exists(Path.Combine(dir.Path, "data")));
    }
    [Fact] public void LegacyImportStillFollowsBasePaletteAndInvalidSavedSelectionFailsClosed()
    {
        using var dir = new TestDirectory(); var root = Fixture(dir); var imported = EmulationStationTheme.Import(root, Path.Combine(dir.Path, "data"));
        Assert.Equal("111111", EmulationStationTheme.Load(imported, "NES", false).Elements.Single(e => e.Name == "gamelist").Properties["primaryColor"]);
        File.WriteAllText(Path.Combine(imported, "emuvora-theme-options.json"), "{\"Variant\":\"hidden\",\"ColorScheme\":\"dark\",\"AspectRatio\":\"16:9\"}");
        Assert.Throws<IOException>(() => EmulationStationTheme.Load(imported, "NES"));
        File.WriteAllText(Path.Combine(imported, "emuvora-theme-options.json"), new string('a', 65537)); Assert.Throws<IOException>(() => EmulationStationTheme.Load(imported, "NES"));
    }
}
