using System;
using Raido.Common.Protocol;

namespace Raido.Server;

/// <summary>
/// Describes which logical Raido connection should receive an accepted physical connection.
/// </summary>
public sealed class RaidoConnectionSelection
{
    private RaidoConnectionSelection(
        IRaidoProtocol? protocol,
        bool statefulReconnect,
        RaidoHubConnectionContext? existingConnection)
    {
        Protocol = protocol;
        StatefulReconnect = statefulReconnect;
        ExistingConnection = existingConnection;
    }

    public IRaidoProtocol? Protocol { get; }

    public bool StatefulReconnect { get; }

    public RaidoHubConnectionContext? ExistingConnection { get; }

    public bool IsRejected => Protocol is null && ExistingConnection is null;

    public static RaidoConnectionSelection New(IRaidoProtocol protocol, bool statefulReconnect)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        return new RaidoConnectionSelection(protocol, statefulReconnect, existingConnection: null);
    }

    public static RaidoConnectionSelection Existing(RaidoHubConnectionContext connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return new RaidoConnectionSelection(protocol: null, statefulReconnect: false, connection);
    }

    public static RaidoConnectionSelection Rejected() =>
        new(protocol: null, statefulReconnect: false, existingConnection: null);
}
