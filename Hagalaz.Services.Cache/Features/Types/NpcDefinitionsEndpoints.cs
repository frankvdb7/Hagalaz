using System;
using System.Linq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Services.Cache.Features.Shared;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Hagalaz.Services.Cache.Features.Types.TypeQueryUtilities;
using static Hagalaz.Services.Cache.Features.Types.TypeMutationUtilities;

namespace Hagalaz.Services.Cache.Features.Types;

public static class NpcDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapNpcDefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/npcs/{id:int}", GetNpcType)
            .WithSummary("Get an NPC definition").Produces<GetNpcDefinitionDto>();
        types.MapGet("/npcs/range", GetNpcTypeRange)
            .WithSummary("List NPC definitions in a range").Produces<GetNpcDefinitionDto[]>();
        types.MapGet("/npcs/search", SearchNpcTypes)
            .WithSummary("Search NPCs by name").Produces<SearchDefinitionsDto<GetNpcDefinitionDto>>();
        types.MapPost("/npcs/{id:int}/combat-level", SetNpcCombatLevel)
            .WithSummary("Set an NPC's combat level").Produces<UpdateNpcCombatLevelResultDto>();
        return types;
    }

    private static IResult GetNpcType(int id, ITypeProvider<INpcType> provider) => TypedResults.Ok(MapNpc(provider.Get(id)));

    private static IResult GetNpcTypeRange(int startId, int endIdExclusive, ITypeProvider<INpcType> provider)
    {
        var error = ValidateRange(startId, endIdExclusive, provider.ArchiveSize);
        return error ?? TypedResults.Ok(provider.GetRange(startId, endIdExclusive).Select(MapNpc).ToArray());
    }

    private static IResult SearchNpcTypes(string query, int offset, int limit, ITypeProvider<INpcType> provider) =>
        ValidateSearch(query, offset, limit, provider.ArchiveSize, requireQuery: true)
        ?? CreateSearchResponse(query, offset, limit, provider.GetRange(0, provider.ArchiveSize)
            .Where(type => !string.IsNullOrWhiteSpace(type.Name) && type.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(MapNpc)
            .ToArray());

    private static GetNpcDefinitionDto MapNpc(INpcType npc) =>
        new(npc.Id, npc.Name, npc.CombatLevel, npc.Size, npc.SpawnFaceDirection, npc.RenderId);

    private static IResult SetNpcCombatLevel(
        int id,
        UpdateNpcCombatLevelDto request,
        INpcTypeCodec codec,
        ICacheAPI cacheApi)
    {
        if (id < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("id must be greater than or equal to 0."));
        }

        if (request.CombatLevel is < -1 or > ushort.MaxValue)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("combatLevel must be -1 or between 0 and 65535."));
        }

        var npc = DecodeRawTypeEntry(id, codec, new NpcTypeData(), cacheApi);
        npc.CombatLevel = request.CombatLevel;
        PersistTypeEntry(id, npc, codec, new NpcTypeData(), cacheApi);
        return TypedResults.Ok(new UpdateNpcCombatLevelResultDto("npc-combat-level-updated", "npc", id));
    }
}
