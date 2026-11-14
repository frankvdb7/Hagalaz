using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features.Types;

public static class DefinitionArchiveEndpoints
{
    public static IEndpointRouteBuilder MapDefinitionArchiveEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/archive-sizes", GetArchiveSizes).WithSummary("Get the size of each definition archive").Produces<GetArchiveSizesDto>();
        return types;
    }

    private static IResult GetArchiveSizes(
        ITypeProvider<IItemType> items,
        ITypeProvider<INpcType> npcs,
        ITypeProvider<IObjectType> objects,
        ITypeProvider<ISpriteType> sprites,
        ITypeProvider<IQuestType> quests,
        ITypeProvider<IAnimationType> animations,
        ITypeProvider<IGraphicType> graphics,
        ITypeProvider<ICs2Definition> cs2,
        IClientMapDefinitionProvider clientMaps,
        IConfigDefinitionProvider configs,
        IVarpBitDefinitionProvider varpBits,
        IMapProvider maps)
    {
        return TypedResults.Ok(new GetArchiveSizesDto(
            items.ArchiveSize,
            npcs.ArchiveSize,
            objects.ArchiveSize,
            sprites.ArchiveSize,
            quests.ArchiveSize,
            animations.ArchiveSize,
            graphics.ArchiveSize,
            cs2.ArchiveSize,
            clientMaps.ArchiveSize,
            configs.ArchiveSize,
            varpBits.ArchiveSize,
            maps.ArchiveSize));
    }
}
