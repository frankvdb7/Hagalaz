using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Skills.Fishing
{
    public interface IFishingSkillService
    {
        Task<bool> TryFish(ICharacter character, INpc fishingSpot, IFishingSpotTable? table);
    }
}