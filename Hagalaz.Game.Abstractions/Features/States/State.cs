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

        /// <inheritdoc />
        public virtual int TicksLeft { get; set; }

        /// <inheritdoc />
        public abstract IStateScript Script { get; }

        /// <inheritdoc />
        public virtual bool Removed { get; protected set; } = false;

        /// <inheritdoc />
        public virtual int RemoveDelay { get; protected set; }

        /// <inheritdoc />
        public virtual void Tick()
        {
            if (TicksLeft > 0)
            {
                TicksLeft--;
            }
            if (TicksLeft <= 0)
            {
                Removed = true;
            }
        }
    }
}
