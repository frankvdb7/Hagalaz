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
using Hagalaz.Cache.Models;
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
        => MutateType(id, provider, codec, new NpcTypeData(), cacheApi, npc =>
        {
            npc.CombatLevel = request.CombatLevel;
            return new TypeMutationResponse("npc-combat-level-updated", "npc", id);
        });

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

        return MutateType(id, provider, codec, new ObjectTypeData(), cacheApi, obj =>
        {
            obj.Name = request.Name;
            return new TypeMutationResponse("object-name-updated", "object", id);
        });
    }

    private static IResult UpdateVarpBitDefinition(
        int id,
        UpdateVarpBitDefinitionRequest request,
        IVarpBitDefinitionProvider provider,
        ITypeCodec<IVarpBitDefinition> codec,
        ICacheAPI cacheApi)
        => MutateType(id, provider, codec, new VarpBitDefinitionData(), cacheApi, varpBit =>
        {
            varpBit.ConfigID = request.ConfigId;
            varpBit.BitLength = request.BitLength;
            varpBit.BitOffset = request.BitOffset;
            return new TypeMutationResponse("varp-bit-updated", "varp-bit", id);
        });

    private static IResult UpdateConfigDefinition(
        int id,
        UpdateConfigDefinitionRequest request,
        IConfigDefinitionProvider provider,
        ITypeCodec<IConfigDefinition> codec,
        ICacheAPI cacheApi)
        => MutateType(id, provider, codec, new ConfigDefinitionData(), cacheApi, config =>
        {
            config.DefaultValue = request.DefaultValue;
            config.ValueType = request.ValueType;
            return new TypeMutationResponse("config-definition-updated", "config-definition", id);
        });

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
            var sprite = new SpriteType(id);

            while (sprite.Image.Frames.Count > 0)
            {
                sprite.Image.Frames.RemoveFrame(0);
            }

            for (var frameIndex = 0; frameIndex < sourceImage.Frames.Count; frameIndex++)
            {
                sprite.InsertFrame(frameIndex, sourceImage.Frames[frameIndex]);
            }

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

    private static IResult MutateType<TType>(
        int typeId,
        ITypeProvider<TType> provider,
        ITypeCodec<TType> codec,
        ITypeData typeData,
        ICacheAPI cacheApi,
        Func<TType, TypeMutationResponse> mutate)
        where TType : IType
    {
        if (typeId < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("id must be greater than or equal to 0."));
        }

        try
        {
            var type = provider.Get(typeId);
            var result = mutate(type);

            var encodedType = codec.Encode(type);
            PersistTypeEntry(cacheApi, typeData, typeId, encodedType);

            return TypedResults.Ok(result);
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

        var table = cacheApi.ReadReferenceTable(indexId);
        var childEntry = table.GetEntry(archiveId, archiveEntryId);
        if (childEntry is null)
        {
            throw new FileNotFoundException($"Unable to locate archive entry for type {typeId}.");
        }

        using var container = cacheApi.ReadContainer(indexId, archiveId);
        using var archive = cacheApi.ReadArchive(indexId, archiveId);

        var entries = archive.Entries ?? throw new InvalidOperationException("Archive entries are not available.");
        entries[childEntry.Index] = new MemoryStream(encodedType.ToArray());

        using var rebuiltArchive = TypeMutationUtilities.BuildSingleChunkArchive(entries);
        using var writeContainer = new Container(container.CompressionType, rebuiltArchive, container.Version);
        cacheApi.Write(indexId, archiveId, writeContainer);
    }

    public sealed record SetNpcCombatLevelRequest(int CombatLevel);

    public sealed record SetObjectNameRequest(string Name);

    public sealed record UpdateVarpBitDefinitionRequest(short ConfigId, byte BitLength, byte BitOffset);

    public sealed record UpdateConfigDefinitionRequest(int DefaultValue, char ValueType);

    public sealed record TypeMutationResponse(string Operation, string Type, int Id);

    public sealed record SpriteMutationResponse(string Operation, string Type, int Id, int Width, int Height, int FrameCount);
}
