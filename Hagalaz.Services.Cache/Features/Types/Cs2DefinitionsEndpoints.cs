using System.Linq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Hagalaz.Services.Cache.Features.Types.TypeQueryUtilities;

namespace Hagalaz.Services.Cache.Features.Types;

public static class Cs2DefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapCs2DefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/cs2/{id:int}", GetCs2Type)
            .WithSummary("Get a CS2 script definition").Produces<GetCs2DefinitionDto>();
        types.MapGet("/cs2/range", GetCs2TypeRange)
            .WithSummary("List CS2 script definitions in a range").Produces<GetCs2DefinitionDto[]>();
        return types;
    }

    private static IResult GetCs2Type(int id, ITypeProvider<ICs2Definition> provider)
        => TypedResults.Ok(MapCs2(provider.Get(id)));

    private static IResult GetCs2TypeRange(int startId, int endIdExclusive, ITypeProvider<ICs2Definition> provider)
    {
        var error = ValidateRange(startId, endIdExclusive, provider.ArchiveSize);
        return error ?? TypedResults.Ok(provider.GetRange(startId, endIdExclusive).Select(MapCs2).ToArray());
    }

    private static GetCs2DefinitionDto MapCs2(ICs2Definition definition) =>
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
}
