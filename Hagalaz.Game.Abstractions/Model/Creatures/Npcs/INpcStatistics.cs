namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// Defines the contract for managing an NPC's statistics.
    /// </summary>
    /// <seealso cref="Hagalaz.Game.Abstractions.Model.Creatures.ICreatureStatistics" />
    public interface INpcStatistics : ICreatureStatistics
    {
        /// <summary>
        /// Gets or sets the combat level of the NPC.
        /// </summary>
        int CombatLevel { get; set; }

        /// <summary>
        /// Gets the maximum level of a specific skill for the NPC, based on its definition.
        /// </summary>
        /// <param name="skillID">The ID of the skill.</param>
        /// <returns>The maximum level of the specified skill.</returns>
        int GetMaxSkillLevel(int skillID);
    }
}