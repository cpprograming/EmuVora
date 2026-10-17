using System.Security.Cryptography;
using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;

public class FirmwareImportTests
{
    [Fact] public void RecognitionUsesContentAndNeverGuessesBySizeOrName()
    {
        using var dir=new TestDirectory();var file=dir.File("wrong-name.rom","synthetic firmware fixture");
        var hash=Convert.ToHexString(SHA1.HashData(File.ReadAllBytes(file)));
        var signatures=new[]{new FirmwareRecognition.Signature("dc/dc_boot.bin",new FileInfo(file).Length,hash)};
        Assert.Equal("dc/dc_boot.bin",FirmwareRecognition.Identify(file,signatures));
        File.WriteAllText(file,"synthetic firmware fixturE");Assert.Null(FirmwareRecognition.Identify(file,signatures));
        Assert.Null(FirmwareRecognition.Identify(file));
    }
    [Fact] public void UnknownFilesKeepTheirNamesAndExistingFilesAreBackedUp()
    {
        using var dir=new TestDirectory();var file=dir.File("source/own-firmware.bin","first");var data=Path.Combine(dir.Path,"data");
        var target=EmulatorSetup.ImportFirmware(file,data);Assert.Equal("own-firmware.bin",Path.GetFileName(target));
        File.WriteAllText(file,"second");EmulatorSetup.ImportFirmware(file,data);Assert.Equal("first",File.ReadAllText(target+".bak"));Assert.Equal("second",File.ReadAllText(target));Assert.Equal("second",File.ReadAllText(file));
    }
    [Fact] public void AmbiguousOrUnsafeSignaturesAreNotUsed()
    {
        using var dir=new TestDirectory();var file=dir.File("sample","fixture");var hash=Convert.ToHexString(SHA1.HashData(File.ReadAllBytes(file)));var size=new FileInfo(file).Length;
        Assert.Null(FirmwareRecognition.Identify(file,[new("first.bin",size,hash),new("second.bin",size,hash)]));
        Assert.Throws<InvalidDataException>(()=>FirmwareRecognition.Identify(file,[new("../outside.bin",size,hash)]));
    }
}
