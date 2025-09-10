using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([10499])]
    public class AvasAccumulator : EquipmentScript
    {
        /// <summary>
        ///     Determines whether this instance [can equip item] the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public override bool CanEquipItem(IItem item, ICharacter character)
        {
            if (character.Statistics.LevelForExperience(StatisticsConstants.Ranged) < 50)
            {
                character.SendChatMessage("You need a " + StatisticsConstants.SkillNames[StatisticsConstants.Ranged] + " level of 50 to wear this item.");
                return false;
            }

            return base.CanEquipItem(item, character);
        }

        /// <summary>
        ///     Happens when this item is equiped by specific character.
        ///     By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnEquiped(IItem item, ICharacter character) => character.AddState(new State(StateType.AvasAccumulatorEquiped, int.MaxValue));

        /// <summary>
        ///     Happens when this item is unequiped by specific character.
        ///     By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState(StateType.AvasAccumulatorEquiped);
    }
}