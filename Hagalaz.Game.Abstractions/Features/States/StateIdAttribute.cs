using System;

namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// An attribute used to associate a unique, stable identifier with a state class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class StateIdAttribute : Attribute
    {
        /// <summary>
        /// Gets the unique identifier for the state.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateIdAttribute"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the state, as a string.</param>
        public StateIdAttribute(string id) => Id = id;
    }
}
