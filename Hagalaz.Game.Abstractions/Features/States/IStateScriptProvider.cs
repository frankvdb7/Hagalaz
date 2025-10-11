using System;

namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Defines a contract for a provider that maps a <see cref="StateType"/> enum to its corresponding <see cref="IStateScript"/> implementation type.
    /// </summary>
    public interface IStateScriptProvider
    {
        /// <summary>
        /// Finds the script type associated with a given state type.
        /// </summary>
        /// <param name="type">The <see cref="StateType"/> to find the script for.</param>
        /// <returns>The <see cref="Type"/> of the script that implements the logic for the specified state type.</returns>
        Type FindByType(StateType type);
    }
}