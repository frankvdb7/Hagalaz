using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Hagalaz.Services.Cache.Features;

internal static class TypeMutationUtilities
{
    internal static MemoryStream BuildSingleChunkArchive(MemoryStream[] entries)
    {
        if (entries.Length == 1)
        {
            return new MemoryStream(entries[0].ToArray());
        }

        using var output = new MemoryStream();
        var cumulative = 0;

        for (var i = 0; i < entries.Length; i++)
        {
            var data = entries[i].ToArray();
            output.Write(data, 0, data.Length);
        }

        for (var i = 0; i < entries.Length; i++)
        {
            cumulative += (int)entries[i].Length;
            WriteInt32BigEndian(output, cumulative);
        }

        output.WriteByte(1); // chunk count
        return new MemoryStream(output.ToArray());
    }

    internal static async Task<MemoryStream> ReadImageRequestStreamAsync(HttpRequest request)
    {
        var stream = new MemoryStream();

        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync(request.HttpContext.RequestAborted);
            var file = form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);
            if (file is not null)
            {
                await file.CopyToAsync(stream, request.HttpContext.RequestAborted);
                stream.Position = 0;
                return stream;
            }
        }

        await request.Body.CopyToAsync(stream, request.HttpContext.RequestAborted);
        stream.Position = 0;
        return stream;
    }

    private static void WriteInt32BigEndian(Stream stream, int value)
    {
        stream.WriteByte((byte)(value >> 24));
        stream.WriteByte((byte)(value >> 16));
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }
}
