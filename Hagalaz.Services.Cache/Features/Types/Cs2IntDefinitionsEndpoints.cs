using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features.Types;

public static class Cs2IntDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapCs2IntDefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/cs2-int/{id:int}", GetCs2IntType);
        return types;
    }

    private static IResult GetCs2IntType(int id, ICs2IntDefinitionProvider provider)
    {
        var result = provider.GetCs2IntDefinition(id);
        return TypedResults.Ok(new GetCs2IntDefinitionDto(result.Id, result.AChar327, result.AnInt325));
    }
}
