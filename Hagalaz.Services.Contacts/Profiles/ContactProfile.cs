using AutoMapper;
using Hagalaz.Configuration;
using Hagalaz.Contacts.Messages;
using Hagalaz.Contacts.Messages.Model;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Contacts.Services.Model;
using Hagalaz.Text.Json;
using ContactDto = Hagalaz.Services.Contacts.Services.Model.ContactDto;
using ContactSettingsDto = Hagalaz.Services.Contacts.Services.Model.ContactSettingsDto;
using Model_ContactDto = Hagalaz.Services.Contacts.Services.Model.ContactDto;
using Model_ContactSettingsDto = Hagalaz.Services.Contacts.Services.Model.ContactSettingsDto;

namespace Hagalaz.Services.Contacts.Profiles
{
    public class ContactProfile : Profile
    {
        public ContactProfile()
        {
            CreateMap<CharactersContact, Model_ContactDto>()
                .ForMember(dto => dto.MasterId, opt => opt.MapFrom(src => src.ContactId))
                .ForMember(dto => dto.Rank, opt => opt.MapFrom(src => (FriendsChatRank?)src.FcRank))
                .ForMember(dto => dto.DisplayName, opt => opt.MapFrom(src => src.Contact.DisplayName))
                .ForMember(dto => dto.PreviousDisplayName, opt => opt.MapFrom(src => src.Contact.PreviousDisplayName))
                .ForMember(dto => dto.WorldId, opt => opt.Ignore())
                .ForMember(dto => dto.WorldName, opt => opt.Ignore())
                .ForMember(dto => dto.AreMutualFriends, opt => opt.Ignore())
                .ForMember(dto => dto.Settings, opt => opt.Ignore());

            CreateMap<Model_ContactSettingsDto.AvailabilitySettingsDto, byte>().ConstructUsing(source => MapAvailabilityDtoToFlag(source));
            CreateMap<byte, Model_ContactSettingsDto.AvailabilitySettingsDto>().ConvertUsing(source => MapFlagToAvailabilityDto(source));
            CreateMap<CharactersProfile, Model_ContactSettingsDto>().ConvertUsing(source => MapProfileToContactSettings(source));

            CreateMap<Model_ContactDto, Hagalaz.Contacts.Messages.Model.ContactDto>()
                .ForMember(dest => dest.WorldName, opt => opt.Ignore());
            CreateMap<Model_ContactSettingsDto, Hagalaz.Contacts.Messages.Model.ContactSettingsDto>();
            CreateMap<Model_ContactSettingsDto.AvailabilitySettingsDto, ContactAvailability>()
                .ConvertUsing(src => MapAvailabilityDtoToEnum(src));

            CreateMap<Hagalaz.Contacts.Messages.Model.ContactSettingsDto, Model_ContactSettingsDto>()
                .ForMember(dest => dest.Availability, opt => opt.MapFrom(src => src.Availability));
            CreateMap<ContactAvailability, Model_ContactSettingsDto.AvailabilitySettingsDto>()
                .ConvertUsing(src => MapContactAvailabilityToSettingsDto(src));

            CreateMap<CharacterDto, Hagalaz.Contacts.Messages.Model.ContactDto>()
                .ForMember(dest => dest.Rank, opt => opt.Ignore())
                .ForMember(dest => dest.WorldId, opt => opt.Ignore())
                .ForMember(dest => dest.WorldName, opt => opt.Ignore())
                .ForMember(dest => dest.AreMutualFriends, opt => opt.Ignore())
                .ForMember(dest => dest.Settings, opt => opt.Ignore());

            CreateMap<CharacterDto, ContactMessageNotification.SenderDto>()
                .ForMember(dest => dest.Claims, opt => opt.MapFrom(src => src.Claims));
            CreateMap<CharacterDto.ClaimDto, ContactMessageNotification.ClaimDto>();
        }

        private static Model_ContactSettingsDto MapProfileToContactSettings(CharactersProfile profile)
        {
            var jsonDictionary = new JsonDictionary(ProfileConstants.DefaultOptions);
            jsonDictionary.FromJson(profile.Data);
            var setting = jsonDictionary.GetValue<byte>(ProfileConstants.ChatSettingsFriendsFilter);
            return new Model_ContactSettingsDto
            {
                Availability = MapFlagToAvailabilityDto(setting)
            };
        }

        private static Model_ContactSettingsDto.AvailabilitySettingsDto MapContactAvailabilityToSettingsDto(
            ContactAvailability availability) =>
            new Model_ContactSettingsDto.AvailabilitySettingsDto()
            {
                Everyone = availability == ContactAvailability.Everyone,
                Friends = availability == ContactAvailability.Friends,
                Off = availability == ContactAvailability.Off
            };

        private static ContactAvailability MapAvailabilityDtoToEnum(Model_ContactSettingsDto.AvailabilitySettingsDto settings)
        {
            if (settings.Everyone)
            {
                return ContactAvailability.Everyone;
            }

            return settings.Friends ? ContactAvailability.Friends : ContactAvailability.Off;
        }

        private static Model_ContactSettingsDto.AvailabilitySettingsDto MapFlagToAvailabilityDto(byte friendsFlag) =>
            new()
            {
                Everyone = friendsFlag == 0, Friends = friendsFlag == 1, Off = friendsFlag == 2
            };

        private static byte MapAvailabilityDtoToFlag(Model_ContactSettingsDto.AvailabilitySettingsDto availabilitySettingsDto)
        {
            if (availabilitySettingsDto.Everyone)
            {
                return 0;
            }

            if (availabilitySettingsDto.Friends)
            {
                return 1;
            }

            return 2;
        }
    }
}