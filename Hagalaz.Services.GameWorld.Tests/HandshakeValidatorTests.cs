using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Network.Handshake;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raido.Common.Messages;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class HandshakeValidatorTests
{
    [TestMethod]
    public void DefaultHandshakeValidator_WhenRevisionDiffers_ReturnsOutdated()
    {
        var systemUpdate = Substitute.For<ISystemUpdateService>();
        var validator = new DefaultHandshakeValidator<WorldReconnectRequest>(
            Options.Create(new ServerConfig { ClientRevision = 742, ClientRevisionPatch = 1 }),
            systemUpdate);

        var response = validator.Validate(new WorldReconnectRequest
        {
            ClientRevision = 741,
            ClientRevisionPatch = 1
        });

        Assert.AreSame(ClientSignInResponse.Outdated, response);
    }

    [TestMethod]
    public void DefaultHandshakeValidator_WhenSystemUpdateIsScheduled_ReturnsSystemUpdate()
    {
        var systemUpdate = Substitute.For<ISystemUpdateService>();
        systemUpdate.SystemUpdateScheduled.Returns(true);
        var validator = new DefaultHandshakeValidator<WorldSignInRequest>(
            Options.Create(new ServerConfig { ClientRevision = 742, ClientRevisionPatch = 1 }),
            systemUpdate);

        var response = validator.Validate(new WorldSignInRequest
        {
            ClientRevision = 742,
            ClientRevisionPatch = 1
        });

        Assert.AreSame(ClientSignInResponse.SystemUpdate, response);
    }
}
