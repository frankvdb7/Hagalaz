using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     A potion that is the base of another potion: using the recipe's secondary ingredient on this potion makes the other potion
    ///     (e.g. avantoe on super attack makes extreme attack); using two doses of the potion together combines them.
    /// </summary>
    public abstract class IngredientPotion : Potion
    {
        private readonly IHerbloreService _herbloreService;

        protected IngredientPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService, IHerbloreService herbloreService)
            : base(potionSkillService, herbloreSkillService) =>
            _herbloreService = herbloreService;

        /// <inheritdoc />
        public override bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character)
        {
            var potion = _herbloreService.FindPotionBySecondaryItemId(PotionKind.Standard, used.Id, usedWith.Id);
            if (potion != null)
            {
                return HerbloreSkillService.MakePotion(character, used, usedWith, potion, false);
            }

            potion = _herbloreService.FindPotionBySecondaryItemId(PotionKind.Standard, usedWith.Id, used.Id);
            if (potion != null)
            {
                return HerbloreSkillService.MakePotion(character, usedWith, used, potion, false);
            }

            if (PotionIds.Contains(usedWith.Id))
            {
                return PotionSkillService.CombinePotions(character, used, usedWith, PotionIds);
            }

            return false;
        }
    }
}
