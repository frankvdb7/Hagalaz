using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Crafting
{
    public interface ICraftingSkillService
    {
        Task TryBakePottery(ICharacter character);
        Task<bool> TryCraftLeather(ICharacter character, IItem resource);
        Task<bool> TryCutGem(ICharacter character, IItem uncut);
        Task TryFormPottery(ICharacter character);
        Task TrySpin(ICharacter character);
        Task TryTan(ICharacter character);
    }
}