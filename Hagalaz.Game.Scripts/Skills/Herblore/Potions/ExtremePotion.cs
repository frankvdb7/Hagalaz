using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Base for extreme potions: a herb used on an extreme potion that is a secondary ingredient of the overload recipe makes an overload,
    ///     otherwise doses combine.
    /// </summary>
    public abstract class ExtremePotion : Potion
    {
        private readonly IHerbloreService _herbloreService;

        protected ExtremePotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService, IHerbloreService herbloreService)
            : base(potionSkillService, herbloreSkillService) =>
            _herbloreService = herbloreService;

        /// <inheritdoc />
        public override bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character)
        {
            var overload = _herbloreService.FindPotionBySecondaryItemId(PotionKind.Overload, used.Id, usedWith.Id);
            if (overload != null)
            {
                return HerbloreSkillService.MakeOverload(character, used, overload);
            }

            overload = _herbloreService.FindPotionBySecondaryItemId(PotionKind.Overload, usedWith.Id, used.Id);
            if (overload != null)
            {
                return HerbloreSkillService.MakeOverload(character, usedWith, overload);
            }

            if (PotionIds.Contains(usedWith.Id))
            {
                return PotionSkillService.CombinePotions(character, used, usedWith, PotionIds);
            }

            return false;
        }
    }
}
