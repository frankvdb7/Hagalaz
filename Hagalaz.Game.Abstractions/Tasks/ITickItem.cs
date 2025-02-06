namespace Hagalaz.Game.Abstractions.Tasks
{
    public interface ITickItem
    {
        /// <summary>
        /// Performs a single tick on this instance.
        /// </summary>
        void Tick();
    }
}