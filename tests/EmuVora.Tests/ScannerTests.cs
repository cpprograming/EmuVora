using EmuVora.Core;
using Xunit;
namespace EmuVora.Tests;
public class ScannerTests
{
    [Fact] public void DetectsSnesAndNesAndSkipsUnrelatedFiles()
    {
        var dir=Path.Combine(Path.GetTempPath(),"emuvora-test-"+Guid.NewGuid()); Directory.CreateDirectory(dir);
        try {File.WriteAllText(Path.Combine(dir,"Mario.sfc"),"");File.WriteAllText(Path.Combine(dir,"Zelda.nes"),"");File.WriteAllText(Path.Combine(dir,"readme.txt"),"");
            var games=RomScanner.Scan(dir); Assert.Equal(2,games.Count);Assert.Contains(games,g=>g.System=="SNES");Assert.Contains(games,g=>g.System=="NES");}
        finally {Directory.Delete(dir,true);}
    }
    [Fact] public void DiscWithoutSystemFolderIsAmbiguous()
    {
        var dir=Path.Combine(Path.GetTempPath(),"emuvora-test-"+Guid.NewGuid());Directory.CreateDirectory(dir);
        try {File.WriteAllText(Path.Combine(dir,"game.iso"),"");Assert.Equal("Unknown disc",RomScanner.Scan(dir).Single().System);}
        finally {Directory.Delete(dir,true);}
    }
}
