using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    public interface IHerbloreService
    {
        public Task<HerbDto?> FindGrimyHerbById(int id);
        public Task<IReadOnlyList<HerbDto>> FindAllHerbs();
        Task<IReadOnlyList<PotionDto>> FindAllPotions();
    }
}
