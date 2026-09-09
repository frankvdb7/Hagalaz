using System;
using System.Collections.Generic;
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
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using MassTransit;
using NSubstitute;
using Polly;
using Raido.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class AuthenticationLogoutTests
{
    [TestMethod]
    [Timeout(5000)]
    public async Task SignOutAsync_WhenPersistenceFails_KeepsSessionAndCharacterLiveForRetry()
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
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));
        var contextAccessor = CreateContextAccessor(character, session);
        var service = CreateAuthenticationService(
            characterService,
            persistenceService,
            gameSessionService,
            contextAccessor);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.SignOutAsync());

        Assert.AreSame(persistenceFailure, exception);
        persistenceService.Received(1).TrackPendingLogout(character);
        await gameSessionService.DidNotReceive().RemoveSession(Arg.Any<IGameSession>());
        await characterService.DidNotReceive().RemoveAsync(character);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignOutAsync_WhenPersistenceAwaitsAcknowledgement_CompletesLogoutAndDefersCleanup()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        var characterService = Substitute.For<ICharacterService>();
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        persistenceService.IsPersistenceAcknowledged(character).Returns(false);
        var mediator = Substitute.For<IGameMediator>();
        var characterLogoutService = Substitute.For<ICharacterLogoutService>();
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));
        var service = CreateAuthenticationService(
            characterService,
            persistenceService,
            gameSessionService,
            CreateContextAccessor(character, session),
            mediator: mediator,
            characterLogoutService: characterLogoutService);

        await service.SignOutAsync();

        persistenceService.Received(1).TrackPendingLogout(character);
        await persistenceService.Received(1).PersistAsync(character, true, Arg.Any<CancellationToken>());
        await gameSessionService.Received(1).RemoveSession(session);
        await characterLogoutService.Received(1).DetachAsync(character, Arg.Any<CancellationToken>());
        persistenceService.DidNotReceive().Forget(Arg.Any<uint>());
        mediator.DidNotReceive().Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    public async Task SignOutAsync_PersistsBeforeReleasingSession()
    {
        var order = new List<string>();
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        var characterService = Substitute.For<ICharacterService>();
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        persistenceService.PersistAsync(character, true, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                order.Add("persist");
                return Task.CompletedTask;
            });
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.RemoveSession(session)
            .Returns(_ =>
            {
                order.Add("release");
                return Task.FromResult(true);
            });
        var characterLogoutService = Substitute.For<ICharacterLogoutService>();
        var service = CreateAuthenticationService(
            characterService,
            persistenceService,
            gameSessionService,
            CreateContextAccessor(character, session),
            characterLogoutService: characterLogoutService);

        await service.SignOutAsync();

        CollectionAssert.AreEqual(new[] { "persist", "release" }, order);
    }

    [TestMethod]
    public async Task SignOutAsync_WhenSessionReleaseFails_KeepsCharacterForRetry()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        persistenceService.PersistAsync(character, true, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var releaseFailure = new InvalidOperationException("Redis is unavailable.");
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.RemoveSession(session).Returns(Task.FromException<bool>(releaseFailure));
        var characterLogoutService = Substitute.For<ICharacterLogoutService>();
        var service = CreateAuthenticationService(
            Substitute.For<ICharacterService>(),
            persistenceService,
            gameSessionService,
            CreateContextAccessor(character, session),
            characterLogoutService: characterLogoutService);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.SignOutAsync());

        Assert.AreSame(releaseFailure, exception);
        await characterLogoutService.DidNotReceive().DetachAsync(Arg.Any<ICharacter>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SignOutAsync_WhenSessionCleanupIsQueued_DetachesCharacterAfterLocalRemoval()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        persistenceService.PersistAsync(character, true, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));
        var characterLogoutService = Substitute.For<ICharacterLogoutService>();
        var service = CreateAuthenticationService(
            Substitute.For<ICharacterService>(),
            persistenceService,
            gameSessionService,
            CreateContextAccessor(character, session),
            characterLogoutService: characterLogoutService);

        await service.SignOutAsync();

        await characterLogoutService.Received(1).DetachAsync(character, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignOutAsync_WhenTokenRevocationFails_ReleasesSessionClaim()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        var characterService = Substitute.For<ICharacterService>();
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        var revokeFailure = new InvalidOperationException("Token service is unavailable.");
        var revokeTokenRequestClient = Substitute.For<IRequestClient<RevokeTokenRequestMessage>>();
        revokeTokenRequestClient
            .GetResponse<RevokeTokenResponseMessage>(
                Arg.Any<RevokeTokenRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(Task.FromException<Response<RevokeTokenResponseMessage>>(revokeFailure));
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));
        var contextAccessor = CreateContextAccessor(
            character,
            session,
            new Hagalaz.Services.GameWorld.Features.AuthenticationProperties
            {
                ClientId = "world-client",
                AuthorizationId = "authorization-id",
                Claims = new Dictionary<string, object> { [Claims.Subject] = "42" }
            });
        var service = CreateAuthenticationService(
            characterService,
            persistenceService,
            gameSessionService,
            contextAccessor,
            revokeTokenRequestClient,
            characterLogoutService: Substitute.For<ICharacterLogoutService>());

        await service.SignOutAsync();

        persistenceService.Received(1).TrackPendingLogout(character);
        await persistenceService.Received(1).PersistAsync(character, true, Arg.Any<CancellationToken>());
        await gameSessionService.Received(1).RemoveSession(session);
        await characterService.DidNotReceive().RemoveAsync(character);
        await revokeTokenRequestClient.Received(1).GetResponse<RevokeTokenResponseMessage>(
            Arg.Is<RevokeTokenRequestMessage>(message => message.AuthorizationId == "authorization-id"),
            Arg.Is<CancellationToken>(token => !token.IsCancellationRequested),
            Arg.Any<RequestTimeout>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignOutAsync_ReleasesSessionBeforeRevocationCompletes()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        var revokeStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completeRevocation = new TaskCompletionSource<Response<RevokeTokenResponseMessage>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var revokeResponse = Substitute.For<Response<RevokeTokenResponseMessage>>();
        revokeResponse.Message.Returns(new RevokeTokenResponseMessage { Succeeded = true });
        var revokeTokenRequestClient = Substitute.For<IRequestClient<RevokeTokenRequestMessage>>();
        revokeTokenRequestClient
            .GetResponse<RevokeTokenResponseMessage>(
                Arg.Any<RevokeTokenRequestMessage>(),
                Arg.Is<CancellationToken>(token => !token.IsCancellationRequested),
                Arg.Any<RequestTimeout>())
            .Returns(_ =>
            {
                revokeStarted.TrySetResult(true);
                return completeRevocation.Task;
            });
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));
        var service = CreateAuthenticationService(
            Substitute.For<ICharacterService>(),
            persistenceService,
            gameSessionService,
            CreateContextAccessor(
                character,
                session,
                new Hagalaz.Services.GameWorld.Features.AuthenticationProperties
                {
                    ClientId = "world-client",
                    AuthorizationId = "authorization-id",
                    Claims = new Dictionary<string, object> { [Claims.Subject] = "42" }
                }),
            revokeTokenRequestClient);

        var signOutTask = service.SignOutAsync();
        await revokeStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await gameSessionService.Received(1).RemoveSession(session);
        Assert.IsFalse(signOutTask.IsCompleted);

        completeRevocation.TrySetResult(revokeResponse);
        await signOutTask;
    }

    [TestMethod]
    public async Task SignOutAsync_WithOwnedLobbySession_PublishesLobbySignOut()
    {
        var session = Substitute.For<IGameSession>();
        session.MasterId.Returns(42u);
        session.ConnectionId.Returns("lobby-a");
        session.SessionGeneration.Returns(3L);
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));
        var mediator = Substitute.For<IGameMediator>();
        var service = CreateAuthenticationService(
            Substitute.For<ICharacterService>(),
            Substitute.For<ICharacterPersistenceService>(),
            gameSessionService,
            CreateContextAccessor(
                character: null,
                session: session,
                authenticationProperties: new Hagalaz.Services.GameWorld.Features.AuthenticationProperties
                {
                    ClientId = "lobby-client",
                    AuthorizationId = "authorization-id",
                    Claims = new Dictionary<string, object> { [Claims.Subject] = "42" }
                }),
            mediator: mediator,
            revokeTokenRequestClient: CreateSuccessfulRevokeClient());

        await service.SignOutAsync();

        mediator.Received(1).Publish(Arg.Is<LobbySignOutCommand>(message => message.MasterId == 42 && message.SessionGeneration == 3L && message.ConnectionId == "lobby-a"));
    }

    [TestMethod]
    public async Task SignOutAsync_WhenFailedLobbySignInRetainsAuthenticationWithoutSession_DoesNotPublishLobbySignOut()
    {
        var mediator = Substitute.For<IGameMediator>();
        var service = CreateAuthenticationService(
            Substitute.For<ICharacterService>(),
            Substitute.For<ICharacterPersistenceService>(),
            Substitute.For<IGameSessionService>(),
            CreateContextAccessor(
                character: null,
                session: null,
                authenticationProperties: new Hagalaz.Services.GameWorld.Features.AuthenticationProperties
                {
                    ClientId = "lobby-client",
                    AuthorizationId = "authorization-id",
                    Claims = new Dictionary<string, object> { [Claims.Subject] = "42" }
                }),
            mediator: mediator,
            revokeTokenRequestClient: CreateSuccessfulRevokeClient());

        await service.SignOutAsync();

        mediator.DidNotReceive().Publish(Arg.Any<LobbySignOutCommand>());
    }

    [TestMethod]
    public async Task SignOutAsync_WhenFailedWorldSignInRetainsAuthenticationWithoutSession_DoesNotPublishLobbySignOut()
    {
        var mediator = Substitute.For<IGameMediator>();
        var service = CreateAuthenticationService(
            Substitute.For<ICharacterService>(),
            Substitute.For<ICharacterPersistenceService>(),
            Substitute.For<IGameSessionService>(),
            CreateContextAccessor(
                character: null,
                session: null,
                authenticationProperties: new Hagalaz.Services.GameWorld.Features.AuthenticationProperties
                {
                    ClientId = "world-client",
                    AuthorizationId = "authorization-id",
                    Claims = new Dictionary<string, object> { [Claims.Subject] = "42" }
                }),
            mediator: mediator,
            revokeTokenRequestClient: CreateSuccessfulRevokeClient());

        await service.SignOutAsync();

        mediator.DidNotReceive().Publish(Arg.Any<LobbySignOutCommand>());
    }

    [TestMethod]
    public async Task SignOutAsync_WhenFailedWorldSignInHasAnotherLobbySession_DoesNotPublishLobbySignOut()
    {
        var existingLobbySession = Substitute.For<IGameSession>();
        existingLobbySession.MasterId.Returns(42u);
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.FindByMasterId(42u).Returns(existingLobbySession);
        var mediator = Substitute.For<IGameMediator>();
        var service = CreateAuthenticationService(
            Substitute.For<ICharacterService>(),
            Substitute.For<ICharacterPersistenceService>(),
            gameSessionService,
            CreateContextAccessor(
                character: null,
                session: null,
                authenticationProperties: new Hagalaz.Services.GameWorld.Features.AuthenticationProperties
                {
                    ClientId = "world-client",
                    AuthorizationId = "authorization-id",
                    Claims = new Dictionary<string, object> { [Claims.Subject] = "42" }
                }),
            mediator: mediator,
            revokeTokenRequestClient: CreateSuccessfulRevokeClient());

        await service.SignOutAsync();

        mediator.DidNotReceive().Publish(Arg.Any<LobbySignOutCommand>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task OnDisconnectedAsync_WhenSignOutFails_DoesNotDestroyRegisteredCharacter()
    {
        var character = Substitute.For<ICharacter>();
        character.IsDestroyed.Returns(false);
        var authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService.SignOutAsync().Returns(Task.FromException(new InvalidOperationException("Sign out failed.")));
        var hub = new ConnectionHub(authenticationService, NullLogger<ConnectionHub>.Instance);
        SetContext(hub, CreateContext(character, session: null));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => hub.OnDisconnectedAsync(null));

        character.DidNotReceive().Destroy();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task OnDisconnectedAsync_WhenSignOutSucceeds_LeavesCleanupToCoordinator()
    {
        var character = Substitute.For<ICharacter>();
        character.IsDestroyed.Returns(false);
        var authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService.SignOutAsync().Returns(Task.CompletedTask);
        var hub = new ConnectionHub(authenticationService, NullLogger<ConnectionHub>.Instance);
        SetContext(hub, CreateContext(character, session: null));

        await hub.OnDisconnectedAsync(null);

        character.DidNotReceive().Destroy();
    }

    private static AuthenticationService CreateAuthenticationService(
        ICharacterService characterService,
        ICharacterPersistenceService persistenceService,
        IGameSessionService gameSessionService,
        IRaidoCallerContextAccessor contextAccessor,
        IRequestClient<RevokeTokenRequestMessage>? revokeTokenRequestClient = null,
        IGameMediator? mediator = null,
        ICharacterLogoutService? characterLogoutService = null) =>
        new(
            NullLogger<AuthenticationService>.Instance,
            Substitute.For<AutoMapper.IMapper>(),
            characterService,
            Substitute.For<ICharacterFactory>(),
            Substitute.For<ICharacterHydrationService>(),
            persistenceService,
            characterLogoutService ?? Substitute.For<ICharacterLogoutService>(),
            gameSessionService,
            Substitute.For<IRequestClient<SignInUserRequestMessage>>(),
            Substitute.For<IRequestClient<ValidateExistingAuthenticationRequestMessage>>(),
            Substitute.For<IRequestClient<GetUserInfoRequestMessage>>(),
            revokeTokenRequestClient ?? Substitute.For<IRequestClient<RevokeTokenRequestMessage>>(),
            Substitute.For<IRequestClient<HydrateCharacter>>(),
            Substitute.For<IClaimsPrincipalFactory>(),
            contextAccessor,
            mediator ?? Substitute.For<IGameMediator>(),
            new ResiliencePipelineBuilder().Build(),
            new ResiliencePipelineBuilder().Build());

    private static IRaidoCallerContextAccessor CreateContextAccessor(
        ICharacter? character,
        IGameSession? session,
        Hagalaz.Services.GameWorld.Features.AuthenticationProperties? authenticationProperties = null)
    {
        var accessor = Substitute.For<IRaidoCallerContextAccessor>();
        var context = CreateContext(character, session, authenticationProperties);
        accessor.Context.Returns(context);
        return accessor;
    }

    private static RaidoCallerContext CreateContext(
        ICharacter? character,
        IGameSession? session,
        Hagalaz.Services.GameWorld.Features.AuthenticationProperties? authenticationProperties = null)
    {
        var context = Substitute.For<RaidoCallerContext>();
        var features = new FeatureCollection();
        if (character is not null)
        {
            features.Set<ICharacterFeature>(new CharacterFeature { Character = character });
        }
        if (authenticationProperties != null)
        {
            features.Set<Hagalaz.Services.GameWorld.Features.IAuthenticationFeature>(new Hagalaz.Services.GameWorld.Features.AuthenticationFeature
            {
                AuthenticationProperties = authenticationProperties
            });
        }

        if (session != null)
        {
            features.Set<Hagalaz.Services.GameWorld.Features.ISessionFeature>(new SessionFeature { Session = session });
        }

        context.Features.Returns(features);
        return context;
    }

    private static IRequestClient<RevokeTokenRequestMessage> CreateSuccessfulRevokeClient()
    {
        var response = Substitute.For<Response<RevokeTokenResponseMessage>>();
        var message = new RevokeTokenResponseMessage { Succeeded = true };
        response.Message.Returns(message);
        ((Response)response).Message.Returns(message);
        var client = Substitute.For<IRequestClient<RevokeTokenRequestMessage>>();
        client
            .GetResponse<RevokeTokenResponseMessage>(
                Arg.Any<RevokeTokenRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(Task.FromResult(response));
        return client;
    }

    private static void SetContext(RaidoHub hub, RaidoCallerContext context) =>
        typeof(RaidoHub)
            .GetProperty(nameof(RaidoHub.Context), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(hub, context);
}
