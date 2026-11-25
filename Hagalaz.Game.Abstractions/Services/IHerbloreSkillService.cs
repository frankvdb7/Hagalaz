using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that handles the logic for the Herblore skill.
    /// </summary>
    public interface IHerbloreSkillService
    {
        /// <summary>
        /// Attempts to clean a grimy herb.
        /// </summary>
        /// <param name="character">The character cleaning the herb.</param>
        /// <param name="item">The grimy herb item.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task TryCleanHerb(
            ICharacter character,
            IItem item,
            System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to make an overload potion.
        /// </summary>
        /// <param name="character">The character making the potion.</param>
        /// <param name="herb">The herb item being used.</param>
        /// <param name="overload">The definition of the overload, whose secondary ingredients are all consumed.</param>
        /// <returns><c>true</c> if the action was successful; otherwise, <c>false</c>.</returns>
        bool MakeOverload(ICharacter character, IItem herb, PotionDto overload);

        /// <summary>
        /// Attempts to make a potion.
        /// </summary>
        /// <param name="character">The character making the potion.</param>
        /// <param name="vial">The vial item.</param>
        /// <param name="ingredient">The ingredient item.</param>
        /// <param name="potion">The definition of the potion to make.</param>
        /// <param name="makeUnfinished">A value indicating whether to make an unfinished potion.</param>
        /// <returns><c>true</c> if the action was successful; otherwise, <c>false</c>.</returns>
        bool MakePotion(ICharacter character, IItem vial, IItem ingredient, PotionDto potion, bool makeUnfinished);

        /// <summary>
        /// Queues a task for cleaning multiple herbs.
        /// </summary>
        /// <param name="character">The character cleaning the herbs.</param>
        /// <param name="definition">The definition of the herb to clean.</param>
        /// <param name="cleanCount">The number of herbs to clean.</param>
        /// <param name="tickDelay">The delay in game ticks between cleaning each herb.</param>
        /// <returns><c>true</c> if the task was successfully queued; otherwise, <c>false</c>.</returns>
        bool QueueCleanHerbTask(ICharacter character, HerbDto definition, int cleanCount, int tickDelay);
    }
}
