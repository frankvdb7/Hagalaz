namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    /// <summary>
    /// Represents the "Can't Cast Vengeance" state.
    /// </summary>
    [StateMetaData("cant-cast-vengeance-state")]
    public class CantCastVengeanceState : TimedState, IKeepLongestDurationState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CantCastVengeanceState"/> class.
        /// </summary>
        public CantCastVengeanceState()
        {
        }
    }
}
