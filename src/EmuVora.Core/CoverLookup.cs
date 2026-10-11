using System.Net;
namespace EmuVora.Core;
public static class CoverLookup
{
    private static readonly Dictionary<string,string> Repositories=new()
    {
        ["NES"]="Nintendo_-_Nintendo_Entertainment_System",["SNES"]="Nintendo_-_Super_Nintendo_Entertainment_System",
        ["GB"]="Nintendo_-_Game_Boy",["GBC"]="Nintendo_-_Game_Boy_Color",["GBA"]="Nintendo_-_Game_Boy_Advance",
        ["N64"]="Nintendo_-_Nintendo_64",["GameCube"]="Nintendo_-_GameCube",["Wii"]="Nintendo_-_Wii",
        ["PS1"]="Sony_-_PlayStation",["PS2"]="Sony_-_PlayStation_2",["PSP"]="Sony_-_PlayStation_Portable",
        ["Mega Drive"]="Sega_-_Mega_Drive_-_Genesis",["Master System"]="Sega_-_Master_System_-_Mark_III",
        ["Nintendo DS"]="Nintendo_-_Nintendo_DS",["Virtual Boy"]="Nintendo_-_Virtual_Boy",
        ["Game Gear"]="Sega_-_Game_Gear",["Mega-CD"]="Sega_-_Mega-CD_-_Sega_CD",["32X"]="Sega_-_32X",["Saturn"]="Sega_-_Saturn",["Dreamcast"]="Sega_-_Dreamcast",
        ["Atari 2600"]="Atari_-_2600",["Atari 5200"]="Atari_-_5200",["Atari 7800"]="Atari_-_7800",["Atari Lynx"]="Atari_-_Lynx",["Atari Jaguar"]="Atari_-_Jaguar",["Atari ST"]="Atari_-_ST",
        ["PC Engine"]="NEC_-_PC_Engine_-_TurboGrafx_16",["PC Engine CD"]="NEC_-_PC_Engine_CD_-_TurboGrafx-CD",["SuperGrafx"]="NEC_-_PC_Engine_SuperGrafx",["PC-FX"]="NEC_-_PC-FX",
        ["Neo Geo"]="SNK_-_Neo_Geo",["Neo Geo CD"]="SNK_-_Neo_Geo_CD",["Neo Geo Pocket"]="SNK_-_Neo_Geo_Pocket",["Neo Geo Pocket Color"]="SNK_-_Neo_Geo_Pocket_Color",
        ["3DO"]="The_3DO_Company_-_3DO",["ColecoVision"]="Coleco_-_ColecoVision",["Intellivision"]="Mattel_-_Intellivision",
        ["WonderSwan"]="Bandai_-_WonderSwan",["WonderSwan Color"]="Bandai_-_WonderSwan_Color",
        ["Commodore 64"]="Commodore_-_64",["Amiga"]="Commodore_-_Amiga",["Amstrad CPC"]="Amstrad_-_CPC",["ZX Spectrum"]="Sinclair_-_ZX_Spectrum",["MSX"]="Microsoft_-_MSX",
        ["DOS"]="DOS",["Arcade (FinalBurn Neo)"]="FBNeo_-_Arcade_Games",["Arcade (MAME)"]="MAME"
    };
    public static Uri? GetUrl(Game game)=>Repositories.TryGetValue(game.System,out var repository)?new Uri($"https://raw.githubusercontent.com/libretro-thumbnails/{repository}/master/Named_Boxarts/{Uri.EscapeDataString(game.Title)}.png"):null;
    public static string? Repository(string system)=>Repositories.GetValueOrDefault(system);
    public static async Task<string?> DownloadAsync(Game game,string folder,CancellationToken token=default)
    {
        var url=GetUrl(game);if(url==null)return null;using var http=new HttpClient {Timeout=TimeSpan.FromSeconds(30)};
        using var response=await http.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,token);if(response.StatusCode==HttpStatusCode.NotFound)return null;response.EnsureSuccessStatusCode();
        if(response.Content.Headers.ContentLength>10_000_000)throw new InvalidDataException("Cover-Datei ist zu groß.");
        Directory.CreateDirectory(folder);var file=Path.Combine(folder,Guid.NewGuid().ToString("N")+".png");
        try
        {
            using var input=await response.Content.ReadAsStreamAsync(token);using var output=File.Create(file);var buffer=new byte[81920];long total=0;int read;
            while((read=await input.ReadAsync(buffer,token))>0){if((total+=read)>10_000_000)throw new InvalidDataException("Cover-Datei ist zu groß.");await output.WriteAsync(buffer.AsMemory(0,read),token);}return file;
        }
        catch{File.Delete(file);throw;}
    }
}
