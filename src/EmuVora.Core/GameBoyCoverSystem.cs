using System.IO.Compression;

namespace EmuVora.Core;

// Cover-catalog selection only; never changes the saved platform or ROM.
// Header layout: https://gbdev.io/pandocs/The_Cartridge_Header.html
public static class GameBoyCoverSystem
{
    private static readonly byte[] Logo=Convert.FromHexString("CEED6666CC0D000B03730083000C000D0008111F8889000EDCCC6EE6DDDDD999BBBB67636E0EECCCDDDC999FBBB9333E");
    public static async Task<string?> DetectAsync(string path,CancellationToken token=default)
    {
        var extension=Path.GetExtension(path);
        if(!extension.Equals(".gb",StringComparison.OrdinalIgnoreCase)&&!extension.Equals(".gbc",StringComparison.OrdinalIgnoreCase)&&!extension.Equals(".zip",StringComparison.OrdinalIgnoreCase))return null;
        try
        {
            await using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,4096,true);
            if(!extension.Equals(".zip",StringComparison.OrdinalIgnoreCase))return await ReadAsync(file,token);
            using var zip=new ZipArchive(file,ZipArchiveMode.Read,true);if(zip.Entries.Count>10000)return null;
            var entries=zip.Entries.Where(e=>Path.GetExtension(e.FullName).Equals(".gb",StringComparison.OrdinalIgnoreCase)||Path.GetExtension(e.FullName).Equals(".gbc",StringComparison.OrdinalIgnoreCase)).ToArray();
            if(entries.Length!=1||entries[0].Length<0x150||entries[0].Length>64L*1024*1024)return null;
            using var rom=entries[0].Open();return await ReadAsync(rom,token);
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidDataException){return null;}
    }
    private static async Task<string?> ReadAsync(Stream stream,CancellationToken token)
    {
        var header=new byte[0x150];
        if(await stream.ReadAtLeastAsync(header,header.Length,false,token)!=header.Length||!header.AsSpan(0x104,48).SequenceEqual(Logo))return null;
        byte checksum=0;for(var i=0x134;i<=0x14C;i++)checksum=unchecked((byte)(checksum-header[i]-1));
        if(checksum!=header[0x14D])return null;
        return (header[0x143]&0x80)!=0?"GBC":"GB";
    }
}
