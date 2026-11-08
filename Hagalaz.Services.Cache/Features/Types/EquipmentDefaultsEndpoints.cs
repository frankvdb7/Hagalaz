using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features.Types;

public static class EquipmentDefaultsEndpoints
{
    public static IEndpointRouteBuilder MapEquipmentDefaultsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/equipment-defaults", GetEquipmentDefaults)
            .WithSummary("Get the equipment defaults").Produces<GetEquipmentDefaultsDto>();
        return types;
    }

    private static IResult GetEquipmentDefaults(IEquipmentDefaultsProvider provider)
    {
        var defaults = provider.Get();
        return TypedResults.Ok(new GetEquipmentDefaultsDto(
            defaults.Id,
            defaults.BodySlotData,
            defaults.MainHandSlot,
            defaults.WeaponData,
            defaults.ShieldData,
            defaults.OffHandSlot));
    }
}
