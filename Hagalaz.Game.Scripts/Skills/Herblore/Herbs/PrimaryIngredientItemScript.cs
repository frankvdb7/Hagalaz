using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Herbs
{
    /// <summary>
    ///     Contains standard def script.
    /// </summary>
    public class PrimaryIngredientItemScript : ItemScript
    {
        private readonly IHerbloreSkillService _herbloreSkillService;

        public PrimaryIngredientItemScript(IHerbloreSkillService herbloreSkillService) => _herbloreSkillService = herbloreSkillService;

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
            if (used.Id == HerbloreConstants.FilledVial || usedWith.Id == HerbloreConstants.FilledVial)
            {
                var potion = _herbloreSkillService.GetPotionByPrimaryItemId(used.Id);
                if (potion != null)
                {
                    character.Interrupt(this);
                    return _herbloreSkillService.MakePotion(character, usedWith, used, potion, true);
                }

                potion = _herbloreSkillService.GetPotionByPrimaryItemId(usedWith.Id);
                if (potion != null)
                {
                    character.Interrupt(this);
                    return _herbloreSkillService.MakePotion(character, used, usedWith, potion, true);
                }
            }

            return false;
        }
    }
}