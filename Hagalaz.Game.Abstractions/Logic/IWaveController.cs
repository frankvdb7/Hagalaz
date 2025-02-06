namespace Hagalaz.Game.Abstractions.Logic
{
    public interface IWaveController
    {
        void SpawnNextWave();
        void StopWaves();
        int CurrentWaveId { get; }
        int FinalWaveId { get; }
    }
}
