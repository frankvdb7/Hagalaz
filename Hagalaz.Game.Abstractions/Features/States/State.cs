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
        public virtual int TicksLeft { get; set; }

        /// <inheritdoc />
        public virtual void Tick()
        {
            if (TicksLeft > 0)
            {
                TicksLeft--;
            }
        }

        /// <inheritdoc />
        public virtual void OnStateRemoved(IState state, ICreature creature)
        {
        }

        /// <inheritdoc />
        public virtual void OnStateAdded(IState state, ICreature creature)
        {
        }
    }
}
