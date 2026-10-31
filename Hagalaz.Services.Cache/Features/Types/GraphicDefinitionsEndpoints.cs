using System.Linq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Hagalaz.Services.Cache.Features.Types.TypeQueryUtilities;

namespace Hagalaz.Services.Cache.Features.Types;

public static class GraphicDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapGraphicDefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/graphics/{id:int}", GetGraphicType);
        types.MapGet("/graphics/range", GetGraphicTypeRange);
        return types;
    }

    private static IResult GetGraphicType(int id, ITypeProvider<IGraphicType> provider) => TypedResults.Ok(MapGraphic(provider.Get(id)));

    private static IResult GetGraphicTypeRange(int startId, int endIdExclusive, ITypeProvider<IGraphicType> provider)
    {
        var error = ValidateRange(startId, endIdExclusive, provider.ArchiveSize);
        return error ?? TypedResults.Ok(provider.GetRange(startId, endIdExclusive).Select(MapGraphic).ToArray());
    }

    private static GetGraphicDefinitionDto MapGraphic(IGraphicType graphic) =>
        new(
            graphic.Id,
            graphic.DefaultModelID,
            graphic.AnimationID,
            graphic.ResizeX,
            graphic.ResizeY,
            graphic.Rotation,
            graphic.Ambient,
            graphic.Contrast);
}
