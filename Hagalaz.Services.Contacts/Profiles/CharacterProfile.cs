using AutoMapper;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Contacts.Services.Model;

namespace Hagalaz.Services.Contacts.Profiles
{
    public class CharacterProfile : Profile
    {
        public CharacterProfile()
        {
            CreateProjection<Character, CharacterDto>()
                .ForMember(dto => dto.MasterId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dto => dto.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
                .ForMember(dto => dto.PreviousDisplayName, opt => opt.MapFrom(src => src.PreviousDisplayName))
                .ForMember(dto => dto.Claims, opt => opt.Ignore());
            CreateProjection<CharactersPermission, CharacterDto.ClaimDto>()
                .ForMember(dto => dto.Name, opt => opt.MapFrom(src => src.Permission.ToString()));
        }
    }
}
