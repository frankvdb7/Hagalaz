using AutoMapper;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Items;

using Hagalaz.Cache.Types;
using GameItemDefinition = Hagalaz.Services.GameWorld.Data.Model.ItemDefinition;

namespace Hagalaz.Services.GameWorld.Profiles
{
    public class ItemProfile : Profile
    {
        public ItemProfile() 
        {
            CreateMap<ItemType, GameItemDefinition>()
                .ForMember(destination => destination.Examine, options => options.Ignore())
                .ForMember(destination => destination.Weight, options => options.Ignore())
                .ForMember(destination => destination.Tradeable, options => options.Ignore())
                .ForMember(destination => destination.HighAlchemyValue, options => options.Ignore())
                .ForMember(destination => destination.LowAlchemyValue, options => options.Ignore())
                .ForMember(destination => destination.TradeValue, options => options.Ignore());
            CreateMap<ItemType, IItemDefinition>().As<GameItemDefinition>();
            CreateMap<Hagalaz.Data.Entities.ItemDefinition, GameItemDefinition>(MemberList.None)
                .ConstructUsing(source => new GameItemDefinition(source.Id))
                .ForMember(destination => destination.Examine, options => options.MapFrom(source => source.Examine))
                .ForMember(destination => destination.Weight, options => options.MapFrom(source => (double)source.Weight))
                .ForMember(destination => destination.Tradeable, options => options.MapFrom(source => source.Tradeable == 1))
                .ForMember(destination => destination.HighAlchemyValue, options => options.MapFrom(source => source.HighAlchemyValue))
                .ForMember(destination => destination.LowAlchemyValue, options => options.MapFrom(source => source.LowAlchemyValue))
                .ForMember(destination => destination.TradeValue, options => options.MapFrom(source => source.TradePrice));
            CreateMap<Hagalaz.Data.Entities.ItemDefinition, IItemDefinition>().As<GameItemDefinition>();

            CreateProjection<ItemSpawn, GroundItemSpawnDto>()
                .ForMember(dto => dto.ItemID, opt => opt.MapFrom(src => (int)src.ItemId))
                .ForMember(dto => dto.ItemCount, opt => opt.MapFrom(src => src.Count))
                .ForMember(dto => dto.RespawnTicks, opt => opt.MapFrom(src => (int)src.RespawnTicks))
                .ForMember(dto => dto.Location, opt => opt.MapFrom(src => new Location(src.CoordX, src.CoordY, src.CoordZ, 0)));
        }
    }
}
