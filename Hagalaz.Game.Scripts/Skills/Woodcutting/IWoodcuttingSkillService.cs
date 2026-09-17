using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Woodcutting
{
    public interface IWoodcuttingSkillService
    {
        Task StartCuttingAsync(
            ICharacter character,
            EntityHandle<IGameObject> treeHandle,
            System.Threading.CancellationToken cancellationToken = default);
    }
}
