namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    /// <summary>
    /// Represents the "Anti-Dragonfire Potion" state.
    /// </summary>
    [StateMetaData("anti-dragonfire-potion")]
    public class AntiDragonfirePotionState : TimedState, IKeepLongestDurationState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AntiDragonfirePotionState"/> class.
        /// </summary>
        public AntiDragonfirePotionState()
        {
        }
    }
}
