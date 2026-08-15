namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Declares how a state type handles a second application while already active.
    /// </summary>
    public interface IStateReapplicationPolicy : IState
    {
        /// <summary>
        /// Gets the policy used when another instance of the same concrete state type is applied.
        /// </summary>
        StateReapplicationPolicy ReapplicationPolicy { get; }
    }
}
