namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// Defines the contract for a quest's static definition data.
    /// </summary>
    public interface IQuestDefinition
    {
        /// <summary>
        /// Gets the name of the quest.
        /// </summary>
        public string Name { get; }
    }
}