using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains extreme magic potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [15320, 15321, 15322, 15323])]
    public class ExtremeMagicPotion : ExtremePotion
    {
        public ExtremeMagicPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
        {
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            var amount = 7;
            var max = character.Statistics.LevelForExperience(StatisticsConstants.Magic) + amount;
            character.Statistics.HealSkill(StatisticsConstants.Magic, max, amount);
        }
    }
}