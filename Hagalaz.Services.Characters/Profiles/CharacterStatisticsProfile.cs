using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Characters.Model;
using Hagalaz.Services.Characters.Services.Model;
using Hagalaz.Utilities;

namespace Hagalaz.Services.Characters.Profiles
{
    public class CharacterStatisticsProfile : Profile
    {
        public CharacterStatisticsProfile()
        {
            CreateMap<CharactersStatistic, CharactersStatisticsDto>()
                .ForMember(dto => dto.DisplayName, opt => opt.MapFrom(src => src.Master.DisplayName))
                .ForMember(dto => dto.OverallExperience, opt => opt.Ignore())
                .ForMember(dto => dto.OverallLevel, opt => opt.Ignore());

            CreateMap<CharactersStatistic, Statistics>()
                .ForMember(dest => dest.AttackLevel, opt => opt.MapFrom(src => (int)src.AttackLevel))
                .ForMember(dest => dest.AttackExp, opt => opt.MapFrom(src => src.AttackExp))
                .ForMember(dest => dest.DefenceLevel, opt => opt.MapFrom(src => (int)src.DefenceLevel))
                .ForMember(dest => dest.DefenceExp, opt => opt.MapFrom(src => src.DefenceExp))
                .ForMember(dest => dest.StrengthLevel, opt => opt.MapFrom(src => (int)src.StrengthLevel))
                .ForMember(dest => dest.StrengthExp, opt => opt.MapFrom(src => src.StrengthExp))
                .ForMember(dest => dest.ConstitutionLevel, opt => opt.MapFrom(src => (int)src.ConstitutionLevel))
                .ForMember(dest => dest.ConstitutionExp, opt => opt.MapFrom(src => src.ConstitutionExp))
                .ForMember(dest => dest.RangeLevel, opt => opt.MapFrom(src => (int)src.RangeLevel))
                .ForMember(dest => dest.RangeExp, opt => opt.MapFrom(src => src.RangeExp))
                .ForMember(dest => dest.PrayerLevel, opt => opt.MapFrom(src => (int)src.PrayerLevel))
                .ForMember(dest => dest.PrayerExp, opt => opt.MapFrom(src => src.PrayerExp))
                .ForMember(dest => dest.MagicLevel, opt => opt.MapFrom(src => (int)src.MagicLevel))
                .ForMember(dest => dest.MagicExp, opt => opt.MapFrom(src => src.MagicExp))
                .ForMember(dest => dest.CookingLevel, opt => opt.MapFrom(src => (int)src.CookingLevel))
                .ForMember(dest => dest.CookingExp, opt => opt.MapFrom(src => src.CookingExp))
                .ForMember(dest => dest.WoodcuttingLevel, opt => opt.MapFrom(src => (int)src.WoodcuttingLevel))
                .ForMember(dest => dest.WoodcuttingExp, opt => opt.MapFrom(src => src.WoodcuttingExp))
                .ForMember(dest => dest.FletchingLevel, opt => opt.MapFrom(src => (int)src.FletchingLevel))
                .ForMember(dest => dest.FletchingExp, opt => opt.MapFrom(src => src.FletchingExp))
                .ForMember(dest => dest.FishingLevel, opt => opt.MapFrom(src => (int)src.FishingLevel))
                .ForMember(dest => dest.FishingExp, opt => opt.MapFrom(src => src.FishingExp))
                .ForMember(dest => dest.FiremakingLevel, opt => opt.MapFrom(src => (int)src.FiremakingLevel))
                .ForMember(dest => dest.FiremakingExp, opt => opt.MapFrom(src => src.FiremakingExp))
                .ForMember(dest => dest.CraftingLevel, opt => opt.MapFrom(src => (int)src.CraftingLevel))
                .ForMember(dest => dest.CraftingExp, opt => opt.MapFrom(src => src.CraftingExp))
                .ForMember(dest => dest.SmithingLevel, opt => opt.MapFrom(src => (int)src.SmithingLevel))
                .ForMember(dest => dest.SmithingExp, opt => opt.MapFrom(src => src.SmithingExp))
                .ForMember(dest => dest.MiningLevel, opt => opt.MapFrom(src => (int)src.MiningLevel))
                .ForMember(dest => dest.MiningExp, opt => opt.MapFrom(src => src.MiningExp))
                .ForMember(dest => dest.HerbloreLevel, opt => opt.MapFrom(src => (int)src.HerbloreLevel))
                .ForMember(dest => dest.HerbloreExp, opt => opt.MapFrom(src => src.HerbloreExp))
                .ForMember(dest => dest.AgilityLevel, opt => opt.MapFrom(src => (int)src.AgilityLevel))
                .ForMember(dest => dest.AgilityExp, opt => opt.MapFrom(src => src.AgilityExp))
                .ForMember(dest => dest.ThievingLevel, opt => opt.MapFrom(src => (int)src.ThievingLevel))
                .ForMember(dest => dest.ThievingExp, opt => opt.MapFrom(src => src.ThievingExp))
                .ForMember(dest => dest.SlayerLevel, opt => opt.MapFrom(src => (int)src.SlayerLevel))
                .ForMember(dest => dest.SlayerExp, opt => opt.MapFrom(src => src.SlayerExp))
                .ForMember(dest => dest.FarmingLevel, opt => opt.MapFrom(src => (int)src.FarmingLevel))
                .ForMember(dest => dest.FarmingExp, opt => opt.MapFrom(src => src.FarmingExp))
                .ForMember(dest => dest.RunecraftingLevel, opt => opt.MapFrom(src => (int)src.RunecraftingLevel))
                .ForMember(dest => dest.RunecraftingExp, opt => opt.MapFrom(src => src.RunecraftingExp))
                .ForMember(dest => dest.ConstructionLevel, opt => opt.MapFrom(src => (int)src.ConstructionLevel))
                .ForMember(dest => dest.ConstructionExp, opt => opt.MapFrom(src => src.ConstructionExp))
                .ForMember(dest => dest.HunterLevel, opt => opt.MapFrom(src => (int)src.HunterLevel))
                .ForMember(dest => dest.HunterExp, opt => opt.MapFrom(src => src.HunterExp))
                .ForMember(dest => dest.SummoningLevel, opt => opt.MapFrom(src => (int)src.SummoningLevel))
                .ForMember(dest => dest.SummoningExp, opt => opt.MapFrom(src => src.SummoningExp))
                .ForMember(dest => dest.DungeoneeringLevel, opt => opt.MapFrom(src => (int)src.DungeoneeringLevel))
                .ForMember(dest => dest.DungeoneeringExp, opt => opt.MapFrom(src => src.DungeoneeringExp))
                .ForMember(dest => dest.LifePoints, opt => opt.MapFrom(src => (int)src.LifePoints))
                .ForMember(dest => dest.PrayerPoints, opt => opt.MapFrom(src => (int)src.PrayerPoints))
                .ForMember(dest => dest.RunEnergy, opt => opt.MapFrom(src => (int)src.RunEnergy))
                .ForMember(dest => dest.SpecialEnergy, opt => opt.MapFrom(src => (int)src.SpecialEnergy))
                .ForMember(dest => dest.PoisonAmount, opt => opt.MapFrom(src => (int)src.PoisonAmount))
                .ForMember(dest => dest.PlayTime, opt => opt.MapFrom(src => (long)src.PlayTime))
                .ForMember(dest => dest.XpCounters, opt => opt.MapFrom(src => StringUtilities.SelectIntFromString(src.XpCounters).ToArray()))
                .ForMember(dest => dest.TrackedXpCounters, opt => opt.MapFrom(src => StringUtilities.SelectIntFromString(src.TrackedXpCounters).ToArray()))
                .ForMember(dest => dest.EnabledXpCounters, opt => opt.MapFrom(src => StringUtilities.SelectBoolFromString(src.EnabledXpCounters).ToArray()))
                .ForMember(dest => dest.TargetSkillLevels, opt => opt.MapFrom(src => StringUtilities.SelectIntFromString(src.TargetSkillLevels).ToArray()))
                .ForMember(dest => dest.TargetSkillExperiences, opt => opt.MapFrom(src => StringUtilities.SelectDoubleFromString(src.TargetSkillExperiences).ToArray()));
            CreateMap<Statistics, StatisticsDto>();
            CreateMap<CharactersStatistic, CharacterStatisticCollectionDto>()
                .ForMember(dest => dest.DisplayName, opt => opt.Ignore())
                .ForMember(dest => dest.Statistics, opt => opt.Ignore())
                .ConvertUsing(src => new CharacterStatisticCollectionDto
                {
                    DisplayName = src.Master.DisplayName,
                    Statistics = new List<CharacterStatisticDetailDto>
                    {
                        new() { Type = CharacterStatisticType.Attack, Level = src.AttackLevel, Experience = src.AttackExp },
                        new() { Type = CharacterStatisticType.Defence, Level = src.DefenceLevel, Experience = src.DefenceExp },
                        new() { Type = CharacterStatisticType.Strength, Level = src.StrengthLevel, Experience = src.StrengthExp },
                        new() { Type = CharacterStatisticType.Constitution, Level = src.ConstitutionLevel, Experience = src.ConstitutionExp },
                        new() { Type = CharacterStatisticType.Range, Level = src.RangeLevel, Experience = src.RangeExp },
                        new() { Type = CharacterStatisticType.Prayer, Level = src.PrayerLevel, Experience = src.PrayerExp },
                        new() { Type = CharacterStatisticType.Magic, Level = src.MagicLevel, Experience = src.MagicExp },
                        new() { Type = CharacterStatisticType.Cooking, Level = src.CookingLevel, Experience = src.CookingExp },
                        new() { Type = CharacterStatisticType.Woodcutting, Level = src.WoodcuttingLevel, Experience = src.WoodcuttingExp },
                        new() { Type = CharacterStatisticType.Fletching, Level = src.FletchingLevel, Experience = src.FletchingExp },
                        new() { Type = CharacterStatisticType.Fishing, Level = src.FishingLevel, Experience = src.FishingExp },
                        new() { Type = CharacterStatisticType.Firemaking, Level = src.FiremakingLevel, Experience = src.FiremakingExp },
                        new() { Type = CharacterStatisticType.Crafting, Level = src.CraftingLevel, Experience = src.CraftingExp },
                        new() { Type = CharacterStatisticType.Smithing, Level = src.SmithingLevel, Experience = src.SmithingExp },
                        new() { Type = CharacterStatisticType.Mining, Level = src.MiningLevel, Experience = src.MiningExp },
                        new() { Type = CharacterStatisticType.Herblore, Level = src.HerbloreLevel, Experience = src.HerbloreExp },
                        new() { Type = CharacterStatisticType.Agility, Level = src.AgilityLevel, Experience = src.AgilityExp },
                        new() { Type = CharacterStatisticType.Thieving, Level = src.ThievingLevel, Experience = src.ThievingExp },
                        new() { Type = CharacterStatisticType.Slayer, Level = src.SlayerLevel, Experience = src.SlayerExp },
                        new() { Type = CharacterStatisticType.Farming, Level = src.FarmingLevel, Experience = src.FarmingExp },
                        new() { Type = CharacterStatisticType.Runecrafting, Level = src.RunecraftingLevel, Experience = src.RunecraftingExp },
                        new() { Type = CharacterStatisticType.Construction, Level = src.ConstructionLevel, Experience = src.ConstructionExp },
                        new() { Type = CharacterStatisticType.Hunter, Level = src.HunterLevel, Experience = src.HunterExp },
                        new() { Type = CharacterStatisticType.Summoning, Level = src.SummoningLevel, Experience = src.SummoningExp },
                        new() { Type = CharacterStatisticType.Dungeoneering, Level = src.DungeoneeringLevel, Experience = src.DungeoneeringExp }
                    }
                });
        }
    }
}