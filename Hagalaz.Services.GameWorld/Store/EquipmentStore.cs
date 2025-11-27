using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Cache.Logic;
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

            equipment.AttackSpeed = ItemTypeLogic.GetAttackSpeed(item);
            equipment.Slot = (EquipmentSlot)item.EquipSlot;
            if (equipment.Type == EquipmentType.Normal)
                equipment.Type = (EquipmentType)item.EquipType;
            equipment.SpecialWeapon = ItemTypeLogic.HasSpecialBar(item);
            var bonusTypes = ItemTypeLogic.GetAttackBonusTypes(item);
            for (var i = 0; i < bonusTypes.Length; i++)
                bonuses[i] = (AttackBonus)bonusTypes[i];
            var styleTypes = ItemTypeLogic.GetAttackStylesTypes(item);
            for (var i = 0; i < styleTypes.Length; i++)
                styles[i] = (AttackStyle)styleTypes[i];
            equipment.Requirements = ItemTypeLogic.GetEquipmentRequirements(item);

            var bonus = new int[18];
            bonus[0] = ItemTypeLogic.GetStabAttack(item);
            bonus[1] = ItemTypeLogic.GetSlashAttack(item);
            bonus[2] = ItemTypeLogic.GetCrushAttack(item);
            bonus[3] = ItemTypeLogic.GetMagicAttack(item);
            bonus[4] = ItemTypeLogic.GetRangeAttack(item);
            bonus[5] = ItemTypeLogic.GetStabDefence(item);
            bonus[6] = ItemTypeLogic.GetSlashDefence(item);
            bonus[7] = ItemTypeLogic.GetCrushDefence(item);
            bonus[8] = ItemTypeLogic.GetMagicDefence(item);
            bonus[9] = ItemTypeLogic.GetRangeDefence(item);
            bonus[10] = ItemTypeLogic.GetSummoningDefence(item);
            bonus[11] = ItemTypeLogic.GetAbsorbMeleeBonus(item);
            bonus[12] = ItemTypeLogic.GetAbsorbMageBonus(item);
            bonus[13] = ItemTypeLogic.GetAbsorbRangeBonus(item);
            bonus[14] = ItemTypeLogic.GetStrengthBonus(item);
            bonus[15] = ItemTypeLogic.GetRangedStrengthBonus(item);
            bonus[16] = ItemTypeLogic.GetPrayerBonus(item);
            bonus[17] = ItemTypeLogic.GetMagicDamage(item);

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

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            _databaseEquipment = (await scope.ServiceProvider.GetRequiredService<IEquipmentDefinitionRepository>().FindAll().ToListAsync()).ToDictionary(i => (int)i.Id);
        }
    }
}
