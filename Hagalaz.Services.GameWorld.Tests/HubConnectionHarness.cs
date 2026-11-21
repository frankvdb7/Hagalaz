using System.IO.Pipelines;
using System.Security.Claims;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Features;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Raido.Common.Protocol;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Tests;

/// <summary>
///     Creates hub connection contexts for a character over in-memory pipes and aborts, cleans up and completes all of
///     them afterwards. Hub tests call <see cref="CleanupAsync"/> from their test cleanup.
/// </summary>
internal sealed class HubConnectionHarness
{
    private readonly List<RaidoHubConnectionContext> _connections = [];
    private readonly List<(Pipe Input, Pipe Output)> _transports = [];

    public RaidoHubConnectionContext CreateConnection(ICharacter character, string connectionIdPrefix, ClaimsPrincipal? user = null)
    {
        var features = new FeatureCollection();
        features.Set<IConnectionUserFeature>(new ConnectionUserFeature
        {
            User = user ?? new ClaimsPrincipal(new ClaimsIdentity([], "test"))
        });
        var rawConnection = Substitute.For<ConnectionContext>();
        rawConnection.ConnectionId.Returns($"{connectionIdPrefix}-{_connections.Count}");
        rawConnection.Features.Returns(features);
        var input = new Pipe();
        var output = new Pipe();
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output.Writer);
        rawConnection.Transport.Returns(transport);
        rawConnection.ConnectionClosed.Returns(CancellationToken.None);
        _transports.Add((input, output));

        var options = new RaidoConnectionContextOptions();
        var tcpConnection = new RaidoTcpConnectionContext(options, NullLoggerFactory.Instance);
        Assert.IsTrue(tcpConnection.TryAttachPhysicalConnection(rawConnection));
        var connection = new RaidoHubConnectionContext(
            tcpConnection,
            options,
            Substitute.For<IRaidoProtocol>(),
            NullLoggerFactory.Instance,
            TimeProvider.System);
        connection.Features.Set<ICharacterFeature>(new CharacterFeature { Character = character });
        _connections.Add(connection);
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

/// <summary>
///     A settable connection user for hub tests.
/// </summary>
internal sealed class ConnectionUserFeature : IConnectionUserFeature
{
    public ClaimsPrincipal? User { get; set; }
}
