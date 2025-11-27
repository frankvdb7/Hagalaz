using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([10498])]
    public class AvasAttractor : EquipmentScript
    {
        /// <summary>
        ///     Determines whether this instance [can equip item] the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public override bool CanEquipItem(IItem item, ICharacter character)
        {
            if (character.Statistics.LevelForExperience(StatisticsConstants.Ranged) < 30)
            {
                character.SendChatMessage("You need a " + StatisticsConstants.SkillNames[StatisticsConstants.Ranged] + " level of 30 to wear this item.");
                return false;
            }

            return base.CanEquipItem(item, character);
        }

        /// <summary>
        ///     Happens when this item is equipped by specific character.
        ///     By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equipped the item.</param>
        public override void OnEquipped(IItem item, ICharacter character) => character.AddState(new AvasAttractorEquippedState());

        /// <summary>
        ///     Happens when this item is unequipped by specific character.
        ///     By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equipped the item.</param>
        public override void OnUnequipped(IItem item, ICharacter character) => character.RemoveState<AvasAttractorEquippedState>();
    }
}