using System;
using System.Threading;
using System.Threading.Tasks;
using Raido.Common.Protocol;

namespace Raido.Server;

/// <summary>
/// An explicit, one-generation sender used only while a physical reconnect handoff owns the
/// logical write lock.
/// </summary>
internal sealed class RaidoReconnectClientProxy : IRaidoClientProxy
{
    private readonly RaidoPhysicalConnectionSession _session;
    private readonly IRaidoProtocol _protocol;
    private int _valid = 1;

    public RaidoReconnectClientProxy(RaidoPhysicalConnectionSession session, IRaidoProtocol protocol)
    {
        _session = session;
        _protocol = protocol;
    }

    public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : RaidoMessage
    {
        if (Volatile.Read(ref _valid) == 0)
        {
            return Task.FromException(new InvalidOperationException("The reconnect sender is no longer valid."));
        }

        return _session.SendReconnectAsync(message, _protocol, cancellationToken);
    }

    public void Invalidate() => Volatile.Write(ref _valid, 0);
}
