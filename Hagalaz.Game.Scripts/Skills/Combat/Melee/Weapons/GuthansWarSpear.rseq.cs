using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Equipment.Barrows;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([4726, 4910, 4911, 4912, 4913])]
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

            var combat = (ICharacterCombat)attacker.Combat;
            var damage = combat.GetMeleeDamage(victim, false);
            var maxDamage = combat.GetMeleeMaxHit(victim, false);

            var handle = attacker.Combat.PerformAttack(new AttackParams()
            {
                Damage = damage,
                MaxDamage = maxDamage,
                DamageType = DamageType.FullMelee,
                Target = victim
            });

            handle.RegisterResultHandler(result =>
            {
                if (!attacker.HasState(StateType.GuthanInfestation) || !result.DamageLifePoints.Succeeded)
                {
                    return;
                }

                if (!(RandomStatic.Generator.NextDouble() >= 0.90))
                {
                    return;
                }

                victim.QueueGraphic(Graphic.Create(398));
                attacker.Statistics.HealLifePoints(result.DamageLifePoints.Count);
            });
        }
    }
}