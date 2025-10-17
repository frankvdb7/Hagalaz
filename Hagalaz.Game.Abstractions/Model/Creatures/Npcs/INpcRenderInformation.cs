namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// Defines the contract for an object that holds the client-side rendering information for an NPC.
    /// </summary>
    /// <seealso cref="Hagalaz.Game.Abstractions.Model.Creatures.ICreatureRenderInformation" />
    public interface INpcRenderInformation : ICreatureRenderInformation
    {
        /// <summary>
        /// Gets the combined bitmask of update flags that are currently scheduled for the NPC.
        /// </summary>
        UpdateFlags UpdateFlag { get; }

        /// <summary>
        /// Schedules a specific type of appearance update for the NPC using a bitmask flag.
        /// </summary>
        /// <param name="flag">The update flag to schedule.</param>
        void ScheduleFlagUpdate(UpdateFlags flag);

        /// <summary>
        /// Cancels a previously scheduled appearance update.
        /// </summary>
        /// <param name="flag">The update flag to cancel.</param>
        void CancelScheduledUpdate(UpdateFlags flag);
    }
}