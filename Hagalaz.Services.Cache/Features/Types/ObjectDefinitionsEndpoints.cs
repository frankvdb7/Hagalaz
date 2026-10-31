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

public static class ObjectDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapObjectDefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/objects/{id:int}", GetObjectType);
        types.MapGet("/objects/range", GetObjectTypeRange);
        types.MapPost("/objects/{id:int}/name", SetObjectName);
        return types;
    }

    private static IResult GetObjectType(int id, ITypeProvider<IObjectType> provider) => TypedResults.Ok(MapObject(provider.Get(id)));

    private static IResult GetObjectTypeRange(int startId, int endIdExclusive, ITypeProvider<IObjectType> provider)
    {
        var error = ValidateRange(startId, endIdExclusive, provider.ArchiveSize);
        return error ?? TypedResults.Ok(provider.GetRange(startId, endIdExclusive).Select(MapObject).ToArray());
    }

    private static GetObjectDefinitionDto MapObject(IObjectType obj) =>
        new(obj.Id, obj.Name, obj.SizeX, obj.SizeY, obj.Solid, obj.Gateway, obj.ClipType, obj.Surroundings, obj.Actions, obj.VarpBitFileId);

    private static IResult SetObjectName(
        int id,
        UpdateObjectNameDto request,
        IObjectTypeCodec codec,
        ICacheAPI cacheApi)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("name is required."));
        }

        for (var i = 0; i < request.Name.Length; i++)
        {
            if (request.Name[i] == '\0')
            {
                return TypedResults.BadRequest(EndpointErrors.Validation("name must not contain a null character."));
            }

            if (request.Name[i] > byte.MaxValue)
            {
                return TypedResults.BadRequest(EndpointErrors.Validation("name must contain only Latin-1 characters."));
            }
        }

        if (id < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("id must be greater than or equal to 0."));
        }

        var typeData = new ObjectTypeData();
        var obj = DecodeRawTypeEntry(id, codec, typeData, cacheApi);
        obj.Name = request.Name;
        PersistTypeEntry(id, obj, codec, typeData, cacheApi);
        return TypedResults.Ok(new UpdateObjectNameResultDto("object-name-updated", "object", id));
    }
}
