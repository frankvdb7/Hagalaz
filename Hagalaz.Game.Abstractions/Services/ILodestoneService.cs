using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILodestoneService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<LodestoneDto>> FindAll();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        Task<LodestoneDto?> FindByGameObjectId(int id);
    }
}