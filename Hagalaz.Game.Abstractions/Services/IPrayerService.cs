using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPrayerService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        Task<PrayerDto?> FindById(int itemId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="definitionType"></param>
        /// <returns></returns>
        Task<IReadOnlyList<PrayerDto>> FindAllByType(PrayerDtoType definitionType);
    }
}