namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// Defines the contract for a handle to an NPC instance, providing access to the NPC and a method to unregister it from the game world.
    /// </summary>
    public interface INpcHandle
    {
        /// <summary>
        /// Gets the NPC instance associated with this handle.
        /// </summary>
        INpc Npc { get; }

        /// <summary>
        /// Unregisters and removes the NPC from the game world.
        /// </summary>
        void Unregister();
    }
}