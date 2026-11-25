using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains magic potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [3040, 3042, 3044, 3046])]
    public class MagicPotion : IngredientPotion
    {
        public MagicPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService, IHerbloreService herbloreService)
            : base(potionSkillService, herbloreSkillService, herbloreService)
        {
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            var amount = 5;
            var max = character.Statistics.LevelForExperience(StatisticsConstants.Magic) + amount;
            character.Statistics.HealSkill(StatisticsConstants.Magic, max, amount);
        }
    }
}