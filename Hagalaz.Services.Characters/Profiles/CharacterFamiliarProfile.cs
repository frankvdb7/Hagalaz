using AutoMapper;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Characters.Services.Model;

namespace Hagalaz.Services.Characters.Profiles
{
    public class CharacterFamiliarProfile : Profile
    {
        public CharacterFamiliarProfile()
        {
            CreateMap<CharactersFamiliar, Familiar>()
                .ForMember(dest => dest.FamiliarId, opt => opt.MapFrom(src => (int)src.FamiliarId))
                .ForMember(dest => dest.SpecialMovePoints, opt => opt.MapFrom(src => (int)src.SpecialMovePoints))
                .ForMember(dest => dest.TicksRemaining, opt => opt.MapFrom(src => (int)src.TicksRemaining))
                .ForMember(dest => dest.IsUsingSpecialMove, opt => opt.MapFrom(src => src.UsingSpecialMove == 1));
            CreateMap<Familiar, FamiliarDto>();
        }
    }
}
