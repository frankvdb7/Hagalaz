using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Features.States.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    ///     Contains ags equipment script.
    /// </summary>
    [EquipmentScriptMetaData([11694, 13450])]
    public class ArmadylGodsword : EquipmentScript
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
                Target = victim,
            });
        }

        /// <summary>
        ///     Render's this weapon attack.
        /// </summary>
        public override void RenderAttack(IItem item, ICharacter animator, bool specialAttack)
        {
            if (specialAttack)
            {
                animator.QueueAnimation(Animation.Create(11989));
                animator.QueueGraphic(Graphic.Create(2113));
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
        ///     Happens when this item is equiped.
        /// </summary>
        public override void OnEquiped(IItem item, ICharacter character) => character.AddState(new ArmadylGodswordEquippedState());

        /// <summary>
        ///     Happens when this item is unequiped.
        /// </summary>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState<ArmadylGodswordEquippedState>();

    }
}
