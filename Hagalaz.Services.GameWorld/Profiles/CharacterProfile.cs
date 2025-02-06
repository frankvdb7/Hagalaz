using System.Linq;
using AutoMapper;
using Hagalaz.Authorization.Constants;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Services.Model;
using OpenIddict.Abstractions;

namespace Hagalaz.Services.GameWorld.Profiles
{
    public class CharacterProfile : Profile
    {
        public CharacterProfile()
        {
            CreateMap<AppearanceDto, HydratedAppearanceDto>()
                .ForMember(dest => dest.DisplayTitle, opt => opt.MapFrom(src => (DisplayTitle)src.DisplayTitle));
            CreateMap<ItemAppearanceDto, HydratedItemAppearanceDto>();
            CreateMap<DetailsDto, HydratedDetailsDto>();
            CreateMap<StatisticsDto, HydratedStatisticsDto>();
            CreateMap<ItemCollectionDto, HydratedItemCollectionDto>();
            CreateMap<ItemAppearanceCollectionDto, HydratedItemAppearanceCollectionDto>();
            CreateMap<ItemDto, HydratedItemDto>();
            CreateMap<FamiliarDto, HydratedFamiliarDto>();
            CreateMap<MusicDto, HydratedMusicDto>();
            CreateMap<FarmingDto, HydratedFarmingDto>();
            CreateMap<FarmingDto.PatchDto, HydratedFarmingDto.PatchDto>();
            CreateMap<SlayerDto, HydratedSlayerDto>();
            CreateMap<SlayerDto.SlayerTaskDto, HydratedSlayerDto.SlayerTaskDto>();
            CreateMap<NotesDto, HydratedNotesDto>();
            CreateMap<NotesDto.NoteDto, HydratedNotesDto.NoteDto>();
            CreateMap<ProfileDto, HydratedProfileDto>();
            CreateMap<StateDto, HydratedStateDto>();
            CreateMap<StateDto.StateExDto, HydratedStateDto.HydratedStateExDto>();
            CreateMap<CharacterHydrated, CharacterModel>()
                .ForMember(dest => dest.Claims, opt => opt.Ignore());
            CreateMap<AuthenticationProperties, HydratedClaims>()
                .ForMember(dest => dest.Permissions,
                opt => opt.MapFrom(
                    src => src.GetClaimList<string>(OpenIddictConstants.Claims.Role)!
                    .Select(PermissionHelpers.ParseRole)
                    .Aggregate((permissions, permission) => permissions | permission)))
                .ForMember(dest => dest.PreviousDisplayName,
                opt => opt.MapFrom(src => src.GetClaim<string?>(Claims.PreviousPreferredUsername)))
                .ForMember(dest => dest.LastLogin,
                opt => opt.MapFrom(src => src.GetClaim<string>(Claims.LastLogin)))
                .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.GetClaim<string>(OpenIddictConstants.Claims.Username)))
                .ForMember(dest => dest.DisplayName,
                opt => opt.MapFrom(src => src.GetClaim<string>(OpenIddictConstants.Claims.PreferredUsername)));

            // reverse
            CreateMap<HydratedAppearanceDto, AppearanceDto>()
                .ForCtorParam("DisplayTitle", opt => opt.MapFrom(src => (int)src.DisplayTitle));
            CreateMap<HydratedItemAppearanceDto, ItemAppearanceDto>();
            CreateMap<HydratedItemAppearanceCollectionDto, ItemAppearanceCollectionDto>();
            CreateMap<HydratedDetailsDto, DetailsDto>();
            CreateMap<HydratedStatisticsDto, StatisticsDto>();
            CreateMap<HydratedItemCollectionDto, ItemCollectionDto>();
            CreateMap<HydratedItemDto, ItemDto>();
            CreateMap<HydratedFamiliarDto, FamiliarDto>();
            CreateMap<HydratedMusicDto, MusicDto>();
            CreateMap<HydratedFarmingDto, FarmingDto>();
            CreateMap<HydratedFarmingDto.PatchDto, FarmingDto.PatchDto>();
            CreateMap<HydratedSlayerDto, SlayerDto>();
            CreateMap<HydratedSlayerDto.SlayerTaskDto, SlayerDto.SlayerTaskDto>();
            CreateMap<HydratedNotesDto, NotesDto>();
            CreateMap<HydratedNotesDto.NoteDto, NotesDto.NoteDto>();
            CreateMap<HydratedProfileDto, ProfileDto>();
            CreateMap<HydratedStateDto, StateDto>();
            CreateMap<HydratedStateDto.HydratedStateExDto, StateDto.StateExDto>();
            CreateMap<CharacterModel, CharacterHydrated>()
                .ForMember(dest => dest.MasterId, opt => opt.Ignore())
                .ForMember(dest => dest.CorrelationId, opt => opt.Ignore());
            CreateMap<CharacterModel, DehydrateCharacter>()
                .ForMember(dest => dest.MasterId, opt => opt.Ignore())
                .ForMember(dest => dest.CorrelationId, opt => opt.Ignore());
        }
    }
}
