using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Provides a base implementation for states that also act as their own scripts.
    /// </summary>
    public abstract class ScriptedState : State, IStateScript
    {
        /// <inheritdoc />
        public override IStateScript Script => this;

        /// <inheritdoc />
        public virtual void OnStateAdded(IState state, ICreature creature)
        {
            // Default implementation does nothing.
        }

        /// <inheritdoc />
        public virtual void OnStateRemoved(IState state, ICreature creature)
        {
            // Default implementation does nothing.
        }

        /// <inheritdoc />
        public virtual bool IsSerializable() => false;
    }
}
