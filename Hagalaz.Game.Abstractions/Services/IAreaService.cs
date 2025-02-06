using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface IAreaService
    {
        /// <summary>
        /// Gets the area.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns></returns>
        IArea FindAreaByLocation(ILocation location);
    }
}