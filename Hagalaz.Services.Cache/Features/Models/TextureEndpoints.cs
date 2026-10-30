using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Services.Cache.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SixLabors.ImageSharp;

namespace Hagalaz.Services.Cache.Features;

public static class TextureEndpoints
{
    public static IEndpointRouteBuilder MapTextureEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/textures/{id:int}/image", GetTextureImage);
        return group;
    }

    private static IResult GetTextureImage(int id, ICacheAPI cache, HttpContext context)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Texture ID must be nonnegative."));
        using var container = cache.ReadContainer(9, id);
        try
        {
            using var materials = cache.Read(26, 0, 0);
            var material = MaterialDefinitionDecoder.Decode(materials.ToArray(), id);
            var alphaMode = material.AlphaMode;
            var png = TextureImageDecoder.ToPng(container.Data.ToArray(), alphaMode);
            context.Response.Headers["X-Texture-Alpha-Mode"] = alphaMode.ToString(System.Globalization.CultureInfo.InvariantCulture);
            context.Response.Headers["X-Texture-Color-Blend"] = material.ColorBlend.ToString(System.Globalization.CultureInfo.InvariantCulture);
            context.Response.Headers["X-Texture-Brightness"] = material.Brightness.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return TypedResults.File(png, "image/png");
        }
        catch (UnknownImageFormatException)
        {
            return TypedResults.Problem(title: "Unsupported texture image", detail: "The cache texture is not a supported image format.", statusCode: 422);
        }
        catch (System.NotSupportedException exception)
        {
            return TypedResults.Problem(title: "Unsupported texture material", detail: exception.Message, statusCode: 422);
        }
    }
}
