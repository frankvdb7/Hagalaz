namespace Hagalaz.Game.Abstractions.Logic.Characters.Model
{
    /// <summary>
    /// Represents the active, in-memory state of a player's summoned familiar.
    /// "Hydrated" refers to the fact that this is a live game object, as opposed to a dehydrated, persisted state.
    /// </summary>
    public record HydratedFamiliar
    {
        /// <summary>
        /// Gets the remaining points the familiar has for performing special moves.
        /// </summary>
        public int SpecialMovePoints { get; init; }

        /// <summary>
        /// Gets a value indicating whether the familiar is currently performing a special move.
        /// </summary>
        public bool IsUsingSpecialMove { get; init; }

        /// <summary>
        /// Gets the number of game ticks remaining until the familiar despawns.
        /// </summary>
        public int TicksRemaining { get; init; }
    }
}
