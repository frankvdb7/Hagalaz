using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that retrieves area scripts, which contain logic for specific map areas.
    /// </summary>
    public interface IAreaScriptProvider
    {
        /// <summary>
        /// Finds and retrieves an area script by its unique identifier.
        /// </summary>
        /// <param name="areaId">The unique identifier of the area.</param>
        /// <returns>The <see cref="IAreaScript"/> associated with the specified area ID, or a default/null script if not found.</returns>
        IAreaScript FindAreaScriptById(int areaId);
    }
}