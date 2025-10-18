namespace Hagalaz.Game.Abstractions.Tasks
{
    /// <summary>
    /// Defines a contract for an object that can be processed on each game tick.
    /// This is a fundamental interface for any object that needs to be updated periodically by the main game loop.
    /// </summary>
    public interface ITickItem
    {
        /// <summary>
        /// Executes a single unit of work or logic for the current game tick.
        /// </summary>
        void Tick();
    }
}