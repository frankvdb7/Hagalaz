using System;
using System.Globalization;
using System.Linq;
using AutoMapper;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Profiles
{
    /// <summary>
    /// Maps the full character snapshot message into the persistence entities.
    /// </summary>
    public sealed class CharacterUpdateProfile : Profile
    {
        public CharacterUpdateProfile()
        {
            CreateMap<AppearanceDto, CharactersLook>()
                .ForMember(dest => dest.MasterId, opt => opt.Ignore())
                .ForMember(dest => dest.Master, opt => opt.Ignore());

            CreateMap<StatisticsDto, CharactersStatistic>()
                .ForMember(dest => dest.MasterId, opt => opt.Ignore())
                .ForMember(dest => dest.Master, opt => opt.Ignore())
                .ForMember(dest => dest.XpCounters, opt => opt.MapFrom(src => string.Join(',', src.XpCounters)))
                .ForMember(dest => dest.TrackedXpCounters, opt => opt.MapFrom(src => string.Join(',', src.TrackedXpCounters)))
                .ForMember(dest => dest.EnabledXpCounters, opt => opt.MapFrom(src => string.Join(',', src.EnabledXpCounters.Select(value => value ? 1 : 0))))
                .ForMember(dest => dest.TargetSkillLevels, opt => opt.MapFrom(src => string.Join(',', src.TargetSkillLevels)))
                .ForMember(dest => dest.TargetSkillExperiences, opt => opt.MapFrom(src => string.Join(',', src.TargetSkillExperiences.Select(value => value.ToString(CultureInfo.InvariantCulture)))));

            CreateMap<FamiliarDto, CharactersFamiliar>()
                .ForMember(dest => dest.MasterId, opt => opt.Ignore())
                .ForMember(dest => dest.UsingSpecialMove, opt => opt.MapFrom(src => src.IsUsingSpecialMove ? (byte)1 : (byte)0))
                .ForMember(dest => dest.Master, opt => opt.Ignore())
                .ForMember(dest => dest.Familiar, opt => opt.Ignore());

            CreateMap<MusicDto, CharactersMusic>()
                .ForMember(dest => dest.MasterId, opt => opt.Ignore())
                .ForMember(dest => dest.UnlockedMusic, opt => opt.MapFrom(src => string.Join(',', src.UnlockedMusicIds)))
                .ForMember(dest => dest.Master, opt => opt.Ignore());

            CreateMap<MusicDto, CharactersMusicPlaylist>()
                .ForMember(dest => dest.MasterId, opt => opt.Ignore())
                .ForMember(dest => dest.Playlist, opt => opt.MapFrom(src => string.Join(',', src.PlaylistMusicIds)))
                .ForMember(dest => dest.PlaylistToggled, opt => opt.MapFrom(src => src.IsPlaylistToggled ? (byte)1 : (byte)0))
                .ForMember(dest => dest.ShuffleToggled, opt => opt.MapFrom(src => src.IsShuffleToggled ? (byte)1 : (byte)0))
                .ForMember(dest => dest.Master, opt => opt.Ignore());

            CreateMap<FarmingDto.PatchDto, CharactersFarmingPatch>()
                .ForMember(dest => dest.MasterId, opt => opt.Ignore())
                .ForMember(dest => dest.PatchId, opt => opt.MapFrom(src => checked((uint)src.Id)))
                .ForMember(dest => dest.SeedId, opt => opt.MapFrom(src => checked((ushort)src.SeedId)))
                .ForMember(dest => dest.ConditionFlag, opt => opt.MapFrom(src => checked((uint)src.Condition)))
                .ForMember(dest => dest.CurrentCycle, opt => opt.MapFrom(src => checked((byte)src.CurrentCycle)))
                .ForMember(dest => dest.CurrentCycleTicks, opt => opt.MapFrom(src => checked((uint)src.CurrentCycleTicks)))
                .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => checked((uint)src.ProductCount)))
                .ForMember(dest => dest.Master, opt => opt.Ignore())
                .ForMember(dest => dest.Patch, opt => opt.Ignore())
                .ForMember(dest => dest.Seed, opt => opt.Ignore());

            CreateMap<ItemDto, CharactersItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MasterId, opt => opt.Ignore())
                .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => checked((ushort)src.ItemId)))
                .ForMember(dest => dest.SlotId, opt => opt.MapFrom(src => checked((ushort)src.SlotId)))
                .ForMember(dest => dest.ContainerType, opt => opt.Ignore())
                .ForMember(dest => dest.Master, opt => opt.Ignore());

            CreateMap<ItemAppearanceDto, CharactersItemsLook>()
                .ForMember(dest => dest.MasterId, opt => opt.Ignore())
                .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => checked((ushort)src.Id)))
                .ForMember(dest => dest.MaleWornModel1, opt => opt.MapFrom(src => src.MaleModels[0]))
                .ForMember(dest => dest.MaleWornModel2, opt => opt.MapFrom(src => src.MaleModels[1]))
                .ForMember(dest => dest.MaleWornModel3, opt => opt.MapFrom(src => src.MaleModels[2]))
                .ForMember(dest => dest.FemaleWornModel1, opt => opt.MapFrom(src => src.FemaleModels[0]))
                .ForMember(dest => dest.FemaleWornModel2, opt => opt.MapFrom(src => src.FemaleModels[1]))
                .ForMember(dest => dest.FemaleWornModel3, opt => opt.MapFrom(src => src.FemaleModels[2]))
                .ForMember(dest => dest.ModelColours, opt => opt.MapFrom(src => string.Join(',', src.ModelColors)))
                .ForMember(dest => dest.TextureColours, opt => opt.MapFrom(src => string.Join(',', src.TextureColors)))
                .ForMember(dest => dest.Master, opt => opt.Ignore());

            CreateMap<NotesDto.NoteDto, CharactersNote>()
                .ForMember(dest => dest.MasterId, opt => opt.Ignore())
                .ForMember(dest => dest.NoteId, opt => opt.MapFrom(src => checked((byte)src.Id)))
                .ForMember(dest => dest.Colour, opt => opt.MapFrom(src => checked((byte)src.Color)))
                .ForMember(dest => dest.Master, opt => opt.Ignore());

            CreateMap<ProfileDto, CharactersProfile>()
                .ForMember(dest => dest.MasterId, opt => opt.Ignore())
                .ForMember(dest => dest.Data, opt => opt.MapFrom(src => src.JsonData))
                .ForMember(dest => dest.Master, opt => opt.Ignore());

            CreateMap<SlayerDto.SlayerTaskDto, CharactersSlayerTask>()
                .ForMember(dest => dest.MasterId, opt => opt.Ignore())
                .ForMember(dest => dest.SlayerTaskId, opt => opt.MapFrom(src => checked((ushort)src.Id)))
                .ForMember(dest => dest.KillCount, opt => opt.MapFrom(src => checked((uint)src.KillCount)))
                .ForMember(dest => dest.Master, opt => opt.Ignore())
                .ForMember(dest => dest.SlayerTask, opt => opt.Ignore());

            CreateMap<StateDto.StateExDto, CharactersState>()
                .ForMember(dest => dest.MasterId, opt => opt.Ignore())
                .ForMember(dest => dest.StateId, opt => opt.MapFrom(src => src.Id.ToString(CultureInfo.InvariantCulture)))
                .ForMember(dest => dest.Master, opt => opt.Ignore());
        }
    }
}
