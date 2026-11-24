using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Fletching recipe definitions.
    /// Definitions are preloaded at startup, so lookups are synchronous and never perform I/O on the game loop.
    /// </summary>
    public interface IFletchingService
    {
        /// <summary>
        /// Finds the recipe of a category made from the two given items, in either order.
        /// </summary>
        /// <param name="category">The recipe category.</param>
        /// <param name="itemId">The first item id.</param>
        /// <param name="otherItemId">The second item id.</param>
        /// <returns>The <see cref="FletchingDto"/> if found; otherwise, <c>null</c>.</returns>
        FletchingDto? FindRecipe(FletchingCategory category, int itemId, int otherItemId);

        /// <summary>
        /// Finds every recipe of a category.
        /// </summary>
        /// <param name="category">The recipe category.</param>
        /// <returns>The recipes of the category.</returns>
        IReadOnlyList<FletchingDto> FindRecipes(FletchingCategory category);
    }
}
