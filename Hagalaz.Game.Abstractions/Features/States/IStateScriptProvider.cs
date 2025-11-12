using System;
using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Defines a contract for a provider that maps a <see cref="IState"/> type to its corresponding <see cref="IStateScript"/> implementation type.
    /// </summary>
    public interface IStateScriptProvider
    {
        /// <summary>
        /// Finds the script type associated with a given state type.
        /// </summary>
        /// <param name="stateType">The <see cref="Type"/> of the state to find the script for.</param>
        /// <returns>The <see cref="Type"/> of the script that implements the logic for the specified state type.</returns>
        Type FindByType(Type stateType);

        /// <summary>
        /// Gets all of the available state types.
        /// </summary>
        /// <returns>A collection of all of the state types.</returns>
        IEnumerable<Type> GetAllStateTypes();
    }
}