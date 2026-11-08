using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic.Rendering;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Services.Cache.Features.Shared;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using static Hagalaz.Services.Cache.Features.Types.TypeQueryUtilities;
using static Hagalaz.Services.Cache.Features.Types.TypeMutationUtilities;

namespace Hagalaz.Services.Cache.Features.Types;

public static class ItemDefinitionEndpoints
{
    public static IEndpointRouteBuilder MapItemDefinitionEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/items/{id:int}", GetItemType).WithSummary("Get an item summary").Produces<GetItemTypeDto>();
        types.MapGet("/items/{id:int}/image", GetItemTypeImage).WithSummary("Render an item's inventory icon as PNG").Produces<System.IO.Stream>(StatusCodes.Status200OK, "image/png").Produces(StatusCodes.Status304NotModified);
        types.MapGet("/items/range", GetItemTypeRange).WithSummary("List item summaries in a range").Produces<GetItemTypeDto[]>();
        types.MapGet("/items/search", SearchItemTypes).WithSummary("Search items by name").Produces<SearchDefinitionsDto<GetItemTypeDto>>();
        types.MapGet("/items/{id:int}/definition", GetItemDefinition).WithSummary("Get an item's full editable definition").Produces<GetItemDefinitionDto>();
        types.MapPut("/items/{id:int}/definition", UpdateItemDefinition).WithSummary("Replace an item's full definition").Produces<UpdateItemDefinitionResultDto>();
        types.MapPost("/items/{id:int}/duplicate", DuplicateItem).WithSummary("Copy an item definition to the next free ID").Produces<DuplicateItemDefinitionResultDto>(StatusCodes.Status201Created);
        return types;
    }

    private static IResult GetItemType(int id, ITypeProvider<IItemType> provider) => TypedResults.Ok(MapItem(provider.Get(id)));

    private static IResult GetItemTypeImage(int id, HttpContext context, ICacheAPI cache, ITypeProvider<IItemType> items,
        int width = ItemIconRenderer.ClientWidth, int height = ItemIconRenderer.ClientHeight, int outline = 1, string? shadow = null,
        int quantity = 1, int? quantityMode = null)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Item ID must be nonnegative."));
        if (width is < 8 or > 512 || height is < 8 or > 512)
            return TypedResults.BadRequest(EndpointErrors.Validation("width and height must be between 8 and 512."));
        if (outline is < 0 or > 2) return TypedResults.BadRequest(EndpointErrors.Validation("outline must be 0, 1 or 2."));
        if (quantity < -1) return TypedResults.BadRequest(EndpointErrors.Validation("quantity must be -1 or greater."));
        if (quantityMode is < 0 or > 2) return TypedResults.BadRequest(EndpointErrors.Validation("quantityMode must be 0, 1 or 2."));
        int? shadowColor = ItemIconRenderer.InventoryShadow;
        if (shadow is not null)
        {
            if (shadow == "none") shadowColor = null;
            else if (shadow.Length == 6 && int.TryParse(shadow, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var rgb)) shadowColor = rgb;
            else return TypedResults.BadRequest(EndpointErrors.Validation("shadow must be none or a six-digit RRGGBB colour."));
        }

        var options = new ItemIcons.Options(width, height, outline, shadowColor, quantity, quantityMode);
        // Revalidation is checked before rendering, so unchanged icons cost only reference-table lookups.
        var etag = new EntityTagHeaderValue($"\"item-icon-{ItemIcons.Validator(id, options, cache)}\"");
        var validators = context.Request.GetTypedHeaders().IfNoneMatch;
        if (validators?.Any(candidate => candidate.Equals(EntityTagHeaderValue.Any) || candidate.Compare(etag, useStrongComparison: false)) == true)
        {
            SetValidator(context, etag);
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        byte[]? png;
        try { png = ItemIcons.RenderPng(id, options, cache, items); }
        catch (FileNotFoundException) { png = null; }
        if (png is null) return TypedResults.NotFound();

        SetValidator(context, etag);
        return TypedResults.File(png, "image/png");

        static void SetValidator(HttpContext context, EntityTagHeaderValue etag)
        {
            context.Response.Headers.CacheControl = "private, no-cache";
            context.Response.GetTypedHeaders().ETag = etag;
        }
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
        if (request.Validate(id) is { } error) return TypedResults.BadRequest(EndpointErrors.Validation(error));
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
