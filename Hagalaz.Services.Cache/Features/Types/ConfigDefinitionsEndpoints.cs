using System.Linq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Services.Cache.Features.Shared;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Hagalaz.Services.Cache.Features.Types.TypeQueryUtilities;
using static Hagalaz.Services.Cache.Features.Types.TypeMutationUtilities;

namespace Hagalaz.Services.Cache.Features.Types;

public static class ConfigDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapConfigDefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/config-definitions/{id:int}", GetConfigDefinitionType)
            .WithSummary("Get a config definition").Produces<GetConfigDefinitionDto>();
        types.MapGet("/config-definitions/range", GetConfigDefinitionTypeRange)
            .WithSummary("List config definitions in a range").Produces<GetConfigDefinitionDto[]>();
        types.MapPost("/config-definitions/{id:int}", UpdateConfigDefinition)
            .WithSummary("Update a config definition").Produces<UpdateConfigDefinitionResultDto>();
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
        IConfigDefinitionProvider provider,
        ITypeCodec<IConfigDefinition> codec,
        ICacheAPI cacheApi)
    {
        if (request.DefaultValue is < ushort.MinValue or > ushort.MaxValue)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("defaultValue must be between 0 and 65535."));
        }

        if (request.ValueType > '\x7f')
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("valueType must be an ASCII character."));
        }

        var error = TryGetType(id, provider, out var config);
        if (error is not null) return error;

        config.DefaultValue = request.DefaultValue;
        config.ValueType = request.ValueType;
        PersistTypeEntry(id, config, codec, new ConfigDefinitionData(), cacheApi);
        return TypedResults.Ok(new UpdateConfigDefinitionResultDto("config-definition-updated", "config-definition", id));
    }
}
