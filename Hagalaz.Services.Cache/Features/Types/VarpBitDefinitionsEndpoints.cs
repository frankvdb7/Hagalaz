using System.Linq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Hagalaz.Services.Cache.Features.Types.TypeQueryUtilities;
using static Hagalaz.Services.Cache.Features.Types.TypeMutationUtilities;

namespace Hagalaz.Services.Cache.Features.Types;

public static class VarpBitDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapVarpBitDefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/varp-bits/{id:int}", GetVarpBitType)
            .WithSummary("Get a varp bit definition").Produces<GetVarpBitDefinitionDto>();
        types.MapGet("/varp-bits/range", GetVarpBitTypeRange)
            .WithSummary("List varp bit definitions in a range").Produces<GetVarpBitDefinitionDto[]>();
        types.MapPost("/varp-bits/{id:int}", UpdateVarpBitDefinition)
            .WithSummary("Update a varp bit definition").Produces<UpdateVarpBitDefinitionResultDto>();
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
        IVarpBitDefinitionProvider provider,
        ITypeCodec<IVarpBitDefinition> codec,
        ICacheAPI cacheApi)
    {
        var error = TryGetType(id, provider, out var varpBit);
        if (error is not null) return error;

        varpBit.ConfigID = request.ConfigId;
        varpBit.BitLength = request.BitLength;
        varpBit.BitOffset = request.BitOffset;
        PersistTypeEntry(id, varpBit, codec, new VarpBitDefinitionData(), cacheApi);
        return TypedResults.Ok(new UpdateVarpBitDefinitionResultDto("varp-bit-updated", "varp-bit", id));
    }
}
