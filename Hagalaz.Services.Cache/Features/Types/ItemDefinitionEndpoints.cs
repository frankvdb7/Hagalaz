using System;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Services.Cache.Features.Shared;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Hagalaz.Services.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Hagalaz.Services.Cache.Features.Types.TypeQueryUtilities;
using static Hagalaz.Services.Cache.Features.Types.TypeMutationUtilities;

namespace Hagalaz.Services.Cache.Features.Types;

public static class ItemDefinitionEndpoints
{
    public static IEndpointRouteBuilder MapItemDefinitionEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/items/{id:int}", GetItemType);
        types.MapGet("/items/{id:int}/image", GetItemTypeImage)
            .WithCacheControl(TimeSpan.FromDays(1), isPublic: true);
        types.MapGet("/items/range", GetItemTypeRange);
        types.MapGet("/items/search", SearchItemTypes);
        types.MapGet("/items/{id:int}/definition", GetItemDefinition);
        types.MapPut("/items/{id:int}/definition", UpdateItemDefinition);
        types.MapPost("/items/{id:int}/duplicate", DuplicateItem);
        return types;
    }

    private static IResult GetItemType(int id, ITypeProvider<IItemType> provider) => TypedResults.Ok(MapItem(provider.Get(id)));

    private static IResult GetItemTypeImage(int id, IWebHostEnvironment environment)
    {
        var path = $"images/items/{id}.gif";
        var file = environment.WebRootFileProvider.GetFileInfo(path);
        if (!file.Exists) return TypedResults.NotFound();

        return TypedResults.VirtualFile(path, "image/gif", lastModified: file.LastModified);
    }

    private static IResult GetItemTypeRange(int startId, int endIdExclusive, ITypeProvider<IItemType> provider)
    {
        var error = ValidateRange(startId, endIdExclusive, provider.ArchiveSize);
        return error ?? TypedResults.Ok(provider.GetRange(startId, endIdExclusive).Select(MapItem).ToArray());
    }

    private static IResult SearchItemTypes(string query, int offset, int limit, ITypeProvider<IItemType> provider) =>
        ValidateSearch(query, offset, limit, provider.ArchiveSize, requireQuery: true)
        ?? CreateSearchResponse(query, offset, limit, provider.GetRange(0, provider.ArchiveSize)
            .Where(type => !string.IsNullOrWhiteSpace(type.Name) && type.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(MapItem)
            .ToArray());

    private static GetItemTypeDto MapItem(IItemType item) =>
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
            item.GroundOptions,
            item.NoteTemplateId,
            item.LendId,
            item.LendTemplateId,
            item.BoughtItemId,
            item.BoughtTemplateId,
            item.InterfaceModelId);

    private static IResult GetItemDefinition(int id, IItemTypeCodec codec, ICacheAPI cacheApi)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Item ID must be nonnegative."));
        return TypedResults.Ok(GetItemDefinitionDto.FromItem((ItemType)DecodeRawTypeEntry(id, codec, new ItemTypeData(), cacheApi)));
    }

    private static IResult UpdateItemDefinition(int id, UpdateItemDefinitionDto request, IItemTypeCodec codec, ICacheAPI cacheApi)
    {
        if (id < 0 || request.Id != id || request.Value < 0 || string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 50)
            return TypedResults.BadRequest(EndpointErrors.Validation("Provide matching nonnegative IDs, a nonnegative base value and a display name of 1-50 characters."));
        foreach (var character in request.Name)
            if (character < ' ' || character > byte.MaxValue)
                return TypedResults.BadRequest(EndpointErrors.Validation("Display name must contain printable Latin-1 characters."));
        var item = (ItemType)DecodeRawTypeEntry(id, codec, new ItemTypeData(), cacheApi);
        request.ApplyTo(item);
        using var encoded = codec.Encode(item);
        using var validation = new MemoryStream(encoded.ToArray());
        _ = codec.Decode(id, validation);
        PersistTypeEntry(cacheApi, new ItemTypeData(), id, encoded.ToArray());
        return TypedResults.Ok(new UpdateItemDefinitionResultDto("item-updated", "item", id));
    }

    private static IResult DuplicateItem(int id, ICacheAPI cacheApi)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Item ID must be nonnegative."));
        var typeData = new ItemTypeData();
        var destination = cacheApi.AppendArchiveMemberCopy(typeData.IndexId, id >> typeData.ArchiveEntryOffset,
            id & (typeData.ArchiveEntrySize - 1), typeData.ArchiveEntrySize);
        var target = checked((destination.FileId << typeData.ArchiveEntryOffset) + destination.EntryId);
        return TypedResults.Created($"/api/v1/cache/types/items/{target}", new DuplicateItemDefinitionResultDto("item-duplicated", "item", target));
    }
}
