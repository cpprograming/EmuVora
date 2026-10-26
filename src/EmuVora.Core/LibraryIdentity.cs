using System.Security.Cryptography;
using System.Text;

namespace EmuVora.Core;

public static class LibraryIdentity
{
    // Retain the original path-derived key so existing cover filenames remain valid.
    public static string GameId(Game game) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(Path.GetFullPath(game.Path).ToUpperInvariant()))).ToLowerInvariant();
}

public static class SystemArtworkIds
{
    public static string SystemId(string system) => system switch
    {
        "Atari Lynx" => "atarilynx", "Atari Jaguar" => "atarijaguar", "WonderSwan" => "wonderswan",
        "WonderSwan Color" => "wonderswancolor", "32X" => "sega32x",
        "Arcade (FinalBurn Neo)" => "fbneo", "Arcade (MAME)" => "mame",
        _ => EmulationStationTheme.SystemId(system)
    };
    public static string ThemeId(string system) => system is "Arcade (FinalBurn Neo)" or "Arcade (MAME)" ? "arcade" : SystemId(system);
}
