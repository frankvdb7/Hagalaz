using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Mining
{
    /// <summary>
    ///     Functionality for the mining skill.
    /// </summary>
    public static class Mining
    {

        /// <summary>
        ///     Attempts to find a pickaxe in the specified character's inventory or equipped items.
        /// </summary>
        /// <param name="character">The character to find pickaxe for.</param>
        /// <returns>
        ///     Returns the pickaxe type if found; Returns Hatchet.Undefined otherwise.
        /// </returns>
        public static PickaxeDto? FindPickaxe(ICharacter character, IReadOnlyList<PickaxeDto> pickaxes)
        {
            var miningLevel = character.Statistics.GetSkillLevel(StatisticsConstants.Mining);
            return pickaxes
                .Where(h => h.RequiredLevel <= miningLevel && (character.Equipment.GetById(h.ItemId) != null || character.Inventory.GetById(h.ItemId) != null))
                .OrderByDescending(h => h.RequiredLevel) // return the highest possible level
                .FirstOrDefault(); // return null if nothing found
        }
    }
}