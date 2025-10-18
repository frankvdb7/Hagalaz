using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Prayer skill data, such as individual prayer and curse definitions.
    /// </summary>
    public interface IPrayerService
    {
        /// <summary>
        /// Finds a prayer or curse definition by its ID.
        /// </summary>
        /// <param name="itemId">The ID of the prayer or curse.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="PrayerDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<PrayerDto?> FindById(int itemId);

        /// <summary>
        /// Finds all prayer or curse definitions of a specific type (e.g., standard prayers, ancient curses).
        /// </summary>
        /// <param name="definitionType">The type of prayer definition to find.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all prayer data transfer objects for the specified type.</returns>
        Task<IReadOnlyList<PrayerDto>> FindAllByType(PrayerDtoType definitionType);
    }
}