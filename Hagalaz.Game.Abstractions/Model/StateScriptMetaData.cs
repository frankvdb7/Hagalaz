using System;
using Hagalaz.Game.Abstractions.Features.States;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// An attribute used to associate a state script class with an <see cref="IState"/> type.
    /// This allows for automatic discovery and mapping of scripts to their corresponding states.
    /// </summary>
    /// <param name="stateType">The type of the state that this script handles. Must implement <see cref="IState"/>.</param>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class StateScriptMetaData(Type stateType) : Attribute
    {
        /// <summary>
        /// Gets the type of the state that this script is associated with.
        /// </summary>
        public Type StateType { get; } = stateType;
    }
}