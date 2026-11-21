using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     A potion made by adding a herb or ingredient to an unfinished potion. Using the ingredient on an unfinished
    ///     potion makes this potion; using two doses of the potion together combines them.
    /// </summary>
    public abstract class IngredientPotion : Potion
    {
        protected IngredientPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
        {
        }

        /// <summary>
        ///     The id of the item added to an unfinished potion to make this potion.
        /// </summary>
        protected abstract int Ingredient { get; }

        /// <inheritdoc />
        public override bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character)
        {
            if (used.Id == Ingredient || usedWith.Id == Ingredient)
            {
                var potion = HerbloreSkillService.GetPotionByUnfinishedPotionId(used.Id);
                if (potion != null)
                {
                    return HerbloreSkillService.MakePotion(character, used, usedWith, potion, false);
                }

                potion = HerbloreSkillService.GetPotionByUnfinishedPotionId(usedWith.Id);
                if (potion != null)
                {
                    return HerbloreSkillService.MakePotion(character, usedWith, used, potion, false);
                }
            }

            if (PotionIds.Contains(usedWith.Id))
            {
                return PotionSkillService.CombinePotions(character, used, usedWith, PotionIds);
            }

            return false;
        }
    }
}
