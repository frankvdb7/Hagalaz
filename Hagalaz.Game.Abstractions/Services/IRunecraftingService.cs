using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface IRunecraftingService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<RunecraftingDto>> FindAllAltars();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="altarId"></param>
        /// <returns></returns>
        Task<RunecraftingDto?> FindAltarById(int altarId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ruinId"></param>
        /// <returns></returns>
        Task<RunecraftingDto?> FindAltarByRuinId(int ruinId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="riftId"></param>
        /// <returns></returns>
        Task<RunecraftingDto?> FindAltarByRiftId(int riftId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="portalId"></param>
        /// <returns></returns>
        Task<RunecraftingDto?> FindAltarByPortalId(int portalId);
    }
}