using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Hagalaz.Services.Cache.Features;

internal static class TypeMutationUtilities
{
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
}
