using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Crafting
{
    public interface ICraftingSkillService
    {
        Task TryBakePottery(ICharacter character, System.Threading.CancellationToken cancellationToken = default);
        Task<bool> TryCraftLeather(ICharacter character, IItem resource);
        Task<bool> TryCutGem(ICharacter character, IItem uncut);
        Task TryFormPottery(ICharacter character, System.Threading.CancellationToken cancellationToken = default);
        Task TrySpin(ICharacter character, System.Threading.CancellationToken cancellationToken = default);
        Task TryTan(ICharacter character, System.Threading.CancellationToken cancellationToken = default);
    }
}
