using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Scripts.Equipment.Barrows;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    /// </summary>
    public class GuthansWarSpear : GuthanEquipment
    {
        /// <summary>
        ///     Perform's standart ( Non special ) attack to victim.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="victim">Victim character.</param>
        public override void PerformStandardAttack(IItem item, ICharacter attacker, ICreature victim)
        {
            RenderAttack(item, attacker, false); // might throw NotImplementedException 

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

            if (attacker.HasState(StateType.GuthanInfestation) && damage > 0)
            {
                if (RandomStatic.Generator.NextDouble() >= 0.90)
                {
                    victim.QueueGraphic(Graphic.Create(398));
                    attacker.Statistics.HealLifePoints(damage);
                }
            }
        }

        /// <summary>
        ///     Get's items for which this script is made.
        /// </summary>
        /// <returns>
        ///     Return's array of item ids for which this script is suitable.
        /// </returns>
        public override IEnumerable<int>  GetSuitableItems() => [4726, 4910, 4911, 4912, 4913];
    }
}