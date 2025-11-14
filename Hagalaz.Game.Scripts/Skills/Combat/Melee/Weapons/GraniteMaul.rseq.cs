using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([4153])]
    public class GraniteMaul : EquipmentScript
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
        ///     Happens when specific character clicks special bar ( which was not enabled ) on combat tab.
        ///     By default this method does return true if weapon is special weapon.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="character">The character.</param>
        /// <returns>
        ///     If the bar should be enabled.
        /// </returns>
        public override bool SpecialBarEnableClicked(IItem item, ICharacter character)
        {
            if (character.Combat.Target == null)
            {
                return true;
            }

            var requiredEnergyAmount = GetRequiredSpecialEnergyAmount(item, character);
            var gameMediator = character.Mediator;
            if (character.Statistics.SpecialEnergy < requiredEnergyAmount)
            {
                character.SendChatMessage(GameStrings.NotEnoughSpecialEnergy);
                gameMediator.Publish(new ProfileSetBoolAction(ProfileConstants.CombatSettingsSpecialAttack, false));
                return false;
            }

            character.Statistics.DrainSpecialEnergy(requiredEnergyAmount);
            gameMediator.Publish(new ProfileSetBoolAction(ProfileConstants.CombatSettingsSpecialAttack, false));
            PerformSpecialAttack(item, character, character.Combat.Target);
            character.Combat.OnAttackPerformed(character.Combat.Target);
            return false;
        }

        /// <summary>
        ///     Render's this weapon attack.
        /// </summary>
        public override void RenderAttack(IItem item, ICharacter animator, bool specialAttack)
        {
            if (specialAttack)
            {
                animator.QueueAnimation(Animation.Create(1667));
                animator.QueueGraphic(Graphic.Create(340));
            }
            else
            {
                base.RenderAttack(item, animator, specialAttack);
            }
        }

        /// <summary>
        ///     Get's amount of special energy required by this weapon.
        ///     By default , this method does throw NotImplementedException
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <returns>
        ///     System.Int16.
        /// </returns>
        public override int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 500;

        /// <summary>
        ///     Happens when this item is equiped.
        /// </summary>
        public override void OnEquiped(IItem item, ICharacter character) => character.AddState(new GraniteMaulEquippedState());

        /// <summary>
        ///     Happens when this item is unequiped.
        /// </summary>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState<GraniteMaulEquippedState>();
    }
}
