using System;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    ///     Contains the staff of light equipment script.
    /// </summary>
    [EquipmentScriptMetaData([15486, 15502, 22207, 22209, 22211, 22213])]
    public class StaffOfLight : EquipmentScript
    {
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
            character.QueueAnimation(Animation.Create(12804));
            character.QueueGraphic(Graphic.Create(2319)); // 2320
            character.QueueGraphic(Graphic.Create(2321));
            character.AddState(new StaffOfLightSpecialEffectState { TicksLeft = 100, OnRemovedCallback = () => { character.SendChatMessage("The power of the light fades. Your resistance to melee attacks returns to normal."); } });
            character.SendChatMessage("The power of the light courses through your veins...");
            return false;
        }

        /// <summary>
        ///     Get's called when character receives damage.
        ///     By default this method returns damage amount in parameter.
        /// </summary>
        /// <param name="item">Item (shield or weapon) instance.</param>
        /// <param name="victim">Character which is defending.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="damageType">Type of the damage.</param>
        /// <param name="damage">Amount of damage, can be -1 incase of miss.</param>
        /// <returns>
        ///     Amount of damage remains after defence.
        /// </returns>
        public override void OnAttack(IItem item, ICharacter victim, ICreature attacker, DamageType damageType, ref int damage)
        {
            if ((damageType == DamageType.FullMelee || damageType == DamageType.StandardMelee) && victim.HasState<StaffOfLightSpecialEffectState>())
            {
                damage *= (int)Math.Truncate(damage * 0.5);
            }

            base.OnAttack(item, victim, attacker, damageType, ref damage);
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
        public override int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 1000;
    }
}
