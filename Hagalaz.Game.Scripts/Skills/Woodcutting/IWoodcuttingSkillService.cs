using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Woodcutting
{
    public interface IWoodcuttingSkillService
    {
        Task<bool> StartCutting(ICharacter character, IGameObject tree);
    }
}