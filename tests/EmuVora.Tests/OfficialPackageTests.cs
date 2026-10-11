using EmuVora.Core;
using System.Text.Json;
using System.Runtime.InteropServices;
using Xunit;
namespace EmuVora.Tests;
public class OfficialPackageTests
{
    [StructLayout(LayoutKind.Sequential)] private struct CoreInfo {public IntPtr Name,Version,Extensions;public byte NeedFullPath,BlockExtract;}
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GetSystemInfo(out CoreInfo info);
    [Fact][Trait("Category","Network")]
    public async Task AllOfficialPackagesVerifyExtractAndContainTheirEntryPoints()
    {
        var file=Environment.GetEnvironmentVariable("EMUVORA_PACKAGE_CATALOG")??throw new InvalidOperationException("Network tests require EMUVORA_PACKAGE_CATALOG.");
        var catalog=JsonSerializer.Deserialize<PackageCatalog>(System.IO.File.ReadAllText(file).TrimStart('\uFEFF'),SettingsStore.JsonOptions)!;
        using var dir=new TestDirectory();using var manager=new PackageManager();
        // "latest" is a mutable official tag: resolve its current producer digest as the app does.
        catalog=await manager.ResolveCurrentReleasesAsync(catalog);
        foreach(var package in catalog.Packages)
        {
            var path=await manager.InstallAsync(package,dir.Path);Assert.True(System.IO.File.Exists(path),package.Id);
            if(package.Id=="retroarch-cores")foreach(var core in PackageManager.CoreIds.Values.Distinct())
            {
                var coreFile=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!,core+"_libretro.dll");Assert.True(System.IO.File.Exists(coreFile),core);
                if(!OperatingSystem.IsWindows())continue;
                var library=NativeLibrary.Load(coreFile);
                try
                {
                    Marshal.GetDelegateForFunctionPointer<GetSystemInfo>(NativeLibrary.GetExport(library,"retro_get_system_info"))(out var info);
                    var extensions=(Marshal.PtrToStringUTF8(info.Extensions)??"").Split('|');
                    foreach(var system in SystemCatalog.RetroSystems.Where(s=>s.Core==core))Assert.True(extensions.Contains(system.Extensions.Split(' ')[0],StringComparer.OrdinalIgnoreCase),$"{system.Name}: core {core} accepts {string.Join('|',extensions)}");
                }
                finally{NativeLibrary.Free(library);}
            }
            else EmulatorSetup.Configure(package.Id,path,System.IO.Path.Combine(dir.Path,"data"),reset:true);
        }
    }
}
