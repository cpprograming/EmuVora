using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace EmuVora.Core;
public static class Ps2Bios
{
    // Structural check only: PS2 ROM directory entries are 16 bytes; ROMVER contains version, region and date.
    public static bool IsImage(string path)
    {
        try
        {
            using var stream=File.Open(path,FileMode.Open,FileAccess.Read,FileShare.Read);
            if(stream.Length<4*1024*1024||stream.Length>8*1024*1024)return false;
            var data=new byte[(int)stream.Length];stream.ReadExactly(data);
            for(var start=0;start<=data.Length-16;start+=16)
            {
                if(!data.AsSpan(start,10).SequenceEqual("RESET\0\0\0\0\0"u8))continue;
                long offset=0;var directory=false;var version=false;
                for(var entry=start;entry<=Math.Min(data.Length-16,start+16384);entry+=16)
                {
                    var bytes=data.AsSpan(entry,10);var end=bytes.IndexOf((byte)0);if(end==0)break;if(end<0)return false;
                    var name=Encoding.ASCII.GetString(bytes[..end]);var size=BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(entry+12,4));
                    if(offset+size>data.Length)return false;
                    if(name=="ROMDIR")directory=true;
                    if(name=="ROMVER")
                    {
                        if(size<14||offset+14>data.Length)return false;
                        var text=Encoding.ASCII.GetString(data,(int)offset,14);
                        version=text[..4].All(c=>c is >= '0' and <= '9')&&"JAEHCTXP".Contains(text[4])&&DateOnly.TryParseExact(text[6..],"yyyyMMdd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _);
                    }
                    offset+=(size+15L)&~15L;
                }
                return directory&&version;
            }
            return false;
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){return false;}
    }
}
