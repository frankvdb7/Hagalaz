using System;
using System.IO;
using System.Threading.Tasks;
using Hagalaz.Cache;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Data;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Services.Cache.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Hagalaz.Services.Cache.Features;

public static class TypeMutationEndpoints
{
    public static IEndpointRouteBuilder MapTypeMutationEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapPost("/npcs/{id:int}/combat-level", SetNpcCombatLevel);
        types.MapPost("/objects/{id:int}/name", SetObjectName);
        types.MapPost("/varp-bits/{id:int}", UpdateVarpBitDefinition);
        types.MapPost("/config-definitions/{id:int}", UpdateConfigDefinition);
        types.MapPost("/sprites/{id:int}/image", ReplaceSpriteImage);
        return types;
    }

    private static IResult SetNpcCombatLevel(
        int id,
        SetNpcCombatLevelRequest request,
        ITypeProvider<INpcType> provider,
        INpcTypeCodec codec,
        ICacheAPI cacheApi)
    {
        if (id < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("id must be greater than or equal to 0."));
        }

        try
        {
            var npc = provider.Get(id);
            npc.CombatLevel = request.CombatLevel;

            using var encodedType = codec.Encode(npc);
            PersistTypeEntry(cacheApi, new NpcTypeData(), id, encodedType);

            return TypedResults.Ok(new TypeMutationResponse("npc-combat-level-updated", "npc", id));
        }
        catch (Exception ex)
        {
            return EndpointErrors.MapException(ex);
        }
    }

    private static IResult SetObjectName(
        int id,
        SetObjectNameRequest request,
        ITypeProvider<IObjectType> provider,
        IObjectTypeCodec codec,
        ICacheAPI cacheApi)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("name is required."));
        }

        if (id < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("id must be greater than or equal to 0."));
        }

        try
        {
            var obj = provider.Get(id);
            obj.Name = request.Name;

            using var encodedType = codec.Encode(obj);
            PersistTypeEntry(cacheApi, new ObjectTypeData(), id, encodedType);

            return TypedResults.Ok(new TypeMutationResponse("object-name-updated", "object", id));
        }
        catch (Exception ex)
        {
            return EndpointErrors.MapException(ex);
        }
    }

    private static IResult UpdateVarpBitDefinition(
        int id,
        UpdateVarpBitDefinitionRequest request,
        IVarpBitDefinitionProvider provider,
        ITypeCodec<IVarpBitDefinition> codec,
        ICacheAPI cacheApi)
    {
        if (id < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("id must be greater than or equal to 0."));
        }

        try
        {
            var varpBit = provider.Get(id);
            varpBit.ConfigID = request.ConfigId;
            varpBit.BitLength = request.BitLength;
            varpBit.BitOffset = request.BitOffset;

            using var encodedType = codec.Encode(varpBit);
            PersistTypeEntry(cacheApi, new VarpBitDefinitionData(), id, encodedType);

            return TypedResults.Ok(new TypeMutationResponse("varp-bit-updated", "varp-bit", id));
        }
        catch (Exception ex)
        {
            return EndpointErrors.MapException(ex);
        }
    }

    private static IResult UpdateConfigDefinition(
        int id,
        UpdateConfigDefinitionRequest request,
        IConfigDefinitionProvider provider,
        ITypeCodec<IConfigDefinition> codec,
        ICacheAPI cacheApi)
    {
        if (id < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("id must be greater than or equal to 0."));
        }

        try
        {
            var config = provider.Get(id);
            config.DefaultValue = request.DefaultValue;
            config.ValueType = request.ValueType;

            using var encodedType = codec.Encode(config);
            PersistTypeEntry(cacheApi, new ConfigDefinitionData(), id, encodedType);

            return TypedResults.Ok(new TypeMutationResponse("config-definition-updated", "config-definition", id));
        }
        catch (Exception ex)
        {
            return EndpointErrors.MapException(ex);
        }
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

        try
        {
            await using var input = await TypeMutationUtilities.ReadImageRequestStreamAsync(request);
            if (input.Length == 0)
            {
                return TypedResults.BadRequest(EndpointErrors.Validation("Request body/form file is empty."));
            }

            using var sourceImage = await Image.LoadAsync<Rgba32>(input);
            using var sprite = new SpriteType(id);
            sprite.ReplaceImage(sourceImage);

            using var encodedType = codec.Encode(sprite);
            PersistTypeEntry(cacheApi, new SpriteTypeData(), id, encodedType);

            return TypedResults.Ok(new SpriteMutationResponse(
                Operation: "sprite-image-replaced",
                Type: "sprite",
                Id: id,
                Width: sprite.Image.Width,
                Height: sprite.Image.Height,
                FrameCount: sprite.Image.Frames.Count));
        }
        catch (Exception ex)
        {
            return EndpointErrors.MapException(ex);
        }
    }

    private static void PersistTypeEntry(ICacheAPI cacheApi, ITypeData typeData, int typeId, MemoryStream encodedType)
    {
        var indexId = typeData.IndexId;
        var archiveId = typeData.GetArchiveId(typeId);
        var archiveEntryId = typeData.GetArchiveEntryId(typeId);

        cacheApi.ReplaceArchiveMember(indexId, archiveId, archiveEntryId, encodedType.ToArray());
    }

    public sealed record SetNpcCombatLevelRequest(int CombatLevel);

    public sealed record SetObjectNameRequest(string Name);

    public sealed record UpdateVarpBitDefinitionRequest(short ConfigId, byte BitLength, byte BitOffset);

    public sealed record UpdateConfigDefinitionRequest(int DefaultValue, char ValueType);

    public sealed record TypeMutationResponse(string Operation, string Type, int Id);

    public sealed record SpriteMutationResponse(string Operation, string Type, int Id, int Width, int Height, int FrameCount);
}
