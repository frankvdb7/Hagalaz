using AutoMapper;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Services.GameWorld.Profiles
{
    public class ItemProfile : Profile
    {
        public ItemProfile() 
        {
            CreateProjection<ItemSpawn, GroundItemSpawnDto>()
                .ForMember(dto => dto.ItemID, opt => opt.MapFrom(src => (int)src.ItemId))
                .ForMember(dto => dto.ItemCount, opt => opt.MapFrom(src => src.Count))
                .ForMember(dto => dto.RespawnTicks, opt => opt.MapFrom(src => (int)src.RespawnTicks))
                .ForMember(dto => dto.Location, opt => opt.MapFrom(src => new Location(src.CoordX, src.CoordY, src.CoordZ, 0)));
        }
    }
}
