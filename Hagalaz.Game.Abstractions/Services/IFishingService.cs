using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface IFishingService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="npcId"></param>
        /// <returns></returns>
        Task<IFishingSpotTable?> FindSpotByNpcId(int npcId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="npcId"></param>
        /// <param name="clickType"></param>
        /// <returns></returns>
        Task<IFishingSpotTable?> FindSpotByNpcIdClickType(int npcId, NpcClickType clickType);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<IFishingSpotTable>> FindAllSpots();
    }
}