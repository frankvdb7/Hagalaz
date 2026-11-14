using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features.Types;

public static class TypeEndpoints
{
    public static IEndpointRouteBuilder MapTypeEndpoints(this IEndpointRouteBuilder group)
    {
        var types = group.MapGroup("/types");
        types.MapItemDefinitionEndpoints();
        types.MapNpcDefinitionsEndpoints();
        types.MapObjectDefinitionsEndpoints();
        types.MapSpriteDefinitionsEndpoints();
        types.MapQuestDefinitionsEndpoints();
        types.MapAnimationDefinitionsEndpoints();
        types.MapGraphicDefinitionsEndpoints();
        types.MapMapDefinitionsEndpoints();
        types.MapVarpBitDefinitionsEndpoints();
        types.MapClientMapDefinitionsEndpoints();
        types.MapConfigDefinitionsEndpoints();
        types.MapCs2DefinitionsEndpoints();
        types.MapCs2IntDefinitionsEndpoints();
        types.MapEquipmentDefaultsEndpoints();
        types.MapHuffmanDefinitionsEndpoints();
        types.MapDefinitionArchiveEndpoints();
        return group;
    }
}
