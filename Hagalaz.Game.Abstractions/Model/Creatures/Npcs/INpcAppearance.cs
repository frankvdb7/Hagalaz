namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// Defines the contract for managing an NPC's visual appearance, including transformations.
    /// </summary>
    /// <seealso cref="Hagalaz.Game.Abstractions.Model.Creatures.ICreatureAppearance" />
    public interface INpcAppearance : ICreatureAppearance
    {
        /// <summary>
        /// Gets the current composite (NPC) ID that defines the NPC's appearance.
        /// </summary>
        int CompositeID { get; }

        /// <summary>
        /// Transforms this NPC's appearance into that of another NPC.
        /// </summary>
        /// <param name="compositeID">The composite ID of the new appearance.</param>
        void Transform(int compositeID);
    }
}