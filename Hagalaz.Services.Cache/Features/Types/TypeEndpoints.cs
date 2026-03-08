using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Services.Cache.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Hagalaz.Services.Cache.Features;

public static class TypeEndpoints
{
    public static IEndpointRouteBuilder MapTypeEndpoints(this IEndpointRouteBuilder group)
    {
        var types = group.MapGroup("/types");

        types.MapGet("/archive-sizes", GetArchiveSizes);

        types.MapGet("/items/{id:int}", GetItemType);
        types.MapGet("/items/range", GetItemTypeRange);
        types.MapGet("/items/search", SearchItemTypes);

        types.MapGet("/npcs/{id:int}", GetNpcType);
        types.MapGet("/npcs/range", GetNpcTypeRange);
        types.MapGet("/npcs/search", SearchNpcTypes);

        types.MapGet("/objects/{id:int}", GetObjectType);
        types.MapGet("/objects/range", GetObjectTypeRange);
        types.MapGet("/objects/search", SearchObjectTypes);

        types.MapGet("/sprites/{id:int}", GetSpriteType);
        types.MapGet("/sprites/{id:int}/image", GetSpriteTypeImage);
        types.MapGet("/sprites/range", GetSpriteTypeRange);
        types.MapGet("/sprites/search", SearchSpriteTypes);

        types.MapGet("/quests/{id:int}", GetQuestType);
        types.MapGet("/quests/range", GetQuestTypeRange);

        types.MapGet("/animations/{id:int}", GetAnimationType);
        types.MapGet("/animations/range", GetAnimationTypeRange);
        types.MapGet("/animations/search", SearchAnimationTypes);

        types.MapGet("/graphics/{id:int}", GetGraphicType);
        types.MapGet("/graphics/range", GetGraphicTypeRange);
        types.MapGet("/graphics/search", SearchGraphicTypes);

        types.MapGet("/maps/{id:int}", GetMapType);
        types.MapGet("/maps/range", GetMapTypeRange);
        types.MapGet("/maps/search", SearchMapTypes);
        types.MapGet("/maps/{id:int}/terrain", GetMapTerrain);
        types.MapPost("/maps/{id:int}/decode", DecodeMapType);

        types.MapGet("/varp-bits/{id:int}", GetVarpBitType);
        types.MapGet("/varp-bits/range", GetVarpBitTypeRange);

        types.MapGet("/client-map-definitions/{id:int}", GetClientMapDefinitionType);
        types.MapGet("/client-map-definitions/range", GetClientMapDefinitionTypeRange);

        types.MapGet("/config-definitions/{id:int}", GetConfigDefinitionType);
        types.MapGet("/config-definitions/range", GetConfigDefinitionTypeRange);

        types.MapGet("/cs2/{id:int}", GetCs2Type);
        types.MapGet("/cs2/range", GetCs2TypeRange);
        types.MapGet("/cs2-int/{id:int}", GetCs2IntType);

        types.MapGet("/equipment-defaults", GetEquipmentDefaults);
        types.MapGet("/huffman/coding", GetHuffmanCoding);
        types.MapTypeMutationEndpoints();

        return group;
    }

    private static IResult GetArchiveSizes(
        ITypeProvider<IItemType> items,
        ITypeProvider<INpcType> npcs,
        ITypeProvider<IObjectType> objects,
        ITypeProvider<ISpriteType> sprites,
        ITypeProvider<IQuestType> quests,
        ITypeProvider<IAnimationType> animations,
        ITypeProvider<IGraphicType> graphics,
        ITypeProvider<ICs2Definition> cs2,
        IClientMapDefinitionProvider clientMaps,
        IConfigDefinitionProvider configs,
        IVarpBitDefinitionProvider varpBits,
        IMapProvider maps)
    {
        return TypedResults.Ok(new ArchiveSizesResponse(
            items.ArchiveSize,
            npcs.ArchiveSize,
            objects.ArchiveSize,
            sprites.ArchiveSize,
            quests.ArchiveSize,
            animations.ArchiveSize,
            graphics.ArchiveSize,
            cs2.ArchiveSize,
            clientMaps.ArchiveSize,
            configs.ArchiveSize,
            varpBits.ArchiveSize,
            maps.ArchiveSize));
    }

    private static IResult GetItemType(int id, ITypeProvider<IItemType> provider) => Resolve(() => TypedResults.Ok(MapItem(provider.Get(id))));

    private static IResult GetItemTypeRange(int startId, int endIdExclusive, ITypeProvider<IItemType> provider)
        => ResolveRange(startId, endIdExclusive, provider.ArchiveSize, () => provider.GetRange(startId, endIdExclusive).Select(MapItem).ToArray());

    private static IResult SearchItemTypes(string query, int offset, int limit, ITypeProvider<IItemType> provider)
        => ResolveSearch(query, offset, limit, provider.ArchiveSize, () =>
        {
            var matches = provider.GetRange(0, provider.ArchiveSize)
                .Where(type => !string.IsNullOrWhiteSpace(type.Name) && type.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(MapItem)
                .ToArray();
            return matches;
        });

    private static IResult GetNpcType(int id, ITypeProvider<INpcType> provider) => Resolve(() => TypedResults.Ok(MapNpc(provider.Get(id))));

    private static IResult GetNpcTypeRange(int startId, int endIdExclusive, ITypeProvider<INpcType> provider)
        => ResolveRange(startId, endIdExclusive, provider.ArchiveSize, () => provider.GetRange(startId, endIdExclusive).Select(MapNpc).ToArray());

    private static IResult SearchNpcTypes(string query, int offset, int limit, ITypeProvider<INpcType> provider)
        => ResolveSearch(query, offset, limit, provider.ArchiveSize, () =>
        {
            var matches = provider.GetRange(0, provider.ArchiveSize)
                .Where(type => !string.IsNullOrWhiteSpace(type.Name) && type.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(MapNpc)
                .ToArray();
            return matches;
        });

    private static IResult GetObjectType(int id, ITypeProvider<IObjectType> provider) => Resolve(() => TypedResults.Ok(MapObject(provider.Get(id))));

    private static IResult GetObjectTypeRange(int startId, int endIdExclusive, ITypeProvider<IObjectType> provider)
        => ResolveRange(startId, endIdExclusive, provider.ArchiveSize, () => provider.GetRange(startId, endIdExclusive).Select(MapObject).ToArray());

    private static IResult SearchObjectTypes(string query, int offset, int limit, ITypeProvider<IObjectType> provider)
        => ResolveSearch(query, offset, limit, provider.ArchiveSize, () =>
        {
            var matches = provider.GetRange(0, provider.ArchiveSize)
                .Where(type => !string.IsNullOrWhiteSpace(type.Name) && type.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(MapObject)
                .ToArray();
            return matches;
        });

    private static IResult GetSpriteType(int id, ITypeProvider<ISpriteType> provider) => Resolve(() => TypedResults.Ok(MapSprite(provider.Get(id))));

    private static IResult GetSpriteTypeImage(int id, int? frame, ITypeProvider<ISpriteType> provider)
        => Resolve(() =>
        {
            var sprite = provider.Get(id);
            var frameIndex = frame ?? 0;
            if (frameIndex < 0 || frameIndex >= sprite.Image.Frames.Count)
            {
                return TypedResults.BadRequest(EndpointErrors.Validation($"frame must be between 0 and {sprite.Image.Frames.Count - 1}."));
            }

            using var image = RenderSpriteFrame(sprite.Image, frameIndex);
            using var buffer = new MemoryStream();
            image.SaveAsPng(buffer);
            return TypedResults.File(buffer.ToArray(), "image/png", $"sprite-{id}-frame-{frameIndex}.png");
        });

    private static IResult GetSpriteTypeRange(int startId, int endIdExclusive, ITypeProvider<ISpriteType> provider)
        => ResolveRange(startId, endIdExclusive, provider.ArchiveSize, () => provider.GetRange(startId, endIdExclusive).Select(MapSprite).ToArray());

    private static IResult SearchSpriteTypes(string? query, int offset, int limit, ITypeProvider<ISpriteType> provider)
        => ResolveIndexSearch(query, offset, limit, provider.ArchiveSize, () =>
        {
            var all = provider.GetRange(0, provider.ArchiveSize).Select(MapSprite);
            if (string.IsNullOrWhiteSpace(query))
            {
                return all.ToArray();
            }

            return all.Where(type => type.Id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        });

    private static IResult GetQuestType(int id, ITypeProvider<IQuestType> provider) => Resolve(() => TypedResults.Ok(new QuestTypeResponse(provider.Get(id).Id)));

    private static IResult GetQuestTypeRange(int startId, int endIdExclusive, ITypeProvider<IQuestType> provider)
        => ResolveRange(startId, endIdExclusive, provider.ArchiveSize, () => provider.GetRange(startId, endIdExclusive).Select(q => new QuestTypeResponse(q.Id)).ToArray());

    private static IResult GetAnimationType(int id, ITypeProvider<IAnimationType> provider)
        => Resolve(() =>
        {
            var animation = provider.Get(id);
            return TypedResults.Ok(new AnimationTypeResponse(animation.Id, animation.Priority));
        });

    private static IResult GetAnimationTypeRange(int startId, int endIdExclusive, ITypeProvider<IAnimationType> provider)
        => ResolveRange(startId, endIdExclusive, provider.ArchiveSize, () => provider.GetRange(startId, endIdExclusive).Select(a => new AnimationTypeResponse(a.Id, a.Priority)).ToArray());

    private static IResult SearchAnimationTypes(string? query, int offset, int limit, ITypeProvider<IAnimationType> provider)
        => ResolveIndexSearch(query, offset, limit, provider.ArchiveSize, () =>
        {
            var all = provider.GetRange(0, provider.ArchiveSize).Select(a => new AnimationTypeResponse(a.Id, a.Priority));
            if (string.IsNullOrWhiteSpace(query))
            {
                return all.ToArray();
            }

            return all.Where(type => type.Id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        });

    private static IResult GetGraphicType(int id, ITypeProvider<IGraphicType> provider) => Resolve(() => TypedResults.Ok(MapGraphic(provider.Get(id))));

    private static IResult GetGraphicTypeRange(int startId, int endIdExclusive, ITypeProvider<IGraphicType> provider)
        => ResolveRange(startId, endIdExclusive, provider.ArchiveSize, () => provider.GetRange(startId, endIdExclusive).Select(MapGraphic).ToArray());

    private static IResult SearchGraphicTypes(string? query, int offset, int limit, ITypeProvider<IGraphicType> provider)
        => ResolveIndexSearch(query, offset, limit, provider.ArchiveSize, () =>
        {
            var all = provider.GetRange(0, provider.ArchiveSize).Select(MapGraphic);
            if (string.IsNullOrWhiteSpace(query))
            {
                return all.ToArray();
            }

            return all.Where(type => type.Id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        });

    private static IResult GetMapType(int id, IMapProvider provider) => Resolve(() => TypedResults.Ok(MapMap(provider.Get(id))));

    private static IResult GetMapTypeRange(int startId, int endIdExclusive, IMapProvider provider)
        => ResolveRange(startId, endIdExclusive, provider.ArchiveSize, () => provider.GetRange(startId, endIdExclusive).Select(MapMap).ToArray());

    private static IResult SearchMapTypes(string? query, int offset, int limit, IMapProvider provider)
        => ResolveIndexSearch(query, offset, limit, provider.ArchiveSize, () =>
        {
            var all = provider.GetRange(0, provider.ArchiveSize).Select(MapMap);
            if (string.IsNullOrWhiteSpace(query))
            {
                return all.ToArray();
            }

            return all.Where(type => type.Id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        });

    private static IResult GetMapTerrain(int id, IMapProvider provider)
        => Resolve(() =>
        {
            var map = provider.Get(id);
            
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            // 1. Write metadata (Planes, Width, Height)
            writer.Write(map.TerrainData.GetLength(0)); // Planes
            writer.Write(map.TerrainData.GetLength(1)); // Width
            writer.Write(map.TerrainData.GetLength(2)); // Height

            // 2. Write Terrain Flags (sbytes)
            var terrainData = new sbyte[map.TerrainData.Length];
            Buffer.BlockCopy(map.TerrainData, 0, terrainData, 0, map.TerrainData.Length);
            foreach (var b in terrainData) writer.Write((byte)b);
            
            // 3. Write Heights (shorts)
            var heights = new short[map.Heights.Length];
            Buffer.BlockCopy(map.Heights, 0, heights, 0, map.Heights.Length * sizeof(short));
            foreach (var h in heights) writer.Write(h);

            return TypedResults.File(ms.ToArray(), "application/octet-stream", $"map-{id}-terrain.dat");
        });

    private static IResult DecodeMapType(int id, MapDecodeRequest request, IMapProvider provider)
    {
        if (request.XteaKeys is null || request.XteaKeys.Length != 4)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("xteaKeys must contain exactly 4 integers."));
        }

        return Resolve(() => TypedResults.Ok(MapMap(provider.Get(id, request.XteaKeys))));
    }

    private static IResult GetVarpBitType(int id, IVarpBitDefinitionProvider provider) => Resolve(() => TypedResults.Ok(MapVarpBit(provider.Get(id))));

    private static IResult GetVarpBitTypeRange(int startId, int endIdExclusive, IVarpBitDefinitionProvider provider)
        => ResolveRange(startId, endIdExclusive, provider.ArchiveSize, () => provider.GetRange(startId, endIdExclusive).Select(MapVarpBit).ToArray());

    private static IResult GetClientMapDefinitionType(int id, IClientMapDefinitionProvider provider)
        => Resolve(() => TypedResults.Ok(MapClientMapDefinition(provider.Get(id))));

    private static IResult GetClientMapDefinitionTypeRange(int startId, int endIdExclusive, IClientMapDefinitionProvider provider)
        => ResolveRange(startId, endIdExclusive, provider.ArchiveSize, () => provider.GetRange(startId, endIdExclusive).Select(MapClientMapDefinition).ToArray());

    private static IResult GetConfigDefinitionType(int id, IConfigDefinitionProvider provider)
        => Resolve(() => TypedResults.Ok(MapConfigDefinition(provider.Get(id))));

    private static IResult GetConfigDefinitionTypeRange(int startId, int endIdExclusive, IConfigDefinitionProvider provider)
        => ResolveRange(startId, endIdExclusive, provider.ArchiveSize, () => provider.GetRange(startId, endIdExclusive).Select(MapConfigDefinition).ToArray());

    private static IResult GetCs2Type(int id, ITypeProvider<ICs2Definition> provider)
        => Resolve(() => TypedResults.Ok(MapCs2(provider.Get(id))));

    private static IResult GetCs2TypeRange(int startId, int endIdExclusive, ITypeProvider<ICs2Definition> provider)
        => ResolveRange(startId, endIdExclusive, provider.ArchiveSize, () => provider.GetRange(startId, endIdExclusive).Select(MapCs2).ToArray());

    private static IResult GetCs2IntType(int id, ICs2IntDefinitionProvider provider)
        => Resolve(() =>
        {
            var result = provider.GetCs2IntDefinition(id);
            return TypedResults.Ok(new Cs2IntDefinitionResponse(result.Id, result.AChar327, result.AnInt325));
        });

    private static IResult GetEquipmentDefaults(IEquipmentDefaultsProvider provider)
        => Resolve(() =>
        {
            var defaults = provider.Get();
            return TypedResults.Ok(new EquipmentDefaultsResponse(
                defaults.Id,
                defaults.BodySlotData,
                defaults.MainHandSlot,
                defaults.WeaponData,
                defaults.ShieldData,
                defaults.OffHandSlot));
        });

    private static IResult GetHuffmanCoding(IHuffmanCodeProvider provider)
        => Resolve(() =>
        {
            var coding = provider.GetCoding();
            return TypedResults.Ok(new HuffmanCodingResponse(coding.BitSizes, coding.Masks, coding.DecryptKeys));
        });

    private static IResult Resolve(Func<IResult> operation)
    {
        try
        {
            return operation();
        }
        catch (Exception ex)
        {
            return EndpointErrors.MapException(ex);
        }
    }

    private static IResult ResolveRange<T>(int startId, int endIdExclusive, int archiveSize, Func<T[]> fetch)
    {
        if (startId < 0 || endIdExclusive < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("startId and endIdExclusive must be greater than or equal to 0."));
        }

        if (endIdExclusive <= startId)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("endIdExclusive must be greater than startId."));
        }

        if (archiveSize > 0 && endIdExclusive > archiveSize)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation($"endIdExclusive must be less than or equal to archive size {archiveSize}."));
        }

        return Resolve(() => TypedResults.Ok(fetch()));
    }

    private static IResult ResolveSearch<T>(string query, int offset, int limit, int archiveSize, Func<T[]> fetch)
    {
        if (archiveSize <= 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("Archive size is 0. Search is not supported for this type."));
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("query is required."));
        }

        if (offset < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("offset must be greater than or equal to 0."));
        }

        if (limit <= 0 || limit > 200)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("limit must be between 1 and 200."));
        }

        return Resolve(() =>
        {
            var matches = fetch();
            var page = matches.Skip(offset).Take(limit).ToArray();
            return TypedResults.Ok(new SearchResponse<T>(query, matches.Length, offset, limit, page));
        });
    }

    private static IResult ResolveIndexSearch<T>(string? query, int offset, int limit, int archiveSize, Func<T[]> fetch)
    {
        if (archiveSize <= 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("Archive size is 0. Search is not supported for this type."));
        }

        if (offset < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("offset must be greater than or equal to 0."));
        }

        if (limit <= 0 || limit > 200)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("limit must be between 1 and 200."));
        }

        return Resolve(() =>
        {
            var matches = fetch();
            var page = matches.Skip(offset).Take(limit).ToArray();
            return TypedResults.Ok(new SearchResponse<T>(query ?? string.Empty, matches.Length, offset, limit, page));
        });
    }

    private static ItemTypeResponse MapItem(IItemType item) =>
        new(
            item.Id,
            item.Name,
            item.Value,
            item.Stackable,
            item.Noted,
            item.NoteId,
            item.MembersOnly,
            item.EquipSlot,
            item.EquipType,
            item.RenderAnimationId,
            item.InventoryOptions,
            item.GroundOptions);

    private static NpcTypeResponse MapNpc(INpcType npc) =>
        new(npc.Id, npc.Name, npc.CombatLevel, npc.Size, npc.SpawnFaceDirection, npc.RenderId);

    private static ObjectTypeResponse MapObject(IObjectType obj) =>
        new(obj.Id, obj.Name, obj.SizeX, obj.SizeY, obj.Solid, obj.Gateway, obj.ClipType, obj.Surroundings, obj.Actions, obj.VarpBitFileId);

    private static SpriteTypeResponse MapSprite(ISpriteType sprite) =>
        new(sprite.Id, sprite.Image.Width, sprite.Image.Height, sprite.Image.Frames.Count);

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

    private static GraphicTypeResponse MapGraphic(IGraphicType graphic) =>
        new(
            graphic.Id,
            graphic.DefaultModelID,
            graphic.AnimationID,
            graphic.ResizeX,
            graphic.ResizeY,
            graphic.Rotation,
            graphic.Ambient,
            graphic.Contrast);

    private static MapTypeResponse MapMap(IMapType map) =>
        new(
            map.Id,
            map.TerrainData.GetLength(0),
            map.TerrainData.GetLength(1),
            map.TerrainData.GetLength(2),
            map.Objects.Count,
            map.Objects.Select(obj => new MapObjectResponse(obj.Id, obj.ShapeType, obj.Rotation, obj.X, obj.Y, obj.Z)).ToArray());

    private static VarpBitDefinitionResponse MapVarpBit(IVarpBitDefinition varpBit) =>
        new(varpBit.Id, varpBit.ConfigID, varpBit.BitLength, varpBit.BitOffset);

    private static ClientMapDefinitionResponse MapClientMapDefinition(IClientMapDefinition definition) =>
        new(
            definition.Id,
            definition.DefaultStringValue,
            definition.DefaultIntValue,
            definition.KeyType,
            definition.ValType,
            definition.Count,
            definition.ValueMap?
                .Select(pair => new ClientMapValueMapEntryResponse(pair.Key, MapDynamicValue(pair.Value)))
                .ToArray(),
            definition.Values?
                .Select(MapDynamicValue)
                .ToArray());

    private static DynamicValueResponse MapDynamicValue(object? value)
    {
        if (value is null)
        {
            return new DynamicValueResponse("null", null);
        }

        return value switch
        {
            string text => new DynamicValueResponse("string", text),
            char character => new DynamicValueResponse("char", character.ToString()),
            bool boolean => new DynamicValueResponse("bool", boolean.ToString()),
            byte b => new DynamicValueResponse("byte", b.ToString()),
            sbyte sb => new DynamicValueResponse("sbyte", sb.ToString()),
            short s => new DynamicValueResponse("short", s.ToString()),
            ushort us => new DynamicValueResponse("ushort", us.ToString()),
            int i => new DynamicValueResponse("int", i.ToString()),
            uint ui => new DynamicValueResponse("uint", ui.ToString()),
            long l => new DynamicValueResponse("long", l.ToString()),
            ulong ul => new DynamicValueResponse("ulong", ul.ToString()),
            float f => new DynamicValueResponse("float", f.ToString()),
            double d => new DynamicValueResponse("double", d.ToString()),
            decimal m => new DynamicValueResponse("decimal", m.ToString()),
            _ => new DynamicValueResponse(value.GetType().Name, value.ToString())
        };
    }

    private static ConfigDefinitionResponse MapConfigDefinition(IConfigDefinition definition) =>
        new(definition.Id, definition.DefaultValue, definition.ValueType);

    private static Cs2DefinitionResponse MapCs2(ICs2Definition definition) =>
        new(
            definition.Id,
            definition.Name,
            definition.IntLocalsCount,
            definition.StringLocalsCount,
            definition.LongLocalsCount,
            definition.IntArgsCount,
            definition.StringArgsCount,
            definition.LongArgsCount,
            definition.Switches.Length,
            definition.Opcodes.Count,
            definition.IntPool.Count,
            definition.StringPool.Count,
            definition.LongPool.Count);

    public sealed record ArchiveSizesResponse(
        int Items,
        int Npcs,
        int Objects,
        int Sprites,
        int Quests,
        int Animations,
        int Graphics,
        int Cs2,
        int ClientMapDefinitions,
        int ConfigDefinitions,
        int VarpBits,
        int Maps);

    public sealed record ItemTypeResponse(
        int Id,
        string Name,
        int Value,
        bool Stackable,
        bool Noted,
        int NoteId,
        bool MembersOnly,
        sbyte EquipSlot,
        sbyte EquipType,
        int RenderAnimationId,
        string?[] InventoryOptions,
        string?[] GroundOptions);

    public sealed record NpcTypeResponse(int Id, string Name, int CombatLevel, int Size, sbyte SpawnFaceDirection, int RenderId);

    public sealed record ObjectTypeResponse(
        int Id,
        string Name,
        int SizeX,
        int SizeY,
        bool Solid,
        bool Gateway,
        int ClipType,
        byte Surroundings,
        string?[] Actions,
        int VarpBitFileId);

    public sealed record SpriteTypeResponse(int Id, int Width, int Height, int FrameCount);

    public sealed record QuestTypeResponse(int Id);

    public sealed record AnimationTypeResponse(int Id, int Priority);

    public sealed record GraphicTypeResponse(int Id, int DefaultModelId, int AnimationId, int ResizeX, int ResizeY, int Rotation, int Ambient, int Contrast);

    public sealed record MapDecodeRequest(int[] XteaKeys);

    public sealed record MapTypeResponse(int Id, int TerrainPlanes, int TerrainWidth, int TerrainHeight, int ObjectCount, IReadOnlyList<MapObjectResponse> Objects);

    public sealed record MapObjectResponse(int Id, int ShapeType, int Rotation, int X, int Y, int Z);

    public sealed record VarpBitDefinitionResponse(int Id, short ConfigId, byte BitLength, byte BitOffset);

    public sealed record ClientMapDefinitionResponse(
        int Id,
        string DefaultStringValue,
        int DefaultIntValue,
        char KeyType,
        char ValueType,
        int Count,
        IReadOnlyList<ClientMapValueMapEntryResponse>? ValueMap,
        IReadOnlyList<DynamicValueResponse>? Values);

    public sealed record ClientMapValueMapEntryResponse(int Key, DynamicValueResponse Value);

    public sealed record DynamicValueResponse(string Type, string? Value);

    public sealed record ConfigDefinitionResponse(int Id, int DefaultValue, char ValueType);

    public sealed record Cs2DefinitionResponse(
        int Id,
        string Name,
        int IntLocalsCount,
        int StringLocalsCount,
        int LongLocalsCount,
        int IntArgsCount,
        int StringArgsCount,
        int LongArgsCount,
        int SwitchTablesCount,
        int OpcodeCount,
        int IntPoolCount,
        int StringPoolCount,
        int LongPoolCount);

    public sealed record Cs2IntDefinitionResponse(int Id, char KeyType, int Value);

    public sealed record EquipmentDefaultsResponse(int Id, byte[] BodySlotData, int MainHandSlot, int[] WeaponData, int[] ShieldData, int OffHandSlot);

    public sealed record HuffmanCodingResponse(byte[] BitSizes, int[] Masks, int[] DecryptKeys);

    public sealed record SearchResponse<T>(string Query, int TotalMatches, int Offset, int Limit, IReadOnlyList<T> Items);
}
