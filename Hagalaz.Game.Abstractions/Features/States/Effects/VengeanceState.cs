namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    /// <summary>
    /// Represents the "Vengeance" state.
    /// </summary>
    [StateMetaData("vengeance-state")]
    public class VengeanceState : TimedState, IKeepLongestDurationState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VengeanceState"/> class.
        /// </summary>
        public VengeanceState()
        {
        }
    }
}
