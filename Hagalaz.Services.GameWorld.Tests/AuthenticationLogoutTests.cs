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
using Hagalaz.Services.GameWorld.Services.Model;
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
    public async Task SignOutAsync_WhenPersistenceFails_KeepsSessionForRetry()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        var characterService = Substitute.For<ICharacterService>();
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        var persistenceFailure = new InvalidOperationException("Persistence is unavailable.");
        persistenceService.PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CharacterPersistenceReceipt?>(persistenceFailure));
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));
        var characterLogoutService = Substitute.For<ICharacterLogoutService>();
        var contextAccessor = CreateContextAccessor(character, session);
        var service = CreateAuthenticationService(
            characterService,
            persistenceService,
            gameSessionService,
            contextAccessor,
            characterLogoutService: characterLogoutService);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.SignOutAsync());

        Assert.AreSame(persistenceFailure, exception);
        await gameSessionService.DidNotReceive().RemoveSession(Arg.Any<IGameSession>());
        await characterLogoutService.Received(1).DetachAsync(character, Arg.Any<CancellationToken>());
        await characterService.DidNotReceive().RemoveAsync(character);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignOutAsync_WaitsForExactPersistenceAcknowledgementBeforeReleasingSession()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        var characterService = Substitute.For<ICharacterService>();
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        var receipt = CreateReceipt(character);
        var persistenceStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var acknowledge = new TaskCompletionSource<CharacterPersistenceOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        persistenceService.PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                persistenceStarted.TrySetResult(true);
                return Task.FromResult<CharacterPersistenceReceipt?>(receipt);
            });
        persistenceService.WaitForAcknowledgementAsync(receipt, Arg.Any<CancellationToken>())
            .Returns(acknowledge.Task);
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

        var signOutTask = service.SignOutAsync();
        await persistenceStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await Task.Yield();

        await persistenceService.Received(1).PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>());
        await gameSessionService.DidNotReceive().RemoveSession(Arg.Any<IGameSession>());
        Assert.IsFalse(signOutTask.IsCompleted);

        acknowledge.TrySetResult(CharacterPersistenceOutcome.Committed);
        await signOutTask;

        characterLogoutService.Received(1).SetPendingLogoutPersistence(character, receipt);
        await gameSessionService.Received(1).RemoveSession(session);
        await characterLogoutService.Received(1).DetachAsync(character, Arg.Any<CancellationToken>());
        mediator.DidNotReceive().Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    public async Task SignOutAsync_ConcurrentDuplicateFailsUntilTheOwnedLogoutCompletes()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        character.Session.Returns(session);
        var receipt = CreateReceipt(character);
        var persistenceStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releasePersistence = new TaskCompletionSource<CharacterPersistenceReceipt?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var characterService = Substitute.For<ICharacterService>();
#pragma warning disable CA2012 // NSubstitute consumes the configured ValueTask exactly once.
        characterService.RemoveAsync(character).Returns(ValueTask.FromResult(true));
#pragma warning restore CA2012
        var logoutService = new BlockingLogoutService();

        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        persistenceService.PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                persistenceStarted.TrySetResult(true);
                return releasePersistence.Task;
            });
        persistenceService.WaitForAcknowledgementAsync(receipt, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CharacterPersistenceOutcome.Committed));
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));
        var contextAccessor = CreateContextAccessor(character, session);
        var firstService = CreateAuthenticationService(
            characterService,
            persistenceService,
            gameSessionService,
            contextAccessor,
            null,
            null,
            logoutService,
            false);
        var secondService = CreateAuthenticationService(
            characterService,
            persistenceService,
            gameSessionService,
            contextAccessor,
            null,
            null,
            logoutService,
            false);

        var firstSignOut = firstService.SignOutAsync();
        await persistenceStarted.Task;

        var secondException = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => secondService.SignOutAsync());

        StringAssert.Contains(secondException.Message, "different logout operation");
        Assert.IsFalse(firstSignOut.IsCompleted);
        await persistenceService.Received(1).PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>());

        releasePersistence.TrySetResult(receipt);
        await firstSignOut;
        await gameSessionService.Received(1).RemoveSession(session);
        Assert.AreEqual(1, logoutService.DetachCalls);
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
        var receipt = CreateReceipt(character);
        persistenceService.PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                order.Add("persist");
                return Task.FromResult<CharacterPersistenceReceipt?>(receipt);
            });
        persistenceService.WaitForAcknowledgementAsync(receipt, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                order.Add("acknowledge");
                return Task.FromResult(CharacterPersistenceOutcome.Committed);
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

        CollectionAssert.AreEqual(new[] { "persist", "acknowledge", "release" }, order);
    }

    [TestMethod]
    public async Task SignOutAsync_WhenLogoutAlreadyHasReceipt_ReusesReceiptWithoutForcedRepersist()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        var receipt = CreateReceipt(character);
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        persistenceService.WaitForAcknowledgementAsync(receipt, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CharacterPersistenceOutcome.Duplicate));
        var logoutService = Substitute.For<ICharacterLogoutService>();
        logoutService.TryBeginLogout(
                character,
                out Arg.Any<bool>(),
                out Arg.Any<CharacterPersistenceReceipt?>())
            .Returns(callInfo =>
            {
                callInfo[1] = false;
                callInfo[2] = receipt;
                return true;
            });
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));
        var service = CreateAuthenticationService(
            Substitute.For<ICharacterService>(),
            persistenceService,
            gameSessionService,
            CreateContextAccessor(character, session),
            characterLogoutService: logoutService);

        logoutService.TryBeginLogout(
                character,
                out Arg.Any<bool>(),
                out Arg.Any<CharacterPersistenceReceipt?>())
            .Returns(callInfo =>
            {
                callInfo[1] = false;
                callInfo[2] = receipt;
                return true;
            });

        await service.SignOutAsync();

        await persistenceService.DidNotReceive().PersistAsync(
            42,
            Arg.Any<CharacterModel>(),
            true,
            Arg.Any<CancellationToken>());
        await persistenceService.Received(1).WaitForAcknowledgementAsync(receipt, Arg.Any<CancellationToken>());
        await gameSessionService.Received(1).RemoveSession(session);
    }

    [TestMethod]
    public async Task SignOutAsync_WhenSessionReleaseFails_KeepsPendingLogoutForRetry()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        ConfigureSuccessfulPersistence(persistenceService, character);
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
        await characterLogoutService.Received(1).DetachAsync(character, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SignOutAsync_WhenSessionCleanupIsQueued_DetachesCharacterAfterLocalRemoval()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        ConfigureSuccessfulPersistence(persistenceService, character);
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
        ConfigureSuccessfulPersistence(persistenceService, character);
        var revokeFailure = new InvalidOperationException("Token service is unavailable.");
        var revokeTokenRequestClient = Substitute.For<IRequestClient<RevokeTokenRequestMessage>>();
        revokeTokenRequestClient
            .GetResponse<RevokeTokenResponseMessage>(
                Arg.Any<RevokeTokenRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(Task.FromException<Response<RevokeTokenResponseMessage>>(revokeFailure));
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));
        var characterLogoutService = Substitute.For<ICharacterLogoutService>();
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
            characterLogoutService: characterLogoutService);

        await service.SignOutAsync();

        await persistenceService.Received(1).PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>());
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
        ConfigureSuccessfulPersistence(persistenceService, character);
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
        var authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService.SignOutAsync().Returns(Task.CompletedTask);
        var hub = new ConnectionHub(authenticationService, NullLogger<ConnectionHub>.Instance);
        SetContext(hub, CreateContext(character, session: null));

        await hub.OnDisconnectedAsync(null);

        character.DidNotReceive().Destroy();
    }

    private static CharacterPersistenceReceipt CreateReceipt(ICharacter character) =>
        new(character.MasterId, Guid.NewGuid(), 7L);

    private static void ConfigureSuccessfulPersistence(
        ICharacterPersistenceService persistenceService,
        ICharacter character)
    {
        var receipt = CreateReceipt(character);
        persistenceService.PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CharacterPersistenceReceipt?>(receipt));
        persistenceService.WaitForAcknowledgementAsync(receipt, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CharacterPersistenceOutcome.Committed));
    }

    private static AuthenticationService CreateAuthenticationService(
        ICharacterService characterService,
        ICharacterPersistenceService persistenceService,
        IGameSessionService gameSessionService,
        IRaidoCallerContextAccessor contextAccessor,
        IRequestClient<RevokeTokenRequestMessage>? revokeTokenRequestClient = null,
        IGameMediator? mediator = null,
        ICharacterLogoutService? characterLogoutService = null,
        bool configureLogoutService = true)
    {
        var logoutService = characterLogoutService ?? Substitute.For<ICharacterLogoutService>();
        if (configureLogoutService && contextAccessor.Context.Features.Get<ICharacterFeature>()?.Character is { } character)
        {
            logoutService.TryBeginLogout(
                    character,
                    out Arg.Any<bool>(),
                    out Arg.Any<CharacterPersistenceReceipt?>())
                .Returns(callInfo =>
                {
                    callInfo[1] = true;
                    callInfo[2] = null;
                    return true;
                });
            logoutService.SetPendingLogoutPersistence(character, Arg.Any<CharacterPersistenceReceipt>()).Returns(true);
            logoutService.DetachAsync(character, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new CharacterModel()));
        }

        return new(
            NullLogger<AuthenticationService>.Instance,
            characterService,
            persistenceService,
            logoutService,
            gameSessionService,
            new WorldSessionAdmissionService(
                NullLogger<WorldSessionAdmissionService>.Instance,
                Substitute.For<AutoMapper.IMapper>(),
                characterService,
                Substitute.For<ICharacterFactory>(),
                Substitute.For<ICharacterHydrationService>(),
                persistenceService,
                gameSessionService,
                Substitute.For<IRequestClient<HydrateCharacter>>()),
            Substitute.For<IRequestClient<SignInUserRequestMessage>>(),
            Substitute.For<IRequestClient<ValidateExistingAuthenticationRequestMessage>>(),
            Substitute.For<IRequestClient<GetUserInfoRequestMessage>>(),
            revokeTokenRequestClient ?? Substitute.For<IRequestClient<RevokeTokenRequestMessage>>(),
            Substitute.For<IClaimsPrincipalFactory>(),
            contextAccessor,
            mediator ?? Substitute.For<IGameMediator>(),
            new ResiliencePipelineBuilder().Build(),
            new ResiliencePipelineBuilder().Build());
    }

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

    private sealed class BlockingLogoutService : ICharacterLogoutService
    {
        private bool _claimed;

        public int DetachCalls { get; private set; }

        public bool TryBeginLogout(ICharacter character, out bool created, out CharacterPersistenceReceipt? persistenceReceipt)
        {
            created = !_claimed;
            persistenceReceipt = null;
            _claimed = true;
            return created;
        }

        public bool SetPendingLogoutPersistence(ICharacter character, CharacterPersistenceReceipt receipt) => true;

        public bool TryGetPendingPersistence(ICharacter character, out CharacterPersistenceReceipt? persistenceReceipt)
        {
            persistenceReceipt = null;
            return false;
        }

        public void CancelPendingLogout(ICharacter character) { }
        public bool IsPendingLogout(ICharacter character) => _claimed;
        public bool IsPendingLogout(uint masterId) => _claimed;

        public Task<CharacterModel> DetachAsync(ICharacter character, CancellationToken cancellationToken = default)
        {
            DetachCalls++;
            return Task.FromResult(new CharacterModel());
        }

        public void CompleteLogout(uint masterId) { }
    }

    private static void SetContext(RaidoHub hub, RaidoCallerContext context) =>
        typeof(RaidoHub)
            .GetProperty(nameof(RaidoHub.Context), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(hub, context);
}
