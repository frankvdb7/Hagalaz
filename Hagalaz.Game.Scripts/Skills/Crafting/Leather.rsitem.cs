using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Crafting
{
    /// <summary>
    /// </summary>
    public class Leather : ItemScript
    {
        private readonly ICraftingSkillService _craftingSkillService;

        public Leather(ICraftingSkillService craftingSkillService)
        {
            _craftingSkillService = craftingSkillService;
        }

        /// <summary>
        ///     Uses the item on an other item.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <param name="character">The character.</param>
        /// <returns>
        ///     <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        public override bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character)
        {
            if (used.Id == CraftingSkillService.NeedleId)
            {
                var result = _craftingSkillService.TryCraftLeather(character, usedWith).Result;
                return result;
            }

            return usedWith.Id == CraftingSkillService.NeedleId && _craftingSkillService.TryCraftLeather(character, used).Result;
        }
    }
}