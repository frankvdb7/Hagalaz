﻿using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    ///     Contains bgs equipment script.
    /// </summary>
    public class BandosGodsword : EquipmentScript
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

            var hit = combat.GetMeleeDamage(victim, true);
            var standartMax = combat.GetMeleeMaxHit(victim, false);
            combat.PerformSoulSplit(victim, hit);
            hit = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, hit, 0);
            combat.AddMeleeExperience(hit);
            var soak = -1;
            hit = victim.Combat.Attack(attacker, DamageType.FullMelee, hit, ref soak);

            var splat = new HitSplat(attacker);
            splat.SetFirstSplat(hit <= 0 ? HitSplatType.HitMiss : HitSplatType.HitMeleeDamage, hit <= 0 ? 0 : hit, standartMax <= hit);
            if (soak != -1)
            {
                splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
            }

            victim.QueueHitSplat(splat);

            if (hit > 0 && victim is ICharacter vic)
            {
                var toDamage = (int)(hit * 0.10);
                if (toDamage > 0)
                {
                    toDamage -= vic.Statistics.DamageSkill(StatisticsConstants.Defence, (byte)toDamage);
                }

                if (toDamage > 0)
                {
                    toDamage -= vic.Statistics.DamageSkill(StatisticsConstants.Strength, (byte)toDamage);
                }

                if (toDamage > 0)
                {
                    toDamage -= vic.Statistics.DamageSkill(StatisticsConstants.Attack, (byte)toDamage);
                }

                if (toDamage > 0)
                {
                    toDamage -= vic.Statistics.DamageSkill(StatisticsConstants.Prayer, (byte)toDamage);
                }

                if (toDamage > 0)
                {
                    toDamage -= vic.Statistics.DamageSkill(StatisticsConstants.Magic, (byte)toDamage);
                }

                if (toDamage > 0)
                {
                    toDamage -= vic.Statistics.DamageSkill(StatisticsConstants.Ranged, (byte)toDamage);
                }
            }
        }

        /// <summary>
        ///     Render's this weapon attack.
        /// </summary>
        public override void RenderAttack(IItem item, ICharacter animator, bool specialAttack)
        {
            if (specialAttack)
            {
                animator.QueueAnimation(Animation.Create(11991));
                animator.QueueGraphic(Graphic.Create(2114));
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
        public override int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 1000;

        /// <summary>
        ///     Happens when this item is equiped.
        /// </summary>
        public override void OnEquiped(IItem item, ICharacter character) => character.AddState(new State(StateType.BandosGodswordEquipped, int.MaxValue));

        /// <summary>
        ///     Happens when this item is unequiped.
        /// </summary>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState(StateType.BandosGodswordEquipped);

        /// <summary>
        ///     Get's items suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<int> GetSuitableItems() => [11696, 13451];
    }
}