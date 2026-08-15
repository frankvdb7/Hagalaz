namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    /// <summary>
    /// Represents the "TeleBlocked" state.
    /// </summary>
    [StateMetaData("teleblocked-state")]
    public class TeleBlockedState : TimedState, IKeepLongestDurationState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TeleBlockedState"/> class.
        /// </summary>
        public TeleBlockedState()
        {
        }
    }
}
