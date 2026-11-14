using System.Linq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Services.Cache.Features.Shared;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Hagalaz.Services.Cache.Features.Types.TypeQueryUtilities;
using Hagalaz.Cache.Types;

namespace Hagalaz.Services.Cache.Features.Types;

public static class ConfigDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapConfigDefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/config-definitions/{id:int}", GetConfigDefinitionType).WithSummary("Get a config definition").Produces<GetConfigDefinitionDto>();
        types.MapGet("/config-definitions/range", GetConfigDefinitionTypeRange).WithSummary("List config definitions in a range").Produces<GetConfigDefinitionDto[]>();
        types.MapPost("/config-definitions/{id:int}", UpdateConfigDefinition).WithSummary("Update a config definition").Produces<UpdateConfigDefinitionResultDto>();
        return types;
    }

    private static IResult GetConfigDefinitionType(int id, IConfigDefinitionProvider provider)
        => TypedResults.Ok(MapConfigDefinition(provider.Get(id)));

    private static IResult GetConfigDefinitionTypeRange(int startId, int endIdExclusive, IConfigDefinitionProvider provider)
    {
        var error = ValidateRange(startId, endIdExclusive, provider.ArchiveSize);
        return error ?? TypedResults.Ok(provider.GetRange(startId, endIdExclusive).Select(MapConfigDefinition).ToArray());
    }

    private static GetConfigDefinitionDto MapConfigDefinition(IConfigDefinition definition) =>
        new(definition.Id, definition.DefaultValue, definition.ValueType);

    private static IResult UpdateConfigDefinition(
        int id,
        UpdateConfigDefinitionDto request,
        TypeMutator<IConfigDefinition> configs)
    {
        if (request.DefaultValue is < ushort.MinValue or > ushort.MaxValue)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("defaultValue must be between 0 and 65535."));
        }

        if (request.ValueType > '\x7f')
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("valueType must be an ASCII character."));
        }

        if (id < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("id must be greater than or equal to 0."));
        }

        var result = configs.Mutate(id, config =>
        {
            config.DefaultValue = request.DefaultValue;
            config.ValueType = request.ValueType;
        });
        if (result.IsFailed) return EndpointErrors.TypeMutationFailure(result, "config definition", id);
        return TypedResults.Ok(new UpdateConfigDefinitionResultDto("config-definition-updated", "config-definition", id));
    }
}
