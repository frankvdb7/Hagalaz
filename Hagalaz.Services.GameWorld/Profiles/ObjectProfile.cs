using AutoMapper;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Services.GameWorld.Profiles
{
    public class ObjectProfile : Profile
    {
        public ObjectProfile()
        {
            CreateProjection<GameobjectSpawn, GameObjectSpawnDto>()
                .ForMember(dto => dto.ObjectId, opt => opt.MapFrom(x => (int)x.GameobjectId))
                .ForMember(dto => dto.ShapeType, opt => opt.MapFrom(x => (ShapeType)x.Type))
                .ForMember(dto => dto.Rotation, opt => opt.MapFrom(x => (int)x.Face))
                .ForMember(dto => dto.Location, opt => opt.MapFrom(x => new Location(x.CoordX, x.CoordY, x.CoordZ, 0)));
            CreateMap<GameobjectLodestone, LodestoneDto>()
                .ForMember(dto => dto.ComponentId, opt => opt.MapFrom(src => (int)src.ButtonId))
                .ForMember(dto => dto.GameObjectId, opt => opt.MapFrom(src => (int)src.GameobjectId))
                .ForMember(dto => dto.StateId, opt => opt.MapFrom(src => src.StateId.ToString()))
                .ForMember(dto => dto.CoordX, opt => opt.MapFrom(src => (int)src.CoordX))
                .ForMember(dto => dto.CoordY, opt => opt.MapFrom(src => (int)src.CoordY))
                .ForMember(dto => dto.CoordZ, opt => opt.MapFrom(src => (int)src.CoordZ));
        }
    }
}
