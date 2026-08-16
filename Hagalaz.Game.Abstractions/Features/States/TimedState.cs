namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Provides the common implementation for a state with an explicit tick lifetime.
    /// </summary>
    public abstract class TimedState : ITimedState
    {
        /// <inheritdoc />
        public int TicksLeft { get; set; }
    }
}
