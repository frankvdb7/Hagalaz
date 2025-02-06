using System.Linq;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Extensions
{
    public static class GameObjectDefinitionExtensions
    {
        /// <summary>
        /// Determines whether this definition has a specific action.
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="action">The action.</param>
        /// <returns>
        ///   <c>true</c> if this definition has the action; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasAction(this IGameObjectDefinition definition, string action) =>
            definition.Actions.Any(a => a != null && string.Compare(a, action, System.StringComparison.OrdinalIgnoreCase) == 0);
    }
}