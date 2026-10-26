using EmuVora.Core;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace EmuVora.Tests;

public class FrontendRemovalTests
{
    [Fact] public void CoverKeysAndArtworkIdsRemainCompatible()
    {
        using var dir = new TestDirectory();
        var game = new Game("Game", "GBA", dir.File("ROMs/Game & Friends.gba"));
        var previous = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(game.Path).ToUpperInvariant()))).ToLowerInvariant();
        Assert.Equal(previous, LibraryIdentity.GameId(game));
        Assert.Equal(previous, LibraryIdentity.GameId(game with { Title = "Edited", Favorite = true }));
        Assert.Equal(47, EmulatorCatalog.Systems.Select(SystemArtworkIds.SystemId).Distinct().Count());
        Assert.Equal("sega32x", SystemArtworkIds.ThemeId("32X"));
        Assert.Equal("atarilynx", SystemArtworkIds.ThemeId("Atari Lynx"));
        Assert.Equal("wonderswancolor", SystemArtworkIds.ThemeId("WonderSwan Color"));
        Assert.Equal("arcade", SystemArtworkIds.ThemeId("Arcade (MAME)"));
        Assert.Equal("arcade", SystemArtworkIds.ThemeId("Arcade (FinalBurn Neo)"));
    }
    private static (string Settings, string Cover, string Theme, string Engine, string Rom) Legacy(TestDirectory dir)
    {
        var rom = dir.File("data/ROMs/Game.gba", "rom");
        var cover = dir.File("data/frontend/ES-DE/downloaded_media/cover.png", "personal cover");
        var theme = Path.GetDirectoryName(dir.File("data/frontend/ES-DE/themes/custom/theme.xml", "<theme/>"))!;
        var engine = dir.File("data/emulators/esde/old/ES-DE.exe", "old engine");
        var settings = dir.File("data/settings.json", JsonSerializer.Serialize(new
        {
            EsDeExecutable = engine, StartInEsDe = true, EmulationStationTheme = theme,
            OnboardingCompleted = true, AutoDownloadCovers = false, PreferGerman = false,
            Covers = new Dictionary<string, string> { [rom] = cover }, ProtectedCovers = new[] { rom },
            EmulatorPaths = new Dictionary<string, string> { ["esde"] = engine, ["duckstation"] = "custom.exe" },
            Packages = new Dictionary<string, InstalledPackage> { ["esde"] = new("old", engine), ["ALEKFULL-NX"] = new("old", "theme"), ["duckstation"] = new("1", "custom.exe") }
        }, SettingsStore.JsonOptions));
        new LibraryStore(Path.Combine(dir.Path, "data/library.db")).Restore([new("Game", "GBA", rom, true, null, 12)]);
        return (settings, cover, theme, engine, rom);
    }
    [Fact] public void LoadingLegacySettingsRetainsMediaAndDoesNotRewriteFiles()
    {
        using var dir = new TestDirectory(); var old = Legacy(dir); var before = File.ReadAllBytes(old.Settings);
        var store = new SettingsStore(old.Settings); var settings = store.Load();
        Assert.Null(store.LoadWarning); Assert.Equal(before, File.ReadAllBytes(old.Settings));
        Assert.Equal(old.Cover, settings.Covers[old.Rom]); Assert.Contains(old.Rom, settings.ProtectedCovers);
        Assert.Equal(old.Theme, settings.EmulationStationTheme); Assert.Single(settings.Packages);
        Assert.Equal("custom.exe", Assert.Single(settings.EmulatorPaths).Value);
        store.Save(settings); using var json = JsonDocument.Parse(File.ReadAllText(old.Settings));
        Assert.False(json.RootElement.TryGetProperty("EsDeExecutable", out _)); Assert.False(json.RootElement.TryGetProperty("StartInEsDe", out _));
        Assert.Equal("personal cover", File.ReadAllText(old.Cover)); Assert.Equal("old engine", File.ReadAllText(old.Engine));
        Assert.Equal("rom", File.ReadAllText(old.Rom));
    }
    [Fact] public async Task LegacyBackupRestoresCoversAndImportedThemeWithoutFrontend()
    {
        using var dir = new TestDirectory(); var old = Legacy(dir); var source = Path.Combine(dir.Path, "data");
        var original = File.ReadAllBytes(old.Settings); var archive = Path.Combine(dir.Path, "legacy.zip");
        await Backups.CreateAsync(source, archive);
        var manifest = Backups.Inspect(archive);
        Assert.DoesNotContain(manifest.Files, f => f.Path.Contains("ES-DE.exe") || f.Path.EndsWith("Game.gba"));
        var restored = await Backups.RestoreAsync(archive, dir.Path);
        var settings = new SettingsStore(Path.Combine(restored, "settings.json")).Load();
        Assert.Equal("personal cover", File.ReadAllText(settings.Covers[old.Rom]));
        Assert.Equal("<theme/>", File.ReadAllText(Path.Combine(settings.EmulationStationTheme!, "theme.xml")));
        Assert.StartsWith(restored, settings.Covers[old.Rom]); Assert.Empty(settings.Packages);
        var game = Assert.Single(new LibraryStore(Path.Combine(restored, "library.db")).GetAll()); Assert.True(game.Favorite); Assert.Equal(12, game.PlayCount);
        Assert.Equal(original, File.ReadAllBytes(old.Settings)); Assert.Equal("old engine", File.ReadAllText(old.Engine));
    }
    [Theory]
    [InlineData("esde", "https://gitlab.com/es-de/emulationstation-de/-/package_files/357718057/download", "ES-DE.exe")]
    [InlineData("alekfull-nx", "https://codeload.github.com/anthonycaccese/alekfull-nx-es-de/zip/645b2bcc2248e93af8f0f4461a6ee7367fb89365", "capabilities.xml")]
    public void RemovedPackagesAreRejectedBeforeDownloading(string id, string url, string executable)
        => Assert.Throws<InvalidDataException>(() => PackageManager.ValidatePackage(new(id, "old", url, new string('a', 64), executable)));

    [Fact] public async Task DirectEmulatorStillReceivesRomAsOneArgument()
    {
        var probe = Environment.GetEnvironmentVariable("EMUVORA_LAUNCH_PROBE"); if (!OperatingSystem.IsWindows() || probe == null) return;
        using var dir = new TestDirectory(); var rom = dir.File("Game & Friends.chd", "synthetic"); var capture = Path.Combine(dir.Path, "capture.json");
        var previous = Environment.GetEnvironmentVariable("EMUVORA_LAUNCH_CAPTURE"); Environment.SetEnvironmentVariable("EMUVORA_LAUNCH_CAPTURE", capture);
        try
        {
            using var process = new ExternalEmulatorAdapter(EmulatorCatalog.Get("duckstation")).Launch(new("Game", "PS1", rom), probe, null, true);
            await process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(15)).Token);
            Assert.Equal(0, process.ExitCode); Assert.Equal(new[] { "-batch", "-fullscreen", rom }, JsonSerializer.Deserialize<string[]>(File.ReadAllText(capture)));
        }
        finally { Environment.SetEnvironmentVariable("EMUVORA_LAUNCH_CAPTURE", previous); }
    }
}
