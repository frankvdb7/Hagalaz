using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    ///     Contains dragon dagger equipment script.
    /// </summary>
    public class DragonDagger : EquipmentScript
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
            var hit1 = combat.GetMeleeDamage(victim, true);
            var hit2 = combat.GetMeleeDamage(victim, true);
            var standartMax = combat.GetMeleeMaxHit(victim, false);
            combat.PerformSoulSplit(victim, CreatureHelper.CalculatePredictedDamage([hit1, hit2]));
            hit1 = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, hit1, 0);
            hit2 = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, hit2, 0);
            combat.AddMeleeExperience(hit1);
            combat.AddMeleeExperience(hit2);
            var soak1 = -1;
            var damage1 = victim.Combat.Attack(attacker, DamageType.FullMelee, hit1, ref soak1);
            var soak2 = -1;
            var damage2 = victim.Combat.Attack(attacker, DamageType.FullMelee, hit2, ref soak2);
            var splat1 = new HitSplat(attacker);
            splat1.SetFirstSplat(damage1 <= 0 ? HitSplatType.HitMiss : HitSplatType.HitMeleeDamage, damage1 <= 0 ? 0 : damage1, standartMax <= damage1);
            if (soak1 != -1)
            {
                splat1.SetSecondSplat(HitSplatType.HitDefendedDamage, soak1, false);
            }

            victim.QueueHitSplat(splat1);

            var splat2 = new HitSplat(attacker);
            splat2.SetFirstSplat(damage2 <= 0 ? HitSplatType.HitMiss : HitSplatType.HitMeleeDamage, damage2 <= 0 ? 0 : damage2, standartMax <= damage2);
            if (soak2 != -1)
            {
                splat2.SetSecondSplat(HitSplatType.HitDefendedDamage, soak2, false);
            }

            victim.QueueHitSplat(splat2);
        }

        /// <summary>
        ///     Render's this weapon attack.
        /// </summary>
        public override void RenderAttack(IItem item, ICharacter animator, bool specialAttack)
        {
            if (specialAttack)
            {
                animator.QueueAnimation(Animation.Create(1062));
                animator.QueueGraphic(Graphic.Create(252, 0, 100));
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
        public override int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 250;

        /// <summary>
        ///     Happens when this item is equiped.
        /// </summary>
        public override void OnEquiped(IItem item, ICharacter character) => character.AddState(new State(StateType.DragonDaggerEquipped, int.MaxValue));

        /// <summary>
        ///     Happens when this item is unequiped.
        /// </summary>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState(StateType.DragonDaggerEquipped);

        /// <summary>
        ///     Get's items suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<int>  GetSuitableItems() => [1215, 1231, 5680, 5698, 13465, 13467];
    }
}