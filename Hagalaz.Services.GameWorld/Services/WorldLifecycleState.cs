using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Services.Abstractions;

namespace Hagalaz.Services.GameWorld.Services;

public sealed class WorldLifecycleState : IStartupTaskState
{
    private readonly TaskCompletionSource<bool> _initialization = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _applicationStart = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _applicationStarted;
    private int _initializationStarted;
    private int _initializationCompleted;
    private int _registrationHealthy;
    private int _stopping;

    public bool IsApplicationStarted => Volatile.Read(ref _applicationStarted) == 1;
    public bool IsInitializationStarted => Volatile.Read(ref _initializationStarted) == 1;
    public bool IsInitializationCompleted => Volatile.Read(ref _initializationCompleted) == 1;
    public bool IsRegistrationHealthy => Volatile.Read(ref _registrationHealthy) == 1;
    public bool IsStopping => Volatile.Read(ref _stopping) == 1;
    public bool CanAcceptWorldSignIns => IsApplicationStarted && IsInitializationCompleted && IsRegistrationHealthy && !IsStopping;

    public void MarkApplicationStarted()
    {
        Interlocked.Exchange(ref _applicationStarted, 1);
        _applicationStart.TrySetResult();
    }

    public void MarkRegistrationSucceeded()
    {
        if (!IsStopping)
        {
            Interlocked.Exchange(ref _registrationHealthy, 1);
        }
    }

    public void MarkRegistrationFailed() => Interlocked.Exchange(ref _registrationHealthy, 0);

    public void MarkStopping()
    {
        Interlocked.Exchange(ref _stopping, 1);
        Interlocked.Exchange(ref _registrationHealthy, 0);
    }

    public Task<bool> WaitForInitializationAsync(CancellationToken cancellationToken = default) =>
        _initialization.Task.WaitAsync(cancellationToken);

    public Task WaitForApplicationStartedAsync(CancellationToken cancellationToken = default) =>
        _applicationStart.Task.WaitAsync(cancellationToken);

    public void MarkStarted()
    {
        Interlocked.Exchange(ref _initializationStarted, 1);
    }

    public void MarkCompleted()
    {
        Interlocked.Exchange(ref _initializationCompleted, 1);
        _initialization.TrySetResult(true);
    }

    public void MarkFailed()
    {
        _initialization.TrySetResult(false);
    }
}
