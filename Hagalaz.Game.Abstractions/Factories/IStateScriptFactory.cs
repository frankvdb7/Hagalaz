using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.States;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Defines a contract for a factory that discovers and provides state scripts.
    /// These scripts define the behavior for various character or world states (e.g., being poisoned, a system-wide double XP event).
    /// </summary>
    public interface IStateScriptFactory
    {
        /// <summary>
        /// Retrieves an asynchronous enumerable of all discovered state scripts, pairing each state type with its corresponding script type.
        /// </summary>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of tuples, where each tuple contains a state's <see cref="Type"/> and the <see cref="Type"/> of the <see cref="IStateScript"/> that handles it.</returns>
        IAsyncEnumerable<(Type stateType, Type scriptType)> GetScripts();
    }
}