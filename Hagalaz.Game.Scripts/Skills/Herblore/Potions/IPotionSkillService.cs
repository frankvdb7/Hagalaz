using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    public interface IPotionSkillService
    {
        bool CombinePotions(ICharacter character, IItem used, IItem usedWith, int[] potionIds);
        void DrinkPotion(ICharacter character, IItem current, IItem next, PotionSkillService.OnFinish finish, int drinkingTicks = 2, string? message = null);
        bool EmptyPotion(ICharacter character, IItem potion);
    }
}
