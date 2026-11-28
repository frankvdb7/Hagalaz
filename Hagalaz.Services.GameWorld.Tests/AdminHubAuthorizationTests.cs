using System.Security.Claims;
using Hagalaz.Authorization.Constants;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Common.Events.Character.Packet;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Hubs;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Raido.Server;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class AdminHubAuthorizationTests
{
    private readonly HubConnectionHarness _hubConnections = new();

    [TestCleanup]
    public async Task CleanupConnections()
    {
        await _hubConnections.CleanupAsync();
    }

    [TestMethod]
    public async Task AdminHub_OnCommand_RejectsAuthenticatedNonAdmin()
    {
        using var provider = CreateProvider();
        var eventManager = Substitute.For<IEventManager>();
        var connection = CreateConnection(eventManager, Roles.Donator);

        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(
            connection,
            new ConsoleCommandMessage { Command = "::dangerous-command" });

        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
    }

    [TestMethod]
    public async Task AdminHub_OnCommand_AllowsModeratorAndPublishesCommandEvent()
    {
        using var provider = CreateProvider();
        var eventManager = Substitute.For<IEventManager>();
        var sentEvent = default(IEvent);
        eventManager.SendEvent(Arg.Do<IEvent>(e => sentEvent = e)).Returns(true);
        var connection = CreateConnection(eventManager, Roles.GameModerator);

        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(
            connection,
            new ConsoleCommandMessage { Command = "::safe-command" });

        eventManager.Received(1).SendEvent(Arg.Any<IEvent>());
        var commandEvent = Assert.IsInstanceOfType<ConsoleCommandEvent>(sentEvent);
        Assert.AreEqual("::safe-command", commandEvent.Command);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRaidoServer().AddHub<AdminHub>();
        return services.BuildServiceProvider();
    }

    private RaidoHubConnectionContext CreateConnection(IEventManager eventManager, string role) =>
        _hubConnections.CreateConnection(
            CreateCharacter(eventManager),
            "admin-hub-test",
            new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "test")));

    private static ICharacter CreateCharacter(IEventManager eventManager)
    {
        var character = Substitute.For<ICharacter>();
        character.EventManager.Returns(eventManager);
        return character;
    }

}
