using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Services.Cache.Features.Shared;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static Hagalaz.Services.Cache.Features.Types.TypeQueryUtilities;
using static Hagalaz.Services.Cache.Features.Types.TypeMutationUtilities;

namespace Hagalaz.Services.Cache.Features.Types;

public static class SpriteDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapSpriteDefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/sprites/{id:int}", GetSpriteType);
        types.MapGet("/sprites/{id:int}/image", GetSpriteTypeImage);
        types.MapGet("/sprites/range", GetSpriteTypeRange);
        types.MapGet("/sprites/search", SearchSpriteTypes);
        types.MapPost("/sprites/{id:int}/image", ReplaceSpriteImage);
        return types;
    }

    private static IResult GetSpriteType(int id, ITypeProvider<ISpriteType> provider)
    {
        using var sprite = provider.Get(id);
        return TypedResults.Ok(MapSprite(sprite));
    }

    private static IResult GetSpriteTypeImage(int id, int? frame, ITypeProvider<ISpriteType> provider)
    {
        using var sprite = provider.Get(id);
        var frameIndex = frame ?? 0;
        if (frameIndex < 0 || frameIndex >= sprite.Image.Frames.Count)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation($"frame must be between 0 and {sprite.Image.Frames.Count - 1}."));
        }

        using var image = RenderSpriteFrame(sprite.Image, frameIndex);
        using var buffer = new MemoryStream();
        image.SaveAsPng(buffer);
        return TypedResults.File(buffer.ToArray(), "image/png", $"sprite-{id}-frame-{frameIndex}.png");
    }

    private static IResult GetSpriteTypeRange(int startId, int endIdExclusive, ITypeProvider<ISpriteType> provider)
    {
        var error = ValidateRange(startId, endIdExclusive, provider.ArchiveSize);
        return error ?? TypedResults.Ok(MapSpritesAndDispose(provider.GetRange(startId, endIdExclusive)));
    }

    private static IResult SearchSpriteTypes(string? query, int offset, int limit, ITypeProvider<ISpriteType> provider, ICacheAPI cache)
    {
        var error = ValidateSearch(query, offset, limit, provider.ArchiveSize, requireQuery: false);
        if (error is not null) return error;

        var matchingIds = cache.ReadReferenceTable(new SpriteTypeData().IndexId).Entries
            .Select(entry => entry.Key)
            .Where(id => string.IsNullOrWhiteSpace(query)
                || id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var pageIds = matchingIds.Skip(offset).Take(limit).ToArray();
        var page = new GetSpriteDefinitionDto[pageIds.Length];
        for (var i = 0; i < pageIds.Length; i++)
        {
            using var sprite = provider.Get(pageIds[i]);
            page[i] = MapSprite(sprite);
        }

        return TypedResults.Ok(new SearchDefinitionsDto<GetSpriteDefinitionDto>(query ?? string.Empty, matchingIds.Length, offset, limit, page));
    }

    private static GetSpriteDefinitionDto MapSprite(ISpriteType sprite) =>
        new(sprite.Id, sprite.Image.Width, sprite.Image.Height, sprite.Image.Frames.Count);

    private static GetSpriteDefinitionDto[] MapSpritesAndDispose(ISpriteType[] sprites)
    {
        try
        {
            return sprites.Select(MapSprite).ToArray();
        }
        finally
        {
            foreach (var sprite in sprites)
            {
                sprite.Dispose();
            }
        }
    }

    private static Image<Rgba32> RenderSpriteFrame(Image<Rgba32> source, int frameIndex)
    {
        var target = new Image<Rgba32>(source.Width, source.Height);
        var frame = source.Frames[frameIndex];

        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                target[x, y] = frame[x, y];
            }
        }

        return target;
    }

    private static async Task<IResult> ReplaceSpriteImage(
        int id,
        HttpRequest request,
        ISpriteTypeCodec codec,
        ICacheAPI cacheApi)
    {
        if (id < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("id must be greater than or equal to 0."));
        }

        await using var input = await TypeMutationUtilities.ReadImageRequestStreamAsync(request);
        if (input.Length == 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("Request body/form file is empty."));
        }

        using var sourceImage = await Image.LoadAsync<Rgba32>(input);
        using var sprite = new SpriteType(id);
        sprite.ReplaceImage(sourceImage);

        using var encodedType = codec.Encode(sprite);
        PersistTypeEntry(cacheApi, new SpriteTypeData(), id, encodedType.ToArray());

        return TypedResults.Ok(new ReplaceSpriteImageResultDto(
            Operation: "sprite-image-replaced",
            Type: "sprite",
            Id: id,
            Width: sprite.Image.Width,
            Height: sprite.Image.Height,
            FrameCount: sprite.Image.Frames.Count));
    }
}
