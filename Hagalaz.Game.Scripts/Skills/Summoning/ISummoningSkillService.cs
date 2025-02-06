using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Summoning
{
    public interface ISummoningSkillService
    {
        bool CanCreatePouch(ICharacter character, SummoningDto definition, int itemId, int count);
        bool CanCreateScroll(ICharacter character, SummoningDto definition);
        Task SummonFamiliar(ICharacter character, IItem item);
    }
}