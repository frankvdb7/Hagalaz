namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Hagalaz.GameAbstractions.Model.Creatures.ICreatureStatistics" />
    public interface INpcStatistics : ICreatureStatistics
    {
        /// <summary>
        /// Contains npc combat level.
        /// </summary>
        /// <value>The combat level.</value>
        int CombatLevel { get; set; }
        /// <summary>
        /// Get's max level of specific skill.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.Exception"></exception>
        int GetMaxSkillLevel(int skillID);
    }
}
