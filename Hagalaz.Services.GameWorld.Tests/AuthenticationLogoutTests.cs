using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Authorization.Messages;
using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Hubs;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using MassTransit;
using NSubstitute;
using Polly;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class AuthenticationLogoutTests
{
    [TestMethod]
    [Timeout(5000)]
    public async Task SignOutAsync_WhenPersistenceFails_RemovesSessionButKeepsCharacterLiveForRetry()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        var characterService = Substitute.For<ICharacterService>();
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        var persistenceFailure = new InvalidOperationException("Persistence is unavailable.");
        persistenceService.PersistAsync(character, true, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(persistenceFailure));
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.RemoveSession("connection").Returns(Task.FromResult(true));
        var contextAccessor = CreateContextAccessor(character, session);
        var service = CreateAuthenticationService(
            characterService,
            persistenceService,
            gameSessionService,
            contextAccessor);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.SignOutAsync());

        Assert.AreSame(persistenceFailure, exception);
        await gameSessionService.Received(1).RemoveSession("connection");
        await characterService.DidNotReceive().RemoveAsync(character);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task OnDisconnectedAsync_WhenSignOutFails_DoesNotDestroyRegisteredCharacter()
    {
        var character = Substitute.For<ICharacter>();
        character.IsDestroyed.Returns(false);
        var authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService.SignOutAsync().Returns(Task.FromException(new InvalidOperationException("Sign out failed.")));
        var hub = new ConnectionHub(authenticationService);
        SetContext(hub, CreateContext(character, session: null));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => hub.OnDisconnectedAsync(null));

        character.DidNotReceive().Destroy();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task OnDisconnectedAsync_WhenSignOutSucceeds_DestroysCharacter()
    {
        var character = Substitute.For<ICharacter>();
        character.IsDestroyed.Returns(false);
        var authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService.SignOutAsync().Returns(Task.CompletedTask);
        var hub = new ConnectionHub(authenticationService);
        SetContext(hub, CreateContext(character, session: null));

        await hub.OnDisconnectedAsync(null);

        character.Received(1).Destroy();
    }

    private static AuthenticationService CreateAuthenticationService(
        ICharacterService characterService,
        ICharacterPersistenceService persistenceService,
        IGameSessionService gameSessionService,
        IRaidoCallerContextAccessor contextAccessor) =>
        new(
            NullLogger<AuthenticationService>.Instance,
            Substitute.For<AutoMapper.IMapper>(),
            characterService,
            Substitute.For<ICharacterFactory>(),
            Substitute.For<ICharacterHydrationService>(),
            persistenceService,
            gameSessionService,
            Substitute.For<IRequestClient<SignInUserRequestMessage>>(),
            Substitute.For<IRequestClient<GetUserInfoRequestMessage>>(),
            Substitute.For<IRequestClient<RevokeTokenRequestMessage>>(),
            Substitute.For<IRequestClient<HydrateCharacter>>(),
            Substitute.For<IClaimsPrincipalFactory>(),
            contextAccessor,
            Substitute.For<IGameMediator>(),
            new ResiliencePipelineBuilder().Build(),
            new ResiliencePipelineBuilder().Build());

    private static IRaidoCallerContextAccessor CreateContextAccessor(ICharacter character, IGameSession session)
    {
        var accessor = Substitute.For<IRaidoCallerContextAccessor>();
        var context = CreateContext(character, session);
        accessor.Context.Returns(context);
        return accessor;
    }

    private static RaidoCallerContext CreateContext(ICharacter character, IGameSession? session)
    {
        var context = Substitute.For<RaidoCallerContext>();
        var features = new FeatureCollection();
        features.Set<ICharacterFeature>(new CharacterFeature { Character = character });
        if (session != null)
        {
            features.Set<Hagalaz.Services.GameWorld.Features.ISessionFeature>(new SessionFeature { Session = session });
        }

        context.Features.Returns(features);
        return context;
    }

    private static void SetContext(RaidoHub hub, RaidoCallerContext context) =>
        typeof(RaidoHub)
            .GetProperty(nameof(RaidoHub.Context), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(hub, context);
}
