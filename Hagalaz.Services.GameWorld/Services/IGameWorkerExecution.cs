using System.Threading.Tasks;

namespace Hagalaz.Services.GameWorld.Services;

/// <summary>
/// Exposes the terminal boundary of the GameWorker execution task.
/// </summary>
public interface IGameWorkerExecution
{
    Task ExecutionCompleted { get; }
}
