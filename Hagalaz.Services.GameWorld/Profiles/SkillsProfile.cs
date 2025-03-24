using System;
using System.Collections.Generic;
using AutoMapper;
using Hagalaz.Data.Entities;
using System.Linq;
using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Logic.Skills;
using Hagalaz.Utilities;

namespace Hagalaz.Services.GameWorld.Profiles
{
    public class SkillsProfile : Profile
    {
        public SkillsProfile()
        {
            CreateMap<SkillsHerbloreHerbDefinition, HerbDto>()
                .ForMember(dest => dest.CleanHerbId, opt => opt.MapFrom(src => (int)src.CleanItemId))
                .ForMember(dest => dest.GrimyHerbId, opt => opt.MapFrom(src => (int)src.GrimyItemId))
                .ForMember(dest => dest.CleanLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.CleanExperience, opt => opt.MapFrom(src => (double)src.Experience));
            CreateMap<SkillsPrayerDefinition, PrayerDto>()
                .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => (int)src.ItemId))
                .ForMember(dest => dest.Experience, opt => opt.MapFrom(src => (double)src.Experience))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => Enum.Parse<PrayerDtoType>(src.Type)));
            CreateMap<SkillsFiremakingDefinition, FiremakingDto>()
                .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => (int)src.ItemId))
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.Experience, opt => opt.MapFrom(src => (double)src.Experience))
                .ForMember(dest => dest.FireObjectId, opt => opt.MapFrom(src => (int)src.FireObjectId))
                .ForMember(dest => dest.Ticks, opt => opt.MapFrom(src => (int)src.Ticks));
            CreateMap<SkillsRunecraftingDefinition, RunecraftingDto>()
                .ForMember(dest => dest.AltarId, opt => opt.MapFrom(src => (int)src.AltarId))
                .ForMember(dest => dest.PortalId, opt => opt.MapFrom(src => (int)src.PortalId))
                .ForMember(dest => dest.RuinId, opt => opt.MapFrom(src => (int)src.RuinId))
                .ForMember(dest => dest.RiftId, opt => opt.MapFrom(src => (int)src.RiftId))
                .ForMember(dest => dest.RuneId, opt => opt.MapFrom(src => (int)src.RuneId))
                .ForMember(dest => dest.TiaraId, opt => opt.MapFrom(src => (int)src.TiaraId))
                .ForMember(dest => dest.TalismanId, opt => opt.MapFrom(src => (int)src.TalismanId))
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.Experience, opt => opt.MapFrom(src => (double)src.Experience))
                .ForMember(dest => dest.AltarLocation, opt => opt.MapFrom(src => ToLocation(StringUtilities.SelectIntFromString(src.AltarLocation).ToArray())))
                .ForMember(dest => dest.RuinLocation, opt => opt.MapFrom(src => ToLocation(StringUtilities.SelectIntFromString(src.RuinLocation).ToArray())))
                .ForMember(dest => dest.RiftLocation, opt => opt.MapFrom(src => ToLocation(StringUtilities.SelectIntFromString(src.RiftLocation).ToArray())))
                .ForMember(dest => dest.LevelCountMultipliers,
                    opt => opt.MapFrom(src => StringUtilities.SelectIntFromString(src.LevelCountMultipliers).ToArray()));
            CreateMap<SkillsCookingFoodDefinition, FoodDto>()
                .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => (int)src.ItemId))
                .ForMember(dest => dest.LeftItemId, opt => opt.MapFrom(src => (int)src.LeftItemId))
                .ForMember(dest => dest.EatingTime, opt => opt.MapFrom(src => (int)src.EatingTime))
                .ForMember(dest => dest.HealAmount, opt => opt.MapFrom(src => (int)src.HealAmount));
            CreateMap<SkillsCookingRawFoodDefinition, RawFoodDto>()
                .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => (int)src.ItemId))
                .ForMember(dest => dest.CookedItemId, opt => opt.MapFrom(src => (int)src.CookedItemId))
                .ForMember(dest => dest.BurntItemId, opt => opt.MapFrom(src => (int)src.BurntItemId))
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.StopBurningLevel, opt => opt.MapFrom(src => (int)src.StopBurningLevel))
                .ForMember(dest => dest.Experience, opt => opt.MapFrom(src => (double)src.Experience));
            CreateMap<SkillsMiningOreDefinition, OreDto>()
                .ForMember(dest => dest.Experience, opt => opt.MapFrom(src => (double)src.Experience))
                .ForMember(dest => dest.BaseHarvestChance, opt => opt.MapFrom(src => (double)src.BaseHarvestChance))
                .ForMember(dest => dest.RespawnTime, opt => opt.MapFrom(src => (double)src.RespawnTime))
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => (int)src.ItemId))
                .ForMember(dest => dest.ExhaustChance, opt => opt.MapFrom(src => (double)src.ExhaustChance));
            CreateMap<SkillsMiningPickaxeDefinition, PickaxeDto>()
                .ForMember(dest => dest.BaseHarvestChance, opt => opt.MapFrom(src => (double)src.BaseHarvestChance))
                .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => (int)src.ItemId))
                .ForMember(dest => dest.AnimationId, opt => opt.MapFrom(src => (int)src.AnimationId))
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => Enum.Parse<PickaxeType>(src.Type)));
            CreateMap<SkillsMiningRockDefinition, RockDto>()
                .ForMember(dest => dest.OreId, opt => opt.MapFrom(src => (int)src.OreId))
                .ForMember(dest => dest.RockId, opt => opt.MapFrom(src => (int)src.RockId))
                .ForMember(dest => dest.ExhaustRockId, opt => opt.MapFrom(src => (int)src.ExhaustRockId));
            CreateMap<SkillsMagicCombatDefinition, CombatSpellDto>()
                .ForMember(dest => dest.ButtonId, opt => opt.MapFrom(src => (int)src.ButtonId))
                .ForMember(dest => dest.BaseDamage, opt => opt.MapFrom(src => (int)src.BaseDamage))
                .ForMember(dest => dest.BaseExperience, opt => opt.MapFrom(src => (double)src.BaseExperience))
                .ForMember(dest => dest.ProjectileId, opt => opt.MapFrom(src => (int)src.ProjectileId))
                .ForMember(dest => dest.CastAnimationId, opt => opt.MapFrom(src => (int)src.CastAnimationId))
                .ForMember(dest => dest.CastGraphicId, opt => opt.MapFrom(src => (int)src.CastGraphicId))
                .ForMember(dest => dest.EndGraphicId, opt => opt.MapFrom(src => (int)src.EndGraphicId))
                .ForMember(dest => dest.AutoCastConfig, opt => opt.MapFrom(src => (int)src.AutocastConfig))
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.RequiredRunes, opt => opt.MapFrom(src => StringUtilities.SelectIntFromString(src.RequiredRunes).Cast<RuneType>()))
                .ForMember(dest => dest.RequiredRunesCounts, opt => opt.MapFrom(src => StringUtilities.SelectIntFromString(src.RequiredRunesCounts)));
            CreateMap<SkillsMagicEnchantDefinition, EnchantingSpellDto>()
                .ForMember(dest => dest.RequiredRunes, opt => opt.MapFrom(src => StringUtilities.SelectIntFromString(src.RequiredRunes).Cast<RuneType>()))
                .ForMember(dest => dest.RequiredRunesCounts, opt => opt.MapFrom(src => StringUtilities.SelectIntFromString(src.RequiredRunesCounts)))
                .ForMember(dest => dest.Experience, opt => opt.MapFrom(src => (double)src.Experience))
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.ButtonId, opt => opt.MapFrom(src => (int)src.ButtonId))
                .ForMember(dest => dest.GraphicId, opt => opt.MapFrom(src => (int)src.GraphicId));
            CreateMap<SkillsMagicEnchantProduct, EnchantingSpellProductDto>()
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => (int)src.ProductId))
                .ForMember(dest => dest.ButtonId, opt => opt.MapFrom(src => (int)src.ButtonId))
                .ForMember(dest => dest.ResourceId, opt => opt.MapFrom(src => (int)src.ResourceId));

            CreateMap<SkillsSummoningDefinition, SummoningDto>()
                .ForMember(dest => dest.NpcId, opt => opt.MapFrom(src => (int)src.NpcId))
                .ForMember(dest => dest.PouchId, opt => opt.MapFrom(src => (int)src.PouchId))
                .ForMember(dest => dest.ScrollId, opt => opt.MapFrom(src => (int)src.ScrollId))
                .ForMember(dest => dest.ConfigId, opt => opt.MapFrom(src => (int)src.ConfigId))
                .ForMember(dest => dest.SummonSpawnCost, opt => opt.MapFrom(src => (int)src.SummonSpawnCost))
                .ForMember(dest => dest.SummonLevel, opt => opt.MapFrom(src => (int)src.SummonLevel))
                .ForMember(dest => dest.SummonExperience, opt => opt.MapFrom(src => (double)src.SummonExperience))
                .ForMember(dest => dest.CreatePouchExperience, opt => opt.MapFrom(src => (double)src.CreatePouchExperience))
                .ForMember(dest => dest.ScrollExperience, opt => opt.MapFrom(src => (double)src.ScrollExperience))
                .ForMember(dest => dest.Ticks, opt => opt.MapFrom(src => (int)src.Ticks));

            CreateMap<SkillsWoodcuttingHatchetDefinition, HatchetDto>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => Enum.Parse<HatchetType>(src.Type)))
                .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => (int)src.ItemId))
                .ForMember(dest => dest.ChopAnimationId, opt => opt.MapFrom(src => (int)src.ChopAnimationId))
                .ForMember(dest => dest.CanoeAnimationId, opt => opt.MapFrom(src => (int)src.CanoueAnimationId))
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.BaseHarvestChance, opt => opt.MapFrom(src => (double)src.BaseHarvestChance));
            CreateMap<SkillsWoodcuttingLogDefinition, LogDto>()
                .ForMember(dest => dest.ItemID, opt => opt.MapFrom(src => (int)src.ItemId))
                .ForMember(dest => dest.RespawnTime, opt => opt.MapFrom(src => (double)src.RespawnTime))
                .ForMember(dest => dest.FallChance, opt => opt.MapFrom(src => (double)src.FallChance))
                .ForMember(dest => dest.BaseHarvestChance, opt => opt.MapFrom(src => (double)src.BaseHarvestChance))
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.WoodcuttingExperience, opt => opt.MapFrom(src => (double)src.Experience));
            CreateMap<SkillsWoodcuttingTreeDefinition, LogDto>()
                .ForMember(dest => dest.ItemID, opt => opt.MapFrom(src => (int)src.Log.ItemId))
                .ForMember(dest => dest.RespawnTime, opt => opt.MapFrom(src => (double)src.Log.RespawnTime))
                .ForMember(dest => dest.FallChance, opt => opt.MapFrom(src => (double)src.Log.FallChance))
                .ForMember(dest => dest.BaseHarvestChance, opt => opt.MapFrom(src => (double)src.Log.BaseHarvestChance))
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.Log.RequiredLevel))
                .ForMember(dest => dest.WoodcuttingExperience, opt => opt.MapFrom(src => (double)src.Log.Experience));
            CreateMap<SkillsWoodcuttingTreeDefinition, TreeDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => (int)src.TreeId))
                .ForMember(dest => dest.StumpId, opt => opt.MapFrom(src => (int)src.StumpId));

            CreateMap<SkillsCraftingGemDefinition, GemDto>()
                .ForMember(dest => dest.UncutGemID, opt => opt.MapFrom(src => (int)src.ResourceId))
                .ForMember(dest => dest.CutGemID, opt => opt.MapFrom(src => (int)src.ProductId))
                .ForMember(dest => dest.AnimationID, opt => opt.MapFrom(src => (int)src.AnimationId))
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.CraftingExperience, opt => opt.MapFrom(src => (double)src.Experience));
            CreateMap<SkillsCraftingJewelryDefinition, JewelryDto>();
            CreateMap<SkillsCraftingLeatherDefinition, LeatherDto>()
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => (int)src.ProductId))
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.ResourceID, opt => opt.MapFrom(src => (int)src.ResourceId))
                .ForMember(dest => dest.RequiredResourceCount, opt => opt.MapFrom(src => (int)src.RequiredResourceCount))
                .ForMember(dest => dest.Experience, opt => opt.MapFrom(src => (double)src.Experience));
            CreateMap<SkillsCraftingPotteryDefinition, PotteryDto>()
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.BakeExperience, opt => opt.MapFrom(src => (double)src.BakeExperience))
                .ForMember(dest => dest.FormExperience, opt => opt.MapFrom(src => (double)src.FormExperience))
                .ForMember(dest => dest.BakedProductID, opt => opt.MapFrom(src => (int)src.BakedProductId))
                .ForMember(dest => dest.FormedProductID, opt => opt.MapFrom(src => (int)src.FormedProductId));
            CreateMap<SkillsCraftingSilverDefinition, SilverDto>()
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.Experience, opt => opt.MapFrom(src => (double)src.Experience))
                .ForMember(dest => dest.ProductID, opt => opt.MapFrom(src => (int)src.ProductId))
                .ForMember(dest => dest.ChildID, opt => opt.MapFrom(src => (int)src.ChildId))
                .ForMember(dest => dest.MouldID, opt => opt.MapFrom(src => (int)src.MouldId));
            CreateMap<SkillsCraftingSpinDefinition, SpinDto>()
                .ForMember(dest => dest.ResourceID, opt => opt.MapFrom(src => (int)src.ResourceId))
                .ForMember(dest => dest.ProductID, opt => opt.MapFrom(src => (int)src.ProductId))
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.CraftingExperience, opt => opt.MapFrom(src => (double)src.Experience));
            CreateMap<SkillsCraftingTanDefinition, TanDto>()
                .ForMember(dest => dest.ProductID, opt => opt.MapFrom(src => (int)src.ProductId))
                .ForMember(dest => dest.ResourceID, opt => opt.MapFrom(src => (int)src.ResourceId))
                .ForMember(dest => dest.BasePrice, opt => opt.MapFrom(src => (int)src.BasePrice));

            CreateMap<SkillsFarmingPatchDefinition, PatchDto>()
                .ForMember(dest => dest.ObjectID, opt => opt.MapFrom(src => (int)src.ObjectId))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => Enum.Parse<PatchType>(src.Type)));
            CreateMap<SkillsFarmingSeedDefinition, SeedDto>()
                .ForMember(dest => dest.ItemID, opt => opt.MapFrom(src => (int)src.ItemId))
                .ForMember(dest => dest.ProductID, opt => opt.MapFrom(src => (int)src.ProductId))
                .ForMember(dest => dest.MinimumProductCount, opt => opt.MapFrom(src => (int)src.MinimumProductCount))
                .ForMember(dest => dest.MaximumProductCount, opt => opt.MapFrom(src => (int)src.MaximumProductCount))
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.PlantingExperience, opt => opt.MapFrom(src => (double)src.PlantingExperience))
                .ForMember(dest => dest.HarvestExperience, opt => opt.MapFrom(src => (double)src.HarvestExperience))
                .ForMember(dest => dest.VarpBitIndex, opt => opt.MapFrom(src => (int)src.VarpbitIndex))
                .ForMember(dest => dest.MaxCycles, opt => opt.MapFrom(src => (int)src.MaxCycles))
                .ForMember(dest => dest.CycleTicks, opt => opt.MapFrom(src => (int)src.CycleTicks))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => Enum.Parse<PatchType>(src.Type)));

            CreateMap<SkillsSlayerMasterDefinition, SlayerMasterTable>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => (int)src.NpcId))
                .ForMember(dest => dest.BaseSlayerRewardPoints, opt => opt.MapFrom(src => (int)src.BaseSlayerRewardPoints))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Enabled, opt => opt.Ignore())
                .ForMember(dest => dest.MaxResultCount, opt => opt.Ignore())
                .ForMember(dest => dest.RandomizeResultCount, opt => opt.Ignore())
                .ForMember(dest => dest.Entries, opt => opt.Ignore())
                .ForMember(dest => dest.Modifiers, opt => opt.Ignore())
                .AfterMap((src, dest, context) =>
                {
                    foreach (var entry in src.SkillsSlayerTaskDefinitions)
                    {
                        dest.AddEntry(context.Mapper.Map<SlayerTaskDefinition>(entry));
                    }

                    dest.AddModifier(new SlayerTaskModifier());
                });
            CreateMap<SkillsSlayerTaskDefinition, SlayerTaskDefinition>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => (int)src.Id))
                .ForMember(dest => dest.Probability, opt => opt.Ignore())
                .ForMember(dest => dest.Always, opt => opt.Ignore())
                .ForMember(dest => dest.CombatLevelRequirement, opt => opt.MapFrom(src => (int)src.CombatRequirement))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.SlayerMasterId, opt => opt.MapFrom(src => (int)src.SlayerMasterId))
                .ForMember(dest => dest.CoinCount, opt => opt.MapFrom(src => (int)src.CoinCount))
                .ForMember(dest => dest.NpcIds, opt => opt.MapFrom(src => StringUtilities.SelectIntFromString(src.NpcIds).ToArray()));

            CreateMap<SkillsFishingSpotDefinition, FishingSpotTable>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => (int)src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => "Fishing spot"))
                .ForMember(dest => dest.RequiredTool, opt => opt.MapFrom(src => src.Tool))
                .ForMember(dest => dest.NpcIds, opt => opt.MapFrom(src => src.SkillsFishingSpotNpcDefinitions.Select(n => (int)n.NpcId).ToHashSet()))
                .ForMember(dest => dest.Enabled, opt => opt.Ignore())
                .ForMember(dest => dest.MaxResultCount, opt => opt.Ignore())
                .ForMember(dest => dest.RandomizeResultCount, opt => opt.Ignore())
                .ForMember(dest => dest.MinimumLevel, opt => opt.MapFrom(src => (int)src.MinimumLevel))
                .ForMember(dest => dest.Entries, opt => opt.Ignore())
                .ForMember(dest => dest.Modifiers, opt => opt.Ignore())
                .ForMember(dest => dest.ExhaustChance, opt => opt.MapFrom(src => (double)src.ExhaustChance))
                .ForMember(dest => dest.BaseCatchChance, opt => opt.MapFrom(src => (double)src.BaseCatchChance))
                .ForMember(dest => dest.ClickType, opt => opt.MapFrom(src => Enum.Parse<NpcClickType>(src.ClickType)))
                .ForMember(dest => dest.MinimumLevel, opt => opt.MapFrom(src => (int)src.MinimumLevel))
                .ForMember(dest => dest.RespawnTime, opt => opt.MapFrom(src => (double)src.RespawnTime))
                .AfterMap((src, dest, context) =>
                {
                    foreach (var entry in src.SkillsFishingFishDefinitions)
                    {
                        dest.AddEntry(context.Mapper.Map<FishingLoot>(entry));
                    }

                    dest.AddModifier(context.Mapper.Map<FishingLootModifier>(src));
                });
            CreateMap<SkillsFishingSpotDefinition, FishingLootModifier>()
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.MinimumLevel));
            CreateMap<SkillsFishingFishDefinition, FishingLoot>()
                .ForMember(dest => dest.RequiredLevel, opt => opt.MapFrom(src => (int)src.RequiredLevel))
                .ForMember(dest => dest.Probability, opt => opt.MapFrom(src => (double)src.Probability))
                .ForMember(dest => dest.FishingExperience, opt => opt.MapFrom(src => (double)src.Experience))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => (int)src.ItemId))
                .ForMember(dest => dest.Enabled, opt => opt.Ignore());
            CreateMap<SkillsFishingToolDefinition, FishingToolDto>()
                .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => (int)src.ItemId))
                .ForMember(dest => dest.CastAnimationId, opt => opt.MapFrom(src => src.CastAnimationId.HasValue ? (int)src.CastAnimationId.Value : -1))
                .ForMember(dest => dest.FishAnimationId, opt => opt.MapFrom(src => (int)src.FishAnimationId));
        }

        private static Location ToLocation(int[] coordinates) => Location.Create(coordinates[0], coordinates[1], coordinates[2]);
    }
}