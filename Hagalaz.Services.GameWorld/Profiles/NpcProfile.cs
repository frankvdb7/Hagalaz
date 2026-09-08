using AutoMapper;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

using Hagalaz.Cache.Types;
using GameNpcDefinition = Hagalaz.Services.GameWorld.Data.Model.NpcDefinition;

namespace Hagalaz.Services.GameWorld.Profiles
{
    public class NpcProfile : Profile
    {
        public NpcProfile()
        {
            CreateMap<NpcType, GameNpcDefinition>(MemberList.None)
                .ConstructUsing(source => new GameNpcDefinition(source.Id))
                .ForMember(destination => destination.DisplayName, options => options.MapFrom(source => source.Name));
            CreateMap<NpcType, INpcDefinition>().As<GameNpcDefinition>();

            CreateProjection<NpcSpawn, NpcSpawnDto>()
                .ForMember(dto => dto.NpcId, opt => opt.MapFrom(src => (int)src.NpcId))
                .ForMember(dto => dto.Location, opt => opt.MapFrom(src => new Location(src.CoordX, src.CoordY, src.CoordZ, 0)))
                .ForMember(dto => dto.MinimumBounds, opt => opt.MapFrom(src => new Location(src.MinCoordX, src.MinCoordY, src.MinCoordZ, 0)))
                .ForMember(dto => dto.MaximumBounds, opt => opt.MapFrom(src => new Location(src.MaxCoordX, src.MaxCoordY, src.MaxCoordZ, 0)))
                .ForMember(dto => dto.SpawnDirection, opt => opt.MapFrom(src => (int?)src.SpawnDirection));
        }
    }
}
