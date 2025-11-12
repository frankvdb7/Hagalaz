using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    /// </summary>
    [ItemScriptMetaData(itemIds: [2452, 2454, 2456, 2458])]
    public class AntiFirePotion : Potion
    {
        public AntiFirePotion(IPotionSkillService potionService, IHerbloreSkillService herbloreSkillService) : base(potionService, herbloreSkillService)
        {
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            character.AddState(new AntiDragonfirePotionWarningState { TicksLeft = 585 });
            character.AddState(new AntiDragonfirePotionState { TicksLeft = 600 });
        }
    }
}