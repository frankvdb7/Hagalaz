using AutoMapper;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Characters.Services.Model;

namespace Hagalaz.Services.Characters.Profiles
{
    public class CharacterSlayerProfile : Profile
    {
        public CharacterSlayerProfile()
        {
            CreateMap<CharactersSlayerTask, Slayer.SlayerTask>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => (int)src.SlayerTaskId))
                .ForMember(dest => dest.KillCount, opt => opt.MapFrom(src => (int)src.KillCount));
            CreateMap<Slayer, SlayerDto>();
            CreateMap<Slayer.SlayerTask, SlayerDto.SlayerTaskDto>();
        }
    }
}
