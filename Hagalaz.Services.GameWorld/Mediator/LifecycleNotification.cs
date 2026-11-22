using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;

namespace Hagalaz.Services.GameWorld.Mediator;

/// <summary>
/// Bounds infrastructure lifecycle sends. The consumer publishes to the broker, which retries while
/// disconnected, so the send is cancelled through the mediator instead of being abandoned.
/// </summary>
public static class LifecycleNotification
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    public static async Task SendAsync<TMessage>(
        IGameMediator mediator,
        TMessage message,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null) where TMessage : class
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(timeout ?? DefaultTimeout);
        await mediator.SendAsync(message, source.Token);
    }
}
