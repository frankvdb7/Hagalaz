using AutoMapper;
using Hagalaz.Data.Entities;
using Hagalaz.Services.GameLogon.Services.Model;

namespace Hagalaz.Services.GameLogon.Profiles
{
    public class CharacterProfile : Profile
    {
        public CharacterProfile()
        {
            // preferences
            CreateMap<CharactersPreference, FriendsChatSettingsDto>()
                .ForMember(dto => dto.ChatName, opt => opt.MapFrom(src => src.FcName))
                .ForMember(dto => dto.RankEnter, opt => opt.MapFrom(src => src.FcRankEnter))
                .ForMember(dto => dto.RankKick, opt => opt.MapFrom(src => src.FcRankKick))
                .ForMember(dto => dto.RankTalk, opt => opt.MapFrom(src => src.FcRankTalk));
        }
    }
}