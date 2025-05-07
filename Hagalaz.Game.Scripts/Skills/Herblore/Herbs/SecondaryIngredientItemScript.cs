using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Herbs
{
    /// <summary>
    ///     Contains standard def script.
    /// </summary>
    public class SecondaryIngredientItemScript : ItemScript
    {
        private readonly IHerbloreSkillService _herbloreSkillService;

        public SecondaryIngredientItemScript(IHerbloreSkillService herbloreSkillService) => _herbloreSkillService = herbloreSkillService;

        /// <summary>
        ///     Uses the item.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <param name="character">The character.</param>
        /// <returns>
        ///     <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        public override bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character)
        {
            var potion = _herbloreSkillService.GetPotionBySecondaryItemId(used.Id, usedWith.Id);
            if (potion != null)
            {
                return _herbloreSkillService.MakePotion(character, used, usedWith, potion, false);
            }

            potion = _herbloreSkillService.GetPotionBySecondaryItemId(usedWith.Id, used.Id);
            if (potion != null)
            {
                return _herbloreSkillService.MakePotion(character, usedWith, used, potion, false);
            }

            return false;
        }
    }
}