namespace Hagalaz.Game.Abstractions.Builders.Npc
{
    /// <summary>
    /// Defines the contract for an NPC builder, which serves as the entry point
    /// for constructing an <see cref="Model.Creatures.Npcs.INpc"/> object using a fluent interface.
    /// </summary>
    public interface INpcBuilder
    {
        /// <summary>
        /// Begins the process of building a new NPC.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the NPC's ID.</returns>
        INpcId Create();
    }
}