using AutoMapper;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Services.GameWorld.Profiles
{
    public class MusicProfile : Profile
    {
        public MusicProfile()
        {
            CreateMap<MusicDefinition, MusicDto>();
        }
    }
}
