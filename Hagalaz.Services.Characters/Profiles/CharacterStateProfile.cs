using AutoMapper;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Characters.Services.Model;

namespace Hagalaz.Services.Characters.Profiles
{
    public class CharacterStateProfile : Profile
    {
        public CharacterStateProfile()
        {
            CreateMap<CharactersState, State.StateEx>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.StateId))
                .ForMember(dest => dest.TicksLeft, opt => opt.MapFrom(src => src.TicksLeft));
            CreateMap<Services.Model.State, StateDto>();
            CreateMap<Services.Model.State.StateEx, StateDto.StateExDto>();
        }
    }
}
