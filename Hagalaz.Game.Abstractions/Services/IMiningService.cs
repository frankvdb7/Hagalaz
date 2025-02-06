using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMiningService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<OreDto>> FindAllOres();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rockId"></param>
        /// <returns></returns>
        Task<OreDto?> FindOreByRockId(int rockId);

        Task<RockDto?> FindRockById(int rockId);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<PickaxeDto>> FindAllPickaxes();

        Task<IReadOnlyList<RockDto>> FindAllRocks();

        Task<ILootTable?> FindRockLootById(int rockId);
    }
}