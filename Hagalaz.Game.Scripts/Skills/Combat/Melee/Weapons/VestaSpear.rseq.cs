using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Scripts.Equipment.Ancient;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    /// </summary>
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
            attacker.AddState(new State(StateType.MeleeImmunity, 8));
            //if (!attacker.Area.MultiCombat)
            //{
            var preDmg = ((ICharacterCombat)attacker.Combat).GetMeleeDamage(victim, false);
            ((ICharacterCombat)attacker.Combat).PerformSoulSplit(victim, preDmg);
            preDmg = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, preDmg, 15);
            ((ICharacterCombat)attacker.Combat).AddMeleeExperience(preDmg);
            var soaked = -1;
            var damage = victim.Combat.Attack(attacker, DamageType.FullMelee, preDmg, ref soaked);
            var splat = new HitSplat(attacker);
            splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMeleeDamage, damage == -1 ? 0 : damage, ((ICharacterCombat)attacker.Combat).GetMeleeMaxHit(victim, false) <= damage);
            if (soaked != -1)
            {
                splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked, false);
            }

            victim.QueueHitSplat(splat);
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
        public override short GetDegradedItemID(IItem item)
        {
            if (item.Id == 13905)
            {
                return 13907;
            }

            return -1;
        }

        /// <summary>
        ///     Get's items for which this script is made.
        /// </summary>
        /// <returns>
        ///     Return's array of item ids for which this script is suitable.
        /// </returns>
        public override IEnumerable<int>  GetSuitableItems() => [13905, 13907];
    }
}