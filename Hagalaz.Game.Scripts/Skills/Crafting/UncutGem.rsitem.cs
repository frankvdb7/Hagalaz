using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Crafting
{
    /// <summary>
    /// </summary>
    public class UncutGem : ItemScript
    {
        private readonly ICraftingSkillService _skillService;

        public UncutGem(ICraftingSkillService skillService)
        {
            _skillService = skillService;
        }

        /// <summary>
        ///     Uses the item on item.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public override bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character)
        {
            if (used.Id == 1755)
            {
                return _skillService.TryCutGem(character, usedWith).Result;
            }

            return usedWith.Id == 1755 && _skillService.TryCutGem(character, used).Result;
        }
    }
}