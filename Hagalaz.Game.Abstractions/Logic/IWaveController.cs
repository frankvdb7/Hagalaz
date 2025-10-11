namespace Hagalaz.Game.Abstractions.Logic
{
    /// <summary>
    /// Defines a contract for a controller that manages waves of NPCs, typically for a minigame or event.
    /// </summary>
    public interface IWaveController
    {
        /// <summary>
        /// Spawns the next wave of NPCs.
        /// </summary>
        void SpawnNextWave();

        /// <summary>
        /// Stops the wave spawning process and cleans up any active NPCs.
        /// </summary>
        void StopWaves();

        /// <summary>
        /// Gets the identifier of the current wave.
        /// </summary>
        int CurrentWaveId { get; }

        /// <summary>
        /// Gets the identifier of the final wave in the sequence.
        /// </summary>
        int FinalWaveId { get; }
    }
}
