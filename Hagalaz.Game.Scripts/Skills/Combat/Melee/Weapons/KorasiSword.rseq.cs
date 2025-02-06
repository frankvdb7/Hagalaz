using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    ///     Contains korasi sword script.
    /// </summary>
    public class KorasiSword : EquipmentScript
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
            victim.QueueGraphic(Graphic.Create(1730, 20));

            var combat = (ICharacterCombat)attacker.Combat;
            var hit = combat.GetMeleeDamage(victim, true);
            var standardMax = combat.GetMeleeMaxHit(victim, false);
            if (hit < (int)(standardMax * 0.5))
            {
                hit = (int)(standardMax * 0.5);
            }

            combat.PerformSoulSplit(victim, hit);
            hit = victim.Combat.IncomingAttack(attacker, DamageType.FullMagic, hit, 20);

            attacker.QueueTask(new RsTask(() =>
                {
                    var soak = -1;
                    hit = victim.Combat.Attack(attacker, DamageType.FullMagic, hit, ref soak);
                    combat.AddMagicExperience(hit);

                    var splat = new HitSplat(attacker);
                    splat.SetFirstSplat(hit <= 0 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, hit <= 0 ? 0 : hit, standardMax <= hit);
                    if (soak != -1)
                    {
                        splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                    }

                    victim.QueueHitSplat(splat);
                }, 1));
        }

        /// <summary>
        ///     Render's this weapon attack.
        /// </summary>
        public override void RenderAttack(IItem item, ICharacter animator, bool specialAttack)
        {
            if (specialAttack)
            {
                animator.QueueAnimation(Animation.Create(14788));
                animator.QueueGraphic(Graphic.Create(1729));
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
        public override int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 600;

        /// <summary>
        ///     Happens when this item is equiped.
        /// </summary>
        public override void OnEquiped(IItem item, ICharacter character) => character.AddState(new State(StateType.KorasiEquipped, int.MaxValue));

        /// <summary>
        ///     Happens when this item is unequiped.
        /// </summary>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState(StateType.KorasiEquipped);

        /// <summary>
        ///     Get's items suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<int>  GetSuitableItems() => [18786, 19780, 19784];
    }
}