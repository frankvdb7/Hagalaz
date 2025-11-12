using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Equipment.Ancient;
using Hagalaz.Game.Scripts.Features.States.Combat;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([13905, 13907])]
    public class VestaSpear : AncientEquipment
    {
        /// <summary>
        ///     Perform's special attack to victim.
        ///     By default , this method does throw NotImplementedException
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="victim">Victim creature.</param>
        public override void PerformSpecialAttack(IItem item, ICharacter attacker, ICreature victim)
        {
            RenderAttack(item, attacker, true);
            attacker.AddState(new MeleeImmunityState(8));
            //if (!attacker.Area.MultiCombat)
            //{


            var damage = ((ICharacterCombat)attacker.Combat).GetMeleeDamage(victim, false);
            var maxDamage = ((ICharacterCombat)attacker.Combat).GetMeleeMaxHit(victim, false);
            attacker.Combat.PerformAttack(new AttackParams()
            {
                Damage = damage, DamageType = DamageType.FullMelee, MaxDamage = maxDamage, Target = victim
            });
            //}
            //else
            //{

            // TODO - Damage everyone adjacent

            // }
        }

        /// <summary>
        ///     Render's weapon attack.
        ///     This method is not guaranteed to be used when performing attacks.
        ///     This method can throw NotImplementedException.
        /// </summary>
        /// <param name="item">Item in equipment instance.</param>
        /// <param name="animator">Character which is performing attack.</param>
        /// <param name="specialAttack">Wheter attack is special.</param>
        public override void RenderAttack(IItem item, ICharacter animator, bool specialAttack)
        {
            if (specialAttack)
            {
                animator.QueueAnimation(Animation.Create(1835));
                animator.QueueGraphic(Graphic.Create(10499));
            }
            else
            {
                base.RenderAttack(item, animator, specialAttack);
            }
        }

        /// <summary>
        ///     Get's required special energy amount.
        /// </summary>
        /// <returns></returns>
        public override int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 500;

        /// <summary>
        ///     Gets the degration ticks for one degradable item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override int GetDegrationTicks(IItem item)
        {
            if (item.Id == 13905)
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
            if (item.Id == 13905)
            {
                return 13907;
            }

            return -1;
        }
    }
}
