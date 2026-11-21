using System.Security.Claims;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.TestSupport;
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
    private readonly TrackedConnections _tracked = new();
    private int _count;

    public RaidoHubConnectionContext CreateConnection(ICharacter character, string connectionIdPrefix, ClaimsPrincipal? user = null)
    {
        var rawConnection = _tracked.CreateRawConnection($"{connectionIdPrefix}-{_count++}");
        var features = new FeatureCollection();
        features.Set<IConnectionUserFeature>(new ConnectionUserFeature
        {
            User = user ?? new ClaimsPrincipal(new ClaimsIdentity([], "test"))
        });
        rawConnection.Features.Returns(features);

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
        _tracked.Add(connection);
        return connection;
    }

    public Task CleanupAsync() => _tracked.CleanupAsync();
}

/// <summary>
///     A settable connection user for hub tests.
/// </summary>
internal sealed class ConnectionUserFeature : IConnectionUserFeature
{
    public ClaimsPrincipal? User { get; set; }
}
