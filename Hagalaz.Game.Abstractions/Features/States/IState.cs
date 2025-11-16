namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Defines the contract for a temporary state or effect that can be applied to a creature.
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Gets or sets the number of ticks remaining for the state.
        /// </summary>
        int TicksLeft { get; set; }

        /// <summary>
        /// Gets the script associated with this state.
        /// </summary>
        IStateScript Script { get; }

        /// <summary>
        /// Performs a tick update for the state.
        /// </summary>
        void Tick();
    }
}
