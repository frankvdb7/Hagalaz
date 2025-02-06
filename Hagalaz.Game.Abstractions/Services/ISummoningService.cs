using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISummoningService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="npcId"></param>
        /// <returns></returns>
        Task<SummoningDto?> FindDefinitionByNpcId(int npcId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pouchId"></param>
        /// <returns></returns>
        Task<SummoningDto?> FindDefinitionByPouchId(int pouchId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scrollId"></param>
        /// <returns></returns>
        Task<SummoningDto?> FindDefinitionByScrollId(int scrollId);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<SummoningDto>> FindAllDefinitions();
    }
}