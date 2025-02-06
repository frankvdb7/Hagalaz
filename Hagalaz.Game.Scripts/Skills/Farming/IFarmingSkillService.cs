using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Farming
{
    public interface IFarmingSkillService
    {
        void HandlePatchClickPerform(ICharacter character, IGameObject obj, GameObjectClickType clickType);
        Task<bool> HandlePatchItem(ICharacter character, IItem item, IGameObject obj);
    }
}