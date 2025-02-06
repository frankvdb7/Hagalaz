using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    public interface IHerbloreSkillService
    {
        Task TryCleanHerb(ICharacter character, IItem item);
        PotionDto? GetPotionByPrimaryItemId(int primaryItemId);
        PotionDto? GetPotionBySecondaryItemId(int unfinishedPotionId, int secondaryItemId);
        PotionDto? GetPotionByUnfinishedPotionId(int unfinishedPotionId);
        bool MakeOverload(ICharacter character, IItem herb);
        bool MakePotion(ICharacter character, IItem vial, IItem ingredient, PotionDto potion, bool makeUnfinished);
        bool QueueCleanHerbTask(ICharacter character, HerbDto definition, int cleanCount, int tickDelay);
    }
}