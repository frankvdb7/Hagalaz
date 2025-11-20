namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    /// <summary>
    /// Represents the "Frozen" state, where a creature cannot move.
    /// </summary>
    [StateMetaData("frozen-state")]
    public class FrozenState : State
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FrozenState"/> class.
        /// </summary>
        public FrozenState()
        {
        }
    }
}
