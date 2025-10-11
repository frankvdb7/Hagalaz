using System;
using Hagalaz.Game.Abstractions.Features.States;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// An attribute used to associate a state script class with one or more <see cref="StateType"/> enums.
    /// This allows for automatic discovery and mapping of scripts to their corresponding states.
    /// </summary>
    /// <param name="stateTypes">An array of <see cref="StateType"/> values that this script handles.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class StateScriptMetaData(StateType[] stateTypes) : Attribute
    {
        /// <summary>
        /// Gets the array of state types that this script is associated with.
        /// </summary>
        public StateType[] StateTypes { get; } = stateTypes;
    }
}