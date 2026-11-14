using System.Linq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Hagalaz.Services.Cache.Features.Types.TypeQueryUtilities;
using Hagalaz.Cache.Types;
using Hagalaz.Services.Cache.Features.Shared;

namespace Hagalaz.Services.Cache.Features.Types;

public static class VarpBitDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapVarpBitDefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/varp-bits/{id:int}", GetVarpBitType).WithSummary("Get a varp bit definition").Produces<GetVarpBitDefinitionDto>();
        types.MapGet("/varp-bits/range", GetVarpBitTypeRange).WithSummary("List varp bit definitions in a range").Produces<GetVarpBitDefinitionDto[]>();
        types.MapPost("/varp-bits/{id:int}", UpdateVarpBitDefinition).WithSummary("Update a varp bit definition").Produces<UpdateVarpBitDefinitionResultDto>();
        return types;
    }

    private static IResult GetVarpBitType(int id, IVarpBitDefinitionProvider provider) => TypedResults.Ok(MapVarpBit(provider.Get(id)));

    private static IResult GetVarpBitTypeRange(int startId, int endIdExclusive, IVarpBitDefinitionProvider provider)
    {
        var error = ValidateRange(startId, endIdExclusive, provider.ArchiveSize);
        return error ?? TypedResults.Ok(provider.GetRange(startId, endIdExclusive).Select(MapVarpBit).ToArray());
    }

    private static GetVarpBitDefinitionDto MapVarpBit(IVarpBitDefinition varpBit) =>
        new(varpBit.Id, varpBit.ConfigID, varpBit.BitLength, varpBit.BitOffset);

    private static IResult UpdateVarpBitDefinition(
        int id,
        UpdateVarpBitDefinitionDto request,
        TypeMutator<IVarpBitDefinition> varpBits)
    {
        if (id < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("id must be greater than or equal to 0."));
        }

        var result = varpBits.Mutate(id, varpBit =>
        {
            varpBit.ConfigID = request.ConfigId;
            varpBit.BitLength = request.BitLength;
            varpBit.BitOffset = request.BitOffset;
        });
        if (result.IsFailed) return EndpointErrors.TypeMutationFailure(result, "varp bit", id);
        return TypedResults.Ok(new UpdateVarpBitDefinitionResultDto("varp-bit-updated", "varp-bit", id));
    }
}
