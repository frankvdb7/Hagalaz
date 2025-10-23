using System.Linq;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="IGameObjectDefinition"/> interface.
    /// </summary>
    public static class GameObjectDefinitionExtensions
    {
        /// <summary>
        /// Checks if the game object definition has a specific action available.
        /// </summary>
        /// <param name="definition">The game object definition to check.</param>
        /// <param name="action">The name of the action to look for (case-insensitive).</param>
        /// <returns><c>true</c> if the definition includes the specified action; otherwise, <c>false</c>.</returns>
        public static bool HasAction(this IGameObjectDefinition definition, string action) =>
            definition.Actions.Any(a => a != null && string.Compare(a, action, System.StringComparison.OrdinalIgnoreCase) == 0);
    }
}