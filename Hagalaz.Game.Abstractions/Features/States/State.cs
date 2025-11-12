using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Provides a base implementation of the <see cref="IState"/> interface.
    /// </summary>
    public abstract class State : IState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="State"/> class.
        /// </summary>
        protected State()
        {
        }

        /// <inheritdoc />
        public virtual void OnApply(ICreature creature)
        {
            // Default implementation does nothing.
        }

        /// <inheritdoc />
        public virtual void OnRemove(ICreature creature)
        {
            // Default implementation does nothing.
        }

        /// <inheritdoc />
        public virtual void OnTick(ICreature creature)
        {
            // Default implementation does nothing.
        }
    }
}
