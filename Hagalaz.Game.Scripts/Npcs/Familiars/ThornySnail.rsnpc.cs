using System;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Familiars
{
    [NpcScriptMetaData([6806, 6807])]
    public class ThornySnail(
        INpc owner, IItemContainerFactory itemContainerFactory, ISmartPathFinder pathFinder, INpcService npcService, IItemService itemService,
        IGroundItemBuilder groundItemBuilder, IItemBuilder itemBuilder, IWidgetScriptActivator widgetScriptActivator)
        : SingleMoveBobFamiliarScriptBase(owner, itemContainerFactory, pathFinder, npcService, itemService, groundItemBuilder, itemBuilder, widgetScriptActivator)
    {
        public override int InventoryCapacity => 3;

        public override int GetRequiredSpecialMovePoints() => 3;

        public override FamiliarSpecialType GetSpecialType() => FamiliarSpecialType.Creature;

        public override string GetSpecialMoveName() => "Slime Spray";

        public override string GetSpecialMoveDescription() => "Inflicts up to 80 damage against your opponent.";

        protected override void ApplySpecialMoveEffect(IRuneObject target)
        {
            if (target is not ICreature creature)
            {
                return;
            }

            var combat = (INpcCombat)Owner.Combat;
            Owner.Combat.PerformAttack(new AttackParams
            {
                Target = creature.Handle,
                DamageType = DamageType.FullSummoning,
                HitType = HitSplatType.HitMagicDamage,
                Damage = Math.Min(combat.GetMagicDamage(creature, MaxHit), MaxHit),
                MaxDamage = MaxHit,
            });
        }

        // Life points are tenths in the combat engine, so 80 life points is 800.
        private const int MaxHit = 800;
    }
}
