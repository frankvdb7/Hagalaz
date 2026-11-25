using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Herblore skill data, such as herb and potion definitions.
    /// Potion definitions are preloaded at startup, so potion lookups are synchronous and never perform I/O on the game loop.
    /// </summary>
    public interface IHerbloreService
    {
        /// <summary>
        /// Finds a herb definition by the item ID of its grimy version.
        /// </summary>
        /// <param name="id">The item ID of the grimy herb.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="HerbDto"/> if found; otherwise, <c>null</c>.</returns>
        public Task<HerbDto?> FindGrimyHerbById(int id);

        /// <summary>
        /// Finds all herb definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all herb data transfer objects.</returns>
        public Task<IReadOnlyList<HerbDto>> FindAllHerbs();

        /// <summary>
        /// Finds every potion definition of a kind.
        /// </summary>
        /// <param name="kind">The recipe kind.</param>
        /// <returns>The potion definitions of the kind, ordered by potion ID.</returns>
        IReadOnlyList<PotionDto> FindPotions(PotionKind kind);

        /// <summary>
        /// Finds the standard potion whose unfinished potion is made from the given primary ingredient.
        /// </summary>
        /// <param name="primaryItemId">The item ID of the primary ingredient.</param>
        /// <returns>The first matching <see cref="PotionDto"/> by potion ID if found; otherwise, <c>null</c>.</returns>
        PotionDto? FindPotionByPrimaryItemId(int primaryItemId);

        /// <summary>
        /// Finds the potion of a kind made by adding the given secondary ingredient to the given base item.
        /// </summary>
        /// <param name="kind">The recipe kind.</param>
        /// <param name="unfinishedPotionId">The item ID of the unfinished potion (or the herb for an overload).</param>
        /// <param name="secondaryItemId">The item ID of the secondary ingredient.</param>
        /// <returns>The <see cref="PotionDto"/> if found; otherwise, <c>null</c>.</returns>
        PotionDto? FindPotionBySecondaryItemId(PotionKind kind, int unfinishedPotionId, int secondaryItemId);
    }
}
