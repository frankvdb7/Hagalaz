using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Services.Abstractions;
using Hagalaz.Services.GameWorld.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Store
{
    public class NpcDefinitionStore : ConcurrentStore<int, INpcDefinition>, IStartupService
    {
        private Dictionary<int, Hagalaz.Data.Entities.NpcDefinition> _databaseNpcs = new();
        private Dictionary<int, Hagalaz.Data.Entities.NpcStatistic> _databaseNpcStatistics = new();
        private Dictionary<int, Hagalaz.Data.Entities.NpcBonuses> _databaseNpcBonuses = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ITypeProvider<INpcDefinition> _npcProvider;
        private readonly ILogger<NpcDefinitionStore> _logger;

        public NpcDefinitionStore(IServiceProvider serviceProvider, ITypeProvider<INpcDefinition> npcProvider, ILogger<NpcDefinitionStore> logger)
        {
            _serviceProvider = serviceProvider;
            _npcProvider = npcProvider;
            _logger = logger;
        }

        private INpcDefinition LoadNpcDefinition(int npcId)
        {
            var type = _npcProvider.Get(npcId);
            if (_databaseNpcs.TryGetValue(npcId, out var npc))
            {
                type.DisplayName = npc.Name;
                type.Examine = npc.Examine;
                type.RespawnTime = (int)npc.RespawnTime;
                type.CombatLevel = npc.CombatLevel;
                type.ReactionType = (ReactionType)npc.ReactionType;
                type.BoundsType = (BoundsType)npc.BoundsType;
                type.Attackable = npc.Attackable == 1;
                type.WalksRandomly = npc.WalksRandomly == 1;
                type.AttackSpeed = (int)npc.AttackSpeed;
                type.AttackAnimation = npc.AttackAnimation;
                type.AttackGraphic = npc.AttackGraphic;
                type.DefenceAnimation = npc.DefenceAnimation;
                type.DefenseGraphic = npc.DefenceGraphic;
                type.DeathAnimation = npc.DeathAnimation;
                type.DeathGraphic = npc.DeathGraphic;
                type.DeathTicks = npc.DeathTicks;
                type.SlayerLevelRequired = npc.SlayerLevelRequired;
                type.LootTableId = npc.NpcLootId ?? 0;
                type.PickPocketingLootTableId = npc.NpcPickpocketingLootId ?? 0;
            }

            if (_databaseNpcStatistics.TryGetValue(npcId, out var stat))
            {
                type.MaxAttackLevel = stat.AttackLevel;
                type.MaxStrengthLevel = stat.StrengthLevel;
                type.MaxDefenceLevel = stat.DefenceLevel;
                type.MaxMagicLevel = stat.MagicLevel;
                type.MaxRangedLevel = stat.RangedLevel;
                type.MaxLifePoints = stat.MaxLifepoints;
            }

            if (_databaseNpcBonuses.TryGetValue(npcId, out var bonus))
            {
                type.Bonuses.SetBonus(BonusType.AttackCrush, bonus.AttackCrush);
                type.Bonuses.SetBonus(BonusType.AttackCrush, bonus.AttackCrush);
                type.Bonuses.SetBonus(BonusType.AttackStab, bonus.AttackStab);
                type.Bonuses.SetBonus(BonusType.AttackRanged, bonus.AttackRanged);
                type.Bonuses.SetBonus(BonusType.AttackMagic, bonus.AttackMagic);
                type.Bonuses.SetBonus(BonusType.AttackSlash, bonus.AttackSlash);
                type.Bonuses.SetBonus(BonusType.AbsorbMagic, bonus.AbsorbMagic);
                type.Bonuses.SetBonus(BonusType.AbsorbRange, bonus.AbsorbRange);
                type.Bonuses.SetBonus(BonusType.AbsorbMelee, bonus.AbsorbMelee);
                type.Bonuses.SetBonus(BonusType.DefenceCrush, bonus.DefenceCrush);
                type.Bonuses.SetBonus(BonusType.DefenceMagic, bonus.DefenceMagic);
                type.Bonuses.SetBonus(BonusType.DefenceRanged, bonus.DefenceRanged);
                type.Bonuses.SetBonus(BonusType.DefenceSlash, bonus.DefenceSlash);
                type.Bonuses.SetBonus(BonusType.DefenceStab, bonus.DefenceStab);
                type.Bonuses.SetBonus(BonusType.DefenceSummoning, bonus.DefenceSummoning);
                type.Bonuses.SetBonus(BonusType.MagicDamage, bonus.Magic);
                type.Bonuses.SetBonus(BonusType.Prayer, bonus.Prayer);
                type.Bonuses.SetBonus(BonusType.RangedStrength, bonus.RangedStrength);
                type.Bonuses.SetBonus(BonusType.Strength, bonus.Strength);
            }

            return type;
        }

        public INpcDefinition GetOrAdd(int npcId) => GetOrAdd(npcId, LoadNpcDefinition);

        // TODO - implement Redis for this
        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            _databaseNpcs =
                (await scope.ServiceProvider.GetRequiredService<INpcDefinitionRepository>().FindAll().AsNoTracking().ToListAsync()).ToDictionary(l =>
                    (int)l.NpcId);

            _logger.LogInformation("Loaded {Count} npc definitions", _databaseNpcs.Count);

            _databaseNpcStatistics =
                (await scope.ServiceProvider.GetRequiredService<INpcStatisticsRepository>().FindAll().AsNoTracking().ToListAsync()).ToDictionary(l =>
                    (int)l.NpcId);

            _logger.LogInformation("Loaded {Count} npc statistics", _databaseNpcStatistics.Count);

            _databaseNpcBonuses =
                (await scope.ServiceProvider.GetRequiredService<INpcBonusesRepository>().FindAll().AsNoTracking().ToListAsync()).ToDictionary(l =>
                    (int)l.NpcId);

            _logger.LogInformation("Loaded {Count} npc bonuses", _databaseNpcStatistics.Count);
        }
    }
}