using System.Linq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Hagalaz.Services.Cache.Features.Types.TypeQueryUtilities;

namespace Hagalaz.Services.Cache.Features.Types;

public static class QuestDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapQuestDefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/quests/{id:int}", GetQuestType);
        types.MapGet("/quests/range", GetQuestTypeRange);
        return types;
    }

    private static IResult GetQuestType(int id, ITypeProvider<IQuestType> provider) => TypedResults.Ok(new GetQuestDefinitionDto(provider.Get(id).Id));

    private static IResult GetQuestTypeRange(int startId, int endIdExclusive, ITypeProvider<IQuestType> provider)
    {
        var error = ValidateRange(startId, endIdExclusive, provider.ArchiveSize);
        return error ?? TypedResults.Ok(provider.GetRange(startId, endIdExclusive).Select(q => new GetQuestDefinitionDto(q.Id)).ToArray());
    }
}
