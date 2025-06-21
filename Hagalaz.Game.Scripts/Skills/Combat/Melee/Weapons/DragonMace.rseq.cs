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

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    ///     Contains dragon mace script.
    /// </summary>
    [EquipmentScriptMetaData([1434])]
    public class DragonMace : EquipmentScript
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
                MaxDamage = maxDamage,
                DamageType = DamageType.FullMelee,
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
                animator.QueueAnimation(Animation.Create(1060));
                animator.QueueGraphic(Graphic.Create(251));
            }
            else
            {
                base.RenderAttack(item, animator, specialAttack);
            }
        }

        /// <summary>
        ///     Get's required special energy amount.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <returns>
        ///     System.Int16.
        /// </returns>
        public override int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 250;

        /// <summary>
        ///     Happens when this item is equiped.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnEquiped(IItem item, ICharacter character) => character.AddState(new State(StateType.DragonMaceEquiped, int.MaxValue));

        /// <summary>
        ///     Happens when this item is unequiped.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState(StateType.DragonMaceEquiped);
    }
}