using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using NSubstitute;
using Raido.Server;

namespace Hagalaz.TestSupport;

/// <summary>
///     Remembers the hub connections and in-memory pipes a test created, so the test cleanup can abort and complete
///     all of them.
/// </summary>
internal sealed class TrackedConnections
{
    private readonly List<RaidoHubConnectionContext> _connections = [];
    private readonly List<(Pipe Input, Pipe Output)> _transports = [];

    public void Add(RaidoHubConnectionContext connection) => _connections.Add(connection);

    public void Add(Pipe input, Pipe output) => _transports.Add((input, output));

    /// <summary>
    ///     A raw connection with the given id over fresh in-memory pipes, which are tracked for cleanup.
    /// </summary>
    public ConnectionContext CreateRawConnection(string id)
    {
        var connection = Substitute.For<ConnectionContext>();
        connection.ConnectionId.Returns(id);
        var transport = Substitute.For<IDuplexPipe>();
        var input = new Pipe();
        var output = new Pipe();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output.Writer);
        connection.Transport.Returns(transport);
        connection.ConnectionClosed.Returns(CancellationToken.None);
        Add(input, output);
        return connection;
    }

    public async Task CleanupAsync()
    {
        foreach (var connection in _connections)
        {
            connection.Abort();
            await connection.CleanupAsync();
        }

        foreach (var (input, output) in _transports)
        {
            input.Reader.Complete();
            input.Writer.Complete();
            output.Reader.Complete();
            output.Writer.Complete();
        }
    }
}
