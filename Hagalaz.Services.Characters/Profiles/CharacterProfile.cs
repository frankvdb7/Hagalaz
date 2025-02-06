using AutoMapper;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Characters.Services.Model;

namespace Hagalaz.Services.Characters.Profiles
{
    public class CharacterProfile : Profile
    {
        public CharacterProfile()
        {
            CreateMap<Character, Details>()
                .ForMember(dest => dest.CoordX, opt => opt.MapFrom(src => (int)src.CoordX))
                .ForMember(dest => dest.CoordY, opt => opt.MapFrom(src => (int)src.CoordY))
                .ForMember(dest => dest.CoordZ, opt => opt.MapFrom(src => (int)src.CoordZ));
            CreateMap<Details, DetailsDto>();

            CreateMap<CharactersProfile, ProfileModel>()
                .ForMember(dest => dest.JsonData, opt => opt.MapFrom(src => src.Data));
            CreateMap<ProfileModel, ProfileDto>();
        }
    }
}
