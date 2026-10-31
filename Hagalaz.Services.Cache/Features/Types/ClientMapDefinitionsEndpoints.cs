using System.Linq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Hagalaz.Services.Cache.Features.Types.TypeQueryUtilities;

namespace Hagalaz.Services.Cache.Features.Types;

public static class ClientMapDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapClientMapDefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/client-map-definitions/{id:int}", GetClientMapDefinitionType);
        types.MapGet("/client-map-definitions/range", GetClientMapDefinitionTypeRange);
        return types;
    }

    private static IResult GetClientMapDefinitionType(int id, IClientMapDefinitionProvider provider)
        => TypedResults.Ok(MapClientMapDefinition(provider.Get(id)));

    private static IResult GetClientMapDefinitionTypeRange(int startId, int endIdExclusive, IClientMapDefinitionProvider provider)
    {
        var error = ValidateRange(startId, endIdExclusive, provider.ArchiveSize);
        return error ?? TypedResults.Ok(provider.GetRange(startId, endIdExclusive).Select(MapClientMapDefinition).ToArray());
    }

    private static GetClientMapDefinitionDto MapClientMapDefinition(IClientMapDefinition definition) =>
        new(
            definition.Id,
            definition.DefaultStringValue,
            definition.DefaultIntValue,
            definition.KeyType,
            definition.ValType,
            definition.Count,
            definition.ValueMap?
                .Select(pair => new ClientMapValueMapEntryDto(pair.Key, MapDynamicValue(pair.Value)))
                .ToArray(),
            definition.Values?
                .Select(MapDynamicValue)
                .ToArray());

    private static DynamicValueDto MapDynamicValue(object? value)
    {
        if (value is null)
        {
            return new DynamicValueDto("null", null);
        }

        return value switch
        {
            string text => new DynamicValueDto("string", text),
            char character => new DynamicValueDto("char", character.ToString()),
            bool boolean => new DynamicValueDto("bool", boolean.ToString()),
            byte b => new DynamicValueDto("byte", b.ToString()),
            sbyte sb => new DynamicValueDto("sbyte", sb.ToString()),
            short s => new DynamicValueDto("short", s.ToString()),
            ushort us => new DynamicValueDto("ushort", us.ToString()),
            int i => new DynamicValueDto("int", i.ToString()),
            uint ui => new DynamicValueDto("uint", ui.ToString()),
            long l => new DynamicValueDto("long", l.ToString()),
            ulong ul => new DynamicValueDto("ulong", ul.ToString()),
            float f => new DynamicValueDto("float", f.ToString()),
            double d => new DynamicValueDto("double", d.ToString()),
            decimal m => new DynamicValueDto("decimal", m.ToString()),
            _ => new DynamicValueDto(value.GetType().Name, value.ToString())
        };
    }
}
