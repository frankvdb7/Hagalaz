namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Provides an explicit tick-based lifetime for a state.
    /// </summary>
    public interface ITimedState : IState
    {
        /// <summary>
        /// Gets or sets the number of game ticks remaining.
        /// </summary>
        int TicksLeft { get; set; }
    }
}
