using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Scripts.Model.States
{
    /// <summary>
    /// Base class for a state.
    /// </summary>
    public abstract class StateScriptBase : IStateScript
    {
        /// <summary>
        /// Gets called when the state is added.
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="creature">The creature.</param>
        public virtual void OnStateAdded(IState state, ICreature creature) { }
        /// <summary>
        /// Gets called when the state is removed.
        /// By default, this method invokes the OnRemoveCallback of the state, if any.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="creature">The creature.</param>
        public virtual void OnStateRemoved(IState state, ICreature creature) { }
        /// <summary>
        /// Determines whether this instance is serializable.
        /// By default, the state is not serializable.
        /// </summary>
        /// <returns></returns>
        public virtual bool IsSerializable() => false;
    }
}
