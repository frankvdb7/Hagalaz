using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Skills.Fishing
{
    public interface IFishingSkillService
    {
        bool TryFish(ICharacter character, EntityHandle<ICreature> fishingSpotHandle, IFishingSpotTable? table, int characterCount);
    }
}
