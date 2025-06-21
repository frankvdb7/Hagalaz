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
    [EquipmentScriptMetaData([1215, 1231, 5680, 5698, 13465, 13467])]
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
            var maxDamage = combat.GetMeleeMaxHit(victim, false);
            attacker.Combat.PerformAttack(new AttackParams()
            {
                Damage = combat.GetMeleeDamage(victim, true),
                DamageType = DamageType.FullMelee,
                Target = victim,
                MaxDamage = maxDamage
            });
            attacker.Combat.PerformAttack(new AttackParams()
            {
                Damage = combat.GetMeleeDamage(victim, true),
                DamageType = DamageType.FullMelee,
                Target = victim,
                MaxDamage = maxDamage
            });
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
    }
}