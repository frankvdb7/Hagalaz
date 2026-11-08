using System.Linq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Hagalaz.Services.Cache.Features.Types.TypeQueryUtilities;

namespace Hagalaz.Services.Cache.Features.Types;

public static class AnimationDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapAnimationDefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/animations/{id:int}", GetAnimationType)
            .WithSummary("Get an animation definition").Produces<GetAnimationDefinitionDto>();
        types.MapGet("/animations/range", GetAnimationTypeRange)
            .WithSummary("List animation definitions in a range").Produces<GetAnimationDefinitionDto[]>();
        return types;
    }

    private static IResult GetAnimationType(int id, ITypeProvider<IAnimationType> provider)
    {
        var animation = provider.Get(id);
        return TypedResults.Ok(new GetAnimationDefinitionDto(animation.Id, animation.Priority));
    }

    private static IResult GetAnimationTypeRange(int startId, int endIdExclusive, ITypeProvider<IAnimationType> provider)
    {
        var error = ValidateRange(startId, endIdExclusive, provider.ArchiveSize);
        return error ?? TypedResults.Ok(provider.GetRange(startId, endIdExclusive).Select(a => new GetAnimationDefinitionDto(a.Id, a.Priority)).ToArray());
    }
}
