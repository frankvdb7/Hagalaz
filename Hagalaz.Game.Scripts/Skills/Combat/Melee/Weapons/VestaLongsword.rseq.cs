using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Equipment.Ancient;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    ///     Contains vesta long equipment script.
    /// </summary>
    [EquipmentScriptMetaData([13899, 13901])]
    public class VestaLongsword : AncientEquipment
    {
        /// <summary>
        ///     Perform's special attack to victim.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="victim">Victim creature.</param>
        public override void PerformSpecialAttack(IItem item, ICharacter attacker, ICreature victim)
        {
            RenderAttack(item, attacker, true);
            var combat = (ICharacterCombat)attacker.Combat;

            var damage = combat.GetMeleeDamage(victim, true);
            var maxDamage = combat.GetMeleeMaxHit(victim, false);

            attacker.Combat.PerformAttack(new AttackParams()
            {
                Damage = damage,
                DamageType = DamageType.FullMelee,
                MaxDamage = maxDamage,
                Target = victim
            });
        }

        /// <summary>
        ///     Render's this weapon attack.
        /// </summary>
        public override void RenderAttack(IItem item, ICharacter animator, bool specialAttack)
        {
            if (specialAttack)
            {
                animator.QueueAnimation(Animation.Create(10502));
            }
            else
            {
                base.RenderAttack(item, animator, false);
            }
        }

        /// <summary>
        ///     Get's required special energy amount.
        /// </summary>
        /// <returns></returns>
        public override int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 250;

        /// <summary>
        ///     Happens when this item is equiped.
        /// </summary>
        public override void OnEquiped(IItem item, ICharacter character) => character.AddState(new State(StateType.VestaLongswordEquipped, int.MaxValue));

        /// <summary>
        ///     Happens when this item is unequiped.
        /// </summary>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState(StateType.VestaLongswordEquipped);

        /// <summary>
        ///     Gets the degration ticks for one degradable item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override int GetDegrationTicks(IItem item)
        {
            if (item.Id == 13899)
            {
                return 0;
            }

            return 6000; // 6000 ticks = 1 hour
        }

        /// <summary>
        ///     Gets the degraded item identifier.
        /// </summary>
        /// <param name="item">The current.</param>
        /// <returns></returns>
        public override int GetDegradedItemID(IItem item)
        {
            if (item.Id == 13899)
            {
                return 13901;
            }

            return -1;
        }
    }
}