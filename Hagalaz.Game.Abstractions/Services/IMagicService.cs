using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMagicService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<CombatSpellDto>> FindAllCombatSpells();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<EnchantingSpellDto>> FindAllEnchantingSpells();

        Task<EnchantingSpellProductDto?> FindEnchantingSpellProductByButtonId(int buttonId);
    }
}