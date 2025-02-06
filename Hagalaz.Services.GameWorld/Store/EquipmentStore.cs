using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Collections;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.Abstractions;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Data.Model;
using Hagalaz.Services.GameWorld.Model.Creatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Store
{
    public class EquipmentStore : ConcurrentStore<int, IEquipmentDefinition>, IStartupService
    {
        private readonly IServiceProvider _serviceProvider;
        private Dictionary<int, Hagalaz.Data.Entities.EquipmentDefinition> _databaseEquipment = new();

        public EquipmentStore(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IEquipmentDefinition GetOrAdd(int itemId) => GetOrAdd(itemId, LoadEquipmentDefinition);

        private IEquipmentDefinition LoadEquipmentDefinition(int itemId)
        {
            var equipment = new EquipmentDefinition(itemId);
            var styles = new[]
            {
                AttackStyle.MeleeAccurate, AttackStyle.MeleeAggressive, AttackStyle.MeleeDefensive, AttackStyle.None
            };
            var bonuses = new[]
            {
                AttackBonus.Slash, AttackBonus.Slash, AttackBonus.Slash, AttackBonus.Slash
            };
            using var scope = _serviceProvider.CreateScope();
            var item = scope.ServiceProvider.GetRequiredService<IItemService>().FindItemDefinitionById(itemId);

            equipment.AttackSpeed = item.GetAttackSpeed();
            equipment.Slot = (EquipmentSlot)item.EquipSlot;
            if (equipment.Type == EquipmentType.Normal)
                equipment.Type = (EquipmentType)item.EquipType;
            equipment.SpecialWeapon = item.HasSpecialBar();
            var bonusTypes = item.GetAttackBonusTypes();
            for (var i = 0; i < bonusTypes.Length; i++)
                bonuses[i] = (AttackBonus)bonusTypes[i];
            var styleTypes = item.GetAttackStylesTypes();
            for (var i = 0; i < styleTypes.Length; i++)
                styles[i] = (AttackStyle)styleTypes[i];
            equipment.Requirements = item.GetEquipmentRequirements();

            var bonus = new int[18];
            bonus[0] = item.GetStabAttack();
            bonus[1] = item.GetSlashAttack();
            bonus[2] = item.GetCrushAttack();
            bonus[3] = item.GetMagicAttack();
            bonus[4] = item.GetRangeAttack();
            bonus[5] = item.GetStabDefence();
            bonus[6] = item.GetSlashDefence();
            bonus[7] = item.GetCrushDefence();
            bonus[8] = item.GetMagicDefence();
            bonus[9] = item.GetRangeDefence();
            bonus[10] = item.GetSummoningDefence();
            bonus[11] = item.GetAbsorbMeleeBonus();
            bonus[12] = item.GetAbsorbMageBonus();
            bonus[13] = item.GetAbsorbRangeBonus();
            bonus[14] = item.GetStrengthBonus();
            bonus[15] = item.GetRangedStrengthBonus();
            bonus[16] = item.GetPrayerBonus();
            bonus[17] = item.GetMagicDamage();

            equipment.Bonuses = new Bonuses(bonus);

            equipment.AttackStyleIDs = styles;
            equipment.AttackBonusesIDs = bonuses;

            if (!_databaseEquipment.TryGetValue(itemId, out var value))
            {
                return equipment;
            }

            const string trueValue = "true";
            if (value.Fullbody == trueValue)
            {
                equipment.Type = EquipmentType.FullBody;
            }
            if (value.Fullhat == trueValue)
            {
                equipment.Type = EquipmentType.FullHat;
            }
            if (value.Fullmask == trueValue)
            {
                equipment.Type = EquipmentType.FullMask;
            }
            equipment.DefenceAnimation = value.DefenceAnim;
            equipment.AttackAnimations =
            [
                value.Attackanim1,
                value.Attackanim2,
                value.Attackanim3,
                value.Attackanim4
            ];
            equipment.AttackGraphics =
            [
                value.Attackgfx1,
                value.Attackgfx2,
                value.Attackgfx3,
                value.Attackgfx4
            ];
            equipment.AttackDistance = value.AttackDistance;
            return equipment;
        }

        public async Task LoadAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            _databaseEquipment = (await scope.ServiceProvider.GetRequiredService<IEquipmentDefinitionRepository>().FindAll().ToListAsync()).ToDictionary(i => (int)i.Id);
        }
    }
}
