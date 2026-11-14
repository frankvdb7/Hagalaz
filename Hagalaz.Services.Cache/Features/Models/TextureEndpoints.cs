using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic.Rendering;
using Hagalaz.Cache.Types;
using Hagalaz.Services.Cache.Features.Shared;
using Hagalaz.Services.Cache.Features.Models.Dtos;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Hagalaz.Services.Cache.Features.Models;

public static class TextureEndpoints
{
    internal const string ImageCacheTag = "texture-images";

    /// <summary>Texture images are derived from the texture (9) and material (26) indexes.</summary>
    internal static bool IsImageSource(int indexId) => indexId is 9 or 26;

    public static IEndpointRouteBuilder MapTextureEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/textures/{id:int}/image", GetTextureImage).WithSummary("Render a texture as PNG with its material in headers").Produces<System.IO.Stream>(StatusCodes.Status200OK, "image/png").Produces(StatusCodes.Status304NotModified)
            .CacheOutput(new TextureOutputCachePolicy());
        group.MapGet("/textures/{id:int}/material", GetMaterial).WithSummary("Get a texture's material definition").Produces<GetTextureMaterialDto>();
        group.MapGet("/textures/{id:int}/hdr", GetHdr).WithSummary("Decode a texture's HDR companion image").Produces<GetTextureHdrDto>();
        return group;
    }

    private sealed class TextureOutputCachePolicy : IOutputCachePolicy
    {
        public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
        {
            var request = context.HttpContext.Request;
            // The cache group authorizes administrators before output caching runs.
            // Conditional requests still check the live image and material after edits.
            var cache = HttpMethods.IsGet(request.Method) && !request.Headers.ContainsKey(HeaderNames.IfNoneMatch);
            context.CacheVaryByRules.QueryKeys = "sourceAlpha";
            context.EnableOutputCaching = cache;
            context.AllowCacheLookup = cache;
            context.AllowCacheStorage = cache;
            context.AllowLocking = true;
            context.ResponseExpirationTimeSpan = System.TimeSpan.FromSeconds(15);
            context.Tags.Add(ImageCacheTag);
            return ValueTask.CompletedTask;
        }

        public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
        {
            var response = context.HttpContext.Response;
            context.AllowCacheStorage &= response.StatusCode == StatusCodes.Status200OK &&
                !response.Headers.ContainsKey(HeaderNames.SetCookie);
            return ValueTask.CompletedTask;
        }
    }

    private static IResult GetHdr(int id, ICacheAPI cache, ITypeCodec<IMaterialTableType> materialCodec)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Texture ID must be nonnegative."));
        using var materials = cache.Read(26, 0, 0);
        var material = MaterialTableType.Require(materialCodec.Decode(0, materials), id);
        if (!material.HasHdr) return TypedResults.NotFound();
        using var image = cache.ReadContainer(9, id);
        using var companion = cache.ReadContainer(37, id);
        var hdr = TextureImageConverter.ToHdr(image.Data.ToArray(), companion.Data.ToArray(), RenderAlphaMode(material, sourceAlpha: true));
        return TypedResults.Ok(new GetTextureHdrDto(hdr.Width, hdr.Height, hdr.Rgba));
    }

    private static IResult GetMaterial(int id, ICacheAPI cache, ITypeCodec<IMaterialTableType> materialCodec)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Texture ID must be nonnegative."));
        using var materials = cache.Read(26, 0, 0);
        var material = MaterialTableType.Require(materialCodec.Decode(0, materials), id);
        return TypedResults.Ok(new GetTextureMaterialDto(material.AlphaMode, material.ColorBlend, material.Brightness,
            material.EffectId, material.EffectParameter, material.ScrollU, material.ScrollV, material.RepeatU,
            material.RepeatV, material.HasHdr, material.EffectParameter2));
    }

    // The client keeps the alpha of effect 1 and 7 textures (Class73) and uses it to mask their highlight or reflection, so an opaque material's alpha is still data.
    private static int RenderAlphaMode(MaterialEntry material, bool sourceAlpha) =>
        sourceAlpha && material.AlphaMode == 0 && material.EffectId is 1 or 7 ? 2 : material.AlphaMode;

    private static IResult GetTextureImage(int id, ICacheAPI cache, ITypeCodec<IMaterialTableType> materialCodec, HttpContext context, bool sourceAlpha = false)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Texture ID must be nonnegative."));
        using var container = cache.ReadContainer(9, id);
        using var materials = cache.Read(26, 0, 0);
        var material = MaterialTableType.Require(materialCodec.Decode(0, materials), id);
        var alphaMode = material.AlphaMode;
        var source = container.Data.ToArray();
        var metadata = JsonSerializer.Serialize(
            new GetTextureMaterialDto(material.AlphaMode, material.ColorBlend, material.Brightness,
                material.EffectId, material.EffectParameter, material.ScrollU, material.ScrollV, material.RepeatU,
                material.RepeatV, material.HasHdr, material.EffectParameter2), JsonSerializerOptions.Web);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(source);
        hash.AppendData(Encoding.UTF8.GetBytes(metadata));
        var pngAlphaMode = RenderAlphaMode(material, sourceAlpha);
        var etag = new EntityTagHeaderValue($"\"texture-png-v1{(pngAlphaMode == alphaMode ? "" : "-source-alpha")}-{System.Convert.ToHexStringLower(hash.GetHashAndReset())}\"", isWeak: true);
        context.Response.Headers["X-Texture-Alpha-Mode"] = alphaMode.ToString(System.Globalization.CultureInfo.InvariantCulture);
        context.Response.Headers["X-Texture-Color-Blend"] = material.ColorBlend.ToString(System.Globalization.CultureInfo.InvariantCulture);
        context.Response.Headers["X-Texture-Brightness"] = material.Brightness.ToString(System.Globalization.CultureInfo.InvariantCulture);
        context.Response.Headers["X-Texture-Material"] = metadata;
        // Revalidate private browser copies, but avoid image conversion and transfer when inputs match.
        var validators = context.Request.GetTypedHeaders().IfNoneMatch;
        if (validators?.Any(candidate => candidate.Equals(EntityTagHeaderValue.Any) || candidate.Compare(etag, useStrongComparison: false)) == true)
        {
            context.Response.Headers.CacheControl = "private, no-cache";
            context.Response.GetTypedHeaders().ETag = etag;
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }
        var png = TextureImageConverter.ToPng(source, pngAlphaMode);
        context.Response.Headers.CacheControl = "private, no-cache";
        context.Response.GetTypedHeaders().ETag = etag;
        return TypedResults.File(png, "image/png");
    }
}
