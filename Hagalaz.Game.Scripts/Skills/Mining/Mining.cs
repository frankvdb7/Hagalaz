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
        /// <param name="pickaxes">The pickaxe definitions to consider.</param>
        /// <returns>
        ///     The highest-level usable owned pickaxe, or <c>null</c> if none qualifies.
        /// </returns>
        public static PickaxeDto? FindPickaxe(ICharacter character, IReadOnlyList<PickaxeDto> pickaxes)
        {
            var miningLevel = character.Statistics.GetSkillLevel(StatisticsConstants.Mining);
            return pickaxes
                .Where(h => h.RequiredLevel <= miningLevel && (character.Equipment.Items.GetById(h.ItemId) != null || character.Inventory.Items.GetById(h.ItemId) != null))
                .OrderByDescending(h => h.RequiredLevel) // return the highest possible level
                .FirstOrDefault(); // return null if nothing found
        }
    }
}
