using System.IO.Pipelines;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Raido.Server;

/// <summary>
/// The physical transport owned by one active Raido transport generation.
/// </summary>
internal sealed class RaidoPhysicalTransport
{
    public RaidoPhysicalTransport(ConnectionContext connection)
    {
        Connection = connection;
    }

    public ConnectionContext Connection { get; }

    public string ConnectionId => Connection.ConnectionId;

    public IDuplexPipe Transport => Connection.Transport;

    public CancellationToken ConnectionClosed => Connection.ConnectionClosed;

    public IFeatureCollection Features => Connection.Features;

    public IPEndPoint? LocalEndPoint => Connection.LocalEndPoint as IPEndPoint;

    public IPEndPoint? RemoteEndPoint => Connection.RemoteEndPoint as IPEndPoint;
}
