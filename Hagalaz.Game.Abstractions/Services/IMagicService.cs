using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Magic skill data, such as spell definitions.
    /// </summary>
    public interface IMagicService
    {
        /// <summary>
        /// Finds all combat spell definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all combat spell data transfer objects.</returns>
        Task<IReadOnlyList<CombatSpellDto>> FindAllCombatSpells();

        /// <summary>
        /// Finds all enchanting spell definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all enchanting spell data transfer objects.</returns>
        Task<IReadOnlyList<EnchantingSpellDto>> FindAllEnchantingSpells();

        /// <summary>
        /// Finds an enchanting spell product definition by its widget button ID.
        /// </summary>
        /// <param name="buttonId">The widget button ID associated with the enchanting product.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="EnchantingSpellProductDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<EnchantingSpellProductDto?> FindEnchantingSpellProductByButtonId(int buttonId);
    }
}