using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.States;

namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Owns the states attached to one creature and their synchronous lifecycle.
    /// </summary>
    public interface ICreatureStateCollection
    {
        /// <summary>
        /// Gets the active state instances.
        /// </summary>
        IEnumerable<IState> States { get; }

        /// <summary>
        /// Determines whether a concrete state type is active.
        /// </summary>
        bool Has(Type stateType);

        /// <summary>
        /// Applies a state using its declared reapplication policy.
        /// </summary>
        void Add(IState state);

        /// <summary>
        /// Removes a concrete state type if it is active.
        /// </summary>
        void Remove(Type stateType);

        /// <summary>
        /// Processes timed and custom-tickable states synchronously.
        /// </summary>
        void ProcessTick();
    }
}
