using AutoMapper;
using Hagalaz.Game.Messages.Protocol;


namespace Hagalaz.Services.GameWorld.Profiles
{
    public class WorldProfile : Profile
    {
        public WorldProfile()
        {
            CreateMap<Services.Model.WorldLocationInfo, SetWorldInfoMessage.WorldLocationInfoDto>();
            CreateMap<Services.Model.WorldSettingsInfo, SetWorldInfoMessage.WorldSettingsInfoDto>();
            CreateMap<Services.Model.WorldInfo, SetWorldInfoMessage.WorldInfoDto>();
            CreateMap<Services.Model.WorldCharacterInfo, SetWorldInfoMessage.WorldCharacterInfoDto>();

            CreateMap<Services.Model.WorldInfo, Store.Model.WorldInfo>()
                .ForMember(dest => dest.CharacterCount, opt => opt.Ignore())
                .ForMember(dest => dest.Online, opt => opt.Ignore());
            CreateMap<Services.Model.WorldSettingsInfo, Store.Model.WorldInfo.WorldSettingsInfo>();
            CreateMap<Services.Model.WorldLocationInfo, Store.Model.WorldInfo.WorldLocationInfo>();

            CreateMap<Store.Model.WorldInfo, Services.Model.WorldInfo>();
            CreateMap<Store.Model.WorldInfo.WorldSettingsInfo, Services.Model.WorldSettingsInfo>();
            CreateMap<Store.Model.WorldInfo.WorldLocationInfo, Services.Model.WorldLocationInfo>();

            CreateMap<Store.Model.WorldInfo, Services.Model.WorldCharacterInfo>();

            CreateMap<Game.Messages.WorldOnlineMessage, Services.Model.WorldInfo>();
            CreateMap<Game.Messages.WorldOnlineMessage.WorldLocation, Services.Model.WorldLocationInfo>();
            CreateMap<Game.Messages.WorldOnlineMessage.WorldSettings, Services.Model.WorldSettingsInfo>();

            CreateMap<Hagalaz.Data.Entities.World, Game.Messages.WorldOnlineMessage.WorldSettings>()
                .ForMember(dest => dest.IsMembersOnly, opt => opt.MapFrom(src => src.MembersOnly == 1))
                .ForMember(dest => dest.IsHighLighted, opt => opt.MapFrom(src => src.Highlight == 1))
                .ForMember(dest => dest.IsPvP, opt => opt.MapFrom(src => src.HighRisk == 1))
                .ForMember(dest => dest.IsQuickChatEnabled, opt => opt.MapFrom(src => src.QuickChatAllowed == 1))
                .ForMember(dest => dest.IsLootShareEnabled, opt => opt.MapFrom(src => src.LootShareAllowed == 1));

            CreateMap<Hagalaz.Data.Entities.World, Game.Messages.WorldOnlineMessage.WorldLocation>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => "Local"))
                .ForMember(dest => dest.Flag, opt => opt.MapFrom(src => src.Country));
        }
    }
}
