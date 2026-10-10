using EmuVora.Core;
using System.Text.Json;
using Xunit;
namespace EmuVora.Tests;
public class OfficialPackageTests
{
    [Fact][Trait("Category","Network")]
    public async Task AllOfficialPackagesVerifyExtractAndContainTheirEntryPoints()
    {
        var file=Environment.GetEnvironmentVariable("EMUVORA_PACKAGE_CATALOG")??throw new InvalidOperationException("Network tests require EMUVORA_PACKAGE_CATALOG.");
        var catalog=JsonSerializer.Deserialize<PackageCatalog>(System.IO.File.ReadAllText(file).TrimStart('\uFEFF'),SettingsStore.JsonOptions)!;
        using var dir=new TestDirectory();using var manager=new PackageManager();
        foreach(var package in catalog.Packages)
        {
            var path=await manager.InstallAsync(package,dir.Path);Assert.True(System.IO.File.Exists(path),package.Id);
            if(package.Id=="retroarch-cores")foreach(var core in PackageManager.CoreIds.Values.Distinct())Assert.True(System.IO.File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!,core+"_libretro.dll")),core);
        }
    }
}
