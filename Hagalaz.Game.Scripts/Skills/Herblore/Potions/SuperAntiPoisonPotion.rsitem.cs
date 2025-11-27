using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    /// </summary>
    [ItemScriptMetaData(itemIds: [2448, 181, 183, 185])]
    public class SuperAntiPoisonPotion : Potion
    {
        public SuperAntiPoisonPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
        {
        }

        /// <summary>
        ///     Applies the efect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            character.Poison(0);
            character.AddState(new ResistPoisonState { TicksLeft = 575 }); // 346 seconds
        }
    }
}