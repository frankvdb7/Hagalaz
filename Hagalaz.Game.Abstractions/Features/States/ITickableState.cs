namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Provides custom behavior that runs during creature state processing.
    /// </summary>
    public interface ITickableState : IState
    {
        /// <summary>
        /// Performs one synchronous game-tick update.
        /// </summary>
        void Tick();
    }
}
