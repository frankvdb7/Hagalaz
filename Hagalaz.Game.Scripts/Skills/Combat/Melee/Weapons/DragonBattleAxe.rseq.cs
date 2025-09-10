using System;
using System.Collections.Generic;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    ///     Contains dragon battle axe script.
    /// </summary>
    [EquipmentScriptMetaData([1377])]
    public class DragonBattleAxe : EquipmentScript
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

            character.Speak(GameStrings.DragonBattleAxeSpecial);

            character.QueueAnimation(Animation.Create(1057));
            character.QueueGraphic(Graphic.Create(246));

            var amount = (int)Math.Floor(0.20 * character.Statistics.LevelForExperience(StatisticsConstants.Strength));
            var max = character.Statistics.LevelForExperience(StatisticsConstants.Strength) + amount;
            character.Statistics.HealSkill(StatisticsConstants.Strength, (byte)max, (byte)amount);

            var damage = (int)Math.Floor(0.10 * character.Statistics.LevelForExperience(StatisticsConstants.Attack));
            character.Statistics.DamageSkill(StatisticsConstants.Attack, (byte)damage);

            damage = (int)Math.Floor(0.10 * character.Statistics.LevelForExperience(StatisticsConstants.Defence));
            character.Statistics.DamageSkill(StatisticsConstants.Defence, (byte)damage);

            damage = (int)Math.Floor(0.10 * character.Statistics.LevelForExperience(StatisticsConstants.Ranged));
            character.Statistics.DamageSkill(StatisticsConstants.Ranged, (byte)damage);

            damage = (int)Math.Floor(0.10 * character.Statistics.LevelForExperience(StatisticsConstants.Magic));
            character.Statistics.DamageSkill(StatisticsConstants.Magic, (byte)damage);
            return false;
        }

        /// <summary>
        ///     Get's required special energy amount.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <returns>
        ///     System.Int16.
        /// </returns>
        public override int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 1000;
    }
}