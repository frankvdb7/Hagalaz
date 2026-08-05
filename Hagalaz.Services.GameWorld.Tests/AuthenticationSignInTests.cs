using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using AutoMapper;
using Hagalaz.Authorization.Messages;
using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using Hagalaz.Services.GameWorld.Store;
using MassTransit;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Polly;
using Raido.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class AuthenticationSignInTests
{
    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenCharacterHydrationFails_RemovesSession()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        gameSessionService.TryAddWorldSession(42, "connection").Returns(Task.FromResult<(IGameSession? Session, bool Created)>((session, Created: true)));
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));

        var characterHydrationService = Substitute.For<ICharacterHydrationService>();
        characterHydrationService.HydrateAsync(Arg.Any<ICharacter>(), Arg.Any<CharacterModel>())
            .Returns(Task.FromResult(false));

        var service = CreateAuthenticationService(
            gameSessionService,
            characterHydrationService: characterHydrationService);

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        await gameSessionService.Received(1).RemoveSession(session);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenCharacterRegistrationFails_RemovesSession()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        gameSessionService.TryAddWorldSession(42, "connection").Returns(Task.FromResult<(IGameSession? Session, bool Created)>((session, Created: true)));
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));

        var characterService = new TestCharacterService(addResult: false);

        var service = CreateAuthenticationService(
            gameSessionService,
            characterService: characterService);

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        await gameSessionService.Received(1).RemoveSession(session);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenClaimReleaseReturnsFalse_RemovesLocalSessionForLaterLogin()
    {
        var claims = new ReleaseFalseClaimStore();
        var factory = Substitute.For<IGameSessionFactory>();
        var initialSession = CreateSession("connection-1", "claim-1");
        var laterSession = CreateSession("connection-2", "claim-2");
        factory.CreateWorld(42, "connection-1").Returns(initialSession);
        factory.CreateWorld(42, "connection-2").Returns(laterSession);
        var store = new GameSessionStore();
        var gameSessionService = GameSessionTestDependencies.CreateService(
            store, store, factory, claims, Substitute.For<IGameSessionConnectionTerminator>());
        var hydrationService = Substitute.For<ICharacterHydrationService>();
        hydrationService.HydrateAsync(Arg.Any<ICharacter>(), Arg.Any<CharacterModel>())
            .Returns(Task.FromResult(false));

        var failedSignIn = CreateAuthenticationService(
            gameSessionService,
            characterHydrationService: hydrationService,
            connectionId: "connection-1");

        var failedResult = await failedSignIn.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(failedResult.Succeeded);
        Assert.IsNull(await gameSessionService.FindByMasterId(42));

        var laterSignIn = CreateAuthenticationService(gameSessionService, connectionId: "connection-2");
        var laterResult = await laterSignIn.SignInWorldAsync(CreateSignInRequest());

        Assert.IsTrue(laterResult.Succeeded);
        Assert.AreEqual(2, claims.TryClaimCount);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenClaimReleaseThrows_RemovesLocalSession()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var session = CreateSession("connection", "claim");
        gameSessionService.TryAddWorldSession(42, "connection")
            .Returns(Task.FromResult<(IGameSession? Session, bool Created)>((session, Created: true)));
        var releaseFailure = new InvalidOperationException("Redis is unavailable.");
        gameSessionService.RemoveSession(session).Returns(Task.FromException<bool>(releaseFailure));
        gameSessionService.RemoveLocalSession(session).Returns(Task.FromResult(true));
        var hydrationService = Substitute.For<ICharacterHydrationService>();
        hydrationService.HydrateAsync(Arg.Any<ICharacter>(), Arg.Any<CharacterModel>())
            .Returns(Task.FromResult(false));

        var service = CreateAuthenticationService(
            gameSessionService,
            characterHydrationService: hydrationService);

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        await gameSessionService.Received(1).RemoveSession(session);
        await gameSessionService.Received(1).RemoveLocalSession(session);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_ThenSignInWorldAsync_ReplacesLobbySession()
    {
        var factory = Substitute.For<IGameSessionFactory>();
        var lobbySession = CreateLobbySession("lobby-connection");
        var worldSession = CreateSession("world-connection", "world-claim");
        factory.Create(42, "lobby-connection").Returns(lobbySession);
        factory.CreateWorld(42, "world-connection").Returns(worldSession);
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var store = new GameSessionStore();
        var gameSessionService = GameSessionTestDependencies.CreateService(
            store,
            store,
            factory,
            new TestGameSessionClaimStore(),
            terminator);

        var lobbySignIn = CreateAuthenticationService(gameSessionService, connectionId: "lobby-connection");
        var lobbyResult = await lobbySignIn.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsTrue(lobbyResult.Succeeded);
        Assert.AreSame(lobbySession, await gameSessionService.FindByMasterId(42));

        var worldSignIn = CreateAuthenticationService(gameSessionService, connectionId: "world-connection");
        var worldResult = await worldSignIn.SignInWorldAsync(CreateSignInRequest());

        Assert.IsTrue(worldResult.Succeeded);
        terminator.Received(1).Abort(lobbySession);
        Assert.AreSame(worldSession, await gameSessionService.FindByMasterId(42));
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenHydrationFails_PreservesLobbySession()
    {
        var factory = Substitute.For<IGameSessionFactory>();
        var lobbySession = CreateLobbySession("lobby-connection");
        var worldSession = CreateSession("world-connection", "world-claim");
        factory.Create(42, "lobby-connection").Returns(lobbySession);
        factory.CreateWorld(42, "world-connection").Returns(worldSession);
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var store = new GameSessionStore();
        var gameSessionService = GameSessionTestDependencies.CreateService(
            store,
            store,
            factory,
            new TestGameSessionClaimStore(),
            terminator);
        var lobbySignIn = CreateAuthenticationService(gameSessionService, connectionId: "lobby-connection");
        Assert.IsTrue((await lobbySignIn.SignInLobbyAsync(CreateSignInRequest())).Succeeded);

        var hydrationService = Substitute.For<ICharacterHydrationService>();
        hydrationService.HydrateAsync(Arg.Any<ICharacter>(), Arg.Any<CharacterModel>()).Returns(Task.FromResult(false));
        var worldSignIn = CreateAuthenticationService(
            gameSessionService,
            characterHydrationService: hydrationService,
            connectionId: "world-connection");

        var result = await worldSignIn.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        Assert.AreSame(lobbySession, await gameSessionService.FindByMasterId(42));
        terminator.DidNotReceive().Abort(Arg.Any<IGameSession>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenCharacterRegistrationFails_PreservesLobbySession()
    {
        var factory = Substitute.For<IGameSessionFactory>();
        var lobbySession = CreateLobbySession("lobby-connection");
        var worldSession = CreateSession("world-connection", "world-claim");
        factory.Create(42, "lobby-connection").Returns(lobbySession);
        factory.CreateWorld(42, "world-connection").Returns(worldSession);
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var store = new GameSessionStore();
        var gameSessionService = GameSessionTestDependencies.CreateService(
            store,
            store,
            factory,
            new TestGameSessionClaimStore(),
            terminator);
        var lobbySignIn = CreateAuthenticationService(gameSessionService, connectionId: "lobby-connection");
        Assert.IsTrue((await lobbySignIn.SignInLobbyAsync(CreateSignInRequest())).Succeeded);

        var worldSignIn = CreateAuthenticationService(
            gameSessionService,
            characterService: new TestCharacterService(addResult: false),
            connectionId: "world-connection");

        var result = await worldSignIn.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        Assert.AreSame(lobbySession, await gameSessionService.FindByMasterId(42));
        terminator.DidNotReceive().Abort(Arg.Any<IGameSession>());
    }

    [TestMethod]
    public async Task SignInLobbyAsync_WhenActiveLobbySessionExists_ReturnsAlreadyLoggedOn()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var activeSession = CreateLobbySession("active-lobby");
        gameSessionService.AddSession(42, "new-lobby")
            .Returns(Task.FromResult<(IGameSession Session, bool Created)>((activeSession, false)));

        var service = CreateAuthenticationService(gameSessionService, connectionId: "new-lobby");

        var result = await service.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsTrue(result.IsAlreadyLoggedOn);
        await gameSessionService.Received(1).AddSession(42, "new-lobby");
    }

    [TestMethod]
    public async Task SignInLobbyAsync_WhenActiveWorldSessionExists_ReturnsAlreadyLoggedOn()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var activeSession = CreateSession("active-world", "world-claim");
        gameSessionService.AddSession(42, "new-lobby")
            .Returns(Task.FromResult<(IGameSession Session, bool Created)>((activeSession, false)));

        var service = CreateAuthenticationService(gameSessionService, connectionId: "new-lobby");

        var result = await service.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsTrue(result.IsAlreadyLoggedOn);
        await gameSessionService.Received(1).AddSession(42, "new-lobby");
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_ThenRemoveSession_AllowsSignInLobbyAsync()
    {
        var factory = Substitute.For<IGameSessionFactory>();
        var worldSession = CreateSession("world-connection", "world-claim");
        var lobbySession = CreateLobbySession("lobby-connection");
        factory.CreateWorld(42, "world-connection").Returns(worldSession);
        factory.Create(42, "lobby-connection").Returns(lobbySession);
        var store = new GameSessionStore();
        var gameSessionService = GameSessionTestDependencies.CreateService(
            store,
            store,
            factory,
            new TestGameSessionClaimStore(),
            Substitute.For<IGameSessionConnectionTerminator>());

        var worldSignIn = CreateAuthenticationService(gameSessionService, connectionId: "world-connection");
        var worldResult = await worldSignIn.SignInWorldAsync(CreateSignInRequest());

        Assert.IsTrue(worldResult.Succeeded);
        Assert.IsTrue(await gameSessionService.RemoveSession(worldSession));

        var lobbySignIn = CreateAuthenticationService(gameSessionService, connectionId: "lobby-connection");
        var lobbyResult = await lobbySignIn.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsTrue(lobbyResult.Succeeded);
        Assert.AreSame(lobbySession, await gameSessionService.FindByMasterId(42));
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenCharacterRegistrationFailsForExistingSession_PreservesSession()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var existingSession = Substitute.For<IGameSession>();
        existingSession.ConnectionId.Returns("connection");
        gameSessionService.TryAddWorldSession(42, "connection")
            .Returns(Task.FromResult<(IGameSession? Session, bool Created)>((existingSession, Created: false)));

        var service = CreateAuthenticationService(
            gameSessionService,
            characterService: new TestCharacterService(addResult: false));

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsTrue(result.IsAlreadyLoggedOn);
        await gameSessionService.DidNotReceive().RemoveSession(Arg.Any<IGameSession>());
    }

    [TestMethod]
    public async Task SignInWorldAsync_WhenWorldClaimIsOwned_DoesNotHydrate()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.TryAddWorldSession(42, "connection")
            .Returns(Task.FromResult<(IGameSession? Session, bool Created)>((null, Created: false)));
        var hydrateClient = Substitute.For<IRequestClient<HydrateCharacter>>();

        var service = CreateAuthenticationService(gameSessionService, getCharacterRequestClient: hydrateClient);

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsTrue(result.IsAlreadyLoggedOn);
        await hydrateClient.DidNotReceive().GetResponse<CharacterHydrated, CharacterNotFound>(
            Arg.Any<HydrateCharacter>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_ConcurrentSameWorldAttemptsHydrateOnlyTheWinner()
    {
        var claims = new BarrierGameSessionClaimStore();
        var factory = Substitute.For<IGameSessionFactory>();
        var lobbySession = CreateLobbySession("lobby-connection");
        var firstSession = CreateSession("connection-1", "claim-1");
        var secondSession = CreateSession("connection-2", "claim-2");
        factory.Create(42, "lobby-connection").Returns(lobbySession);
        factory.CreateWorld(42, "connection-1").Returns(firstSession);
        factory.CreateWorld(42, "connection-2").Returns(secondSession);
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var store = new GameSessionStore();
        var gameSessionService = GameSessionTestDependencies.CreateService(store, store, factory, claims, terminator);
        await gameSessionService.AddSession(42, "lobby-connection");
        var firstHydrator = new TrackingHydrationService();
        var secondHydrator = new TrackingHydrationService();
        var firstCharacterService = new TestCharacterService(addResult: true);
        var secondCharacterService = new TestCharacterService(addResult: true);
        var firstAuthentication = CreateAuthenticationService(
            gameSessionService,
            firstCharacterService,
            firstHydrator,
            connectionId: "connection-1");
        var secondAuthentication = CreateAuthenticationService(
            gameSessionService,
            secondCharacterService,
            secondHydrator,
            connectionId: "connection-2");

        var signInsTask = Task.WhenAll(
            firstAuthentication.SignInWorldAsync(CreateSignInRequest()).AsTask(),
            secondAuthentication.SignInWorldAsync(CreateSignInRequest()).AsTask());
        await claims.WaitForBothClaimAttemptsAsync();
        claims.ReleaseClaimAttempts();
        var results = await signInsTask;

        Assert.AreEqual(1, results.Count(result => result.Succeeded));
        Assert.AreEqual(1, results.Count(result => result.IsAlreadyLoggedOn));
        Assert.AreEqual(1, firstHydrator.Calls + secondHydrator.Calls);
        Assert.AreEqual(1, firstCharacterService.AddCallCount + secondCharacterService.AddCallCount);
        terminator.Received(1).Abort(lobbySession);
    }

    [TestMethod]
    public async Task GameSessionService_AddSession_ReportsOwnershipOnlyForNewSession()
    {
        var session = Substitute.For<IGameSession>();
        var gameSessionFactory = Substitute.For<IGameSessionFactory>();
        gameSessionFactory.Create(42, "connection").Returns(session);
        var store = new GameSessionStore();
        var service = GameSessionTestDependencies.CreateService(
            store,
            store,
            gameSessionFactory,
            new TestGameSessionClaimStore(),
            Substitute.For<IGameSessionConnectionTerminator>());

        var firstRegistration = await service.AddSession(42, "connection");
        var secondRegistration = await service.AddSession(42, "connection");

        Assert.IsTrue(firstRegistration.Created);
        Assert.IsFalse(secondRegistration.Created);
        Assert.AreSame(session, firstRegistration.Session);
        Assert.AreSame(session, secondRegistration.Session);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenCharacterRegistrationSucceeds_KeepsSession()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        gameSessionService.TryAddWorldSession(42, "connection").Returns(Task.FromResult<(IGameSession? Session, bool Created)>((session, Created: true)));
        gameSessionService.CommitWorldSession(session).Returns(Task.FromResult(true));
        var characterService = new TestCharacterService(addResult: true);
        var characterHydrationService = Substitute.For<ICharacterHydrationService>();
        characterHydrationService.HydrateAsync(Arg.Any<ICharacter>(), Arg.Any<CharacterModel>()).Returns(Task.FromResult(true));

        var service = CreateAuthenticationService(
            gameSessionService,
            characterService,
            characterHydrationService);

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsTrue(result.Succeeded);
        await characterHydrationService.Received(1).HydrateAsync(Arg.Any<ICharacter>(), Arg.Any<CharacterModel>());
        Assert.AreEqual(1, characterService.AddCallCount);
        await gameSessionService.DidNotReceive().RemoveSession(Arg.Any<IGameSession>());
    }

    private static AuthenticationService CreateAuthenticationService(
        IGameSessionService gameSessionService,
        ICharacterService? characterService = null,
        ICharacterHydrationService? characterHydrationService = null,
        IRequestClient<HydrateCharacter>? getCharacterRequestClient = null,
        string connectionId = "connection")
    {
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CharacterModel>(Arg.Any<CharacterHydrated>()).Returns(new CharacterModel());
        mapper.Map<HydratedClaims>(Arg.Any<AuthenticationProperties>()).Returns(new HydratedClaims());

        var signInResponse = CreateResponse(new SignInUserResponseMessage
        {
            Succeeded = true,
            IdToken = "id-token",
            AccessToken = "access-token",
            Scope = "openid",
            ExpireDate = DateTimeOffset.UtcNow.AddMinutes(5),
            TokenType = "Bearer"
        });
        var signInUserRequestClient = Substitute.For<IRequestClient<SignInUserRequestMessage>>();
        signInUserRequestClient
            .GetResponse<SignInUserResponseMessage>(
                Arg.Any<SignInUserRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .ReturnsForAnyArgs(Task.FromResult(signInResponse));

        var userInfoResponse = CreateResponse(new GetUserInfoResponseMessage
        {
            Succeeded = true,
            Claims = new Dictionary<string, object> { [Claims.Subject] = "42" }
        });
        var getUserInfoRequestClient = Substitute.For<IRequestClient<GetUserInfoRequestMessage>>();
        getUserInfoRequestClient
            .GetResponse<GetUserInfoResponseMessage>(
                Arg.Any<GetUserInfoRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .ReturnsForAnyArgs(Task.FromResult(userInfoResponse));

        var characterResponse = CreateResponse<CharacterHydrated, CharacterNotFound>(CreateCharacterHydrated());
        var hydrateRequestClient = Substitute.For<IRequestClient<HydrateCharacter>>();
        hydrateRequestClient
            .GetResponse<CharacterHydrated, CharacterNotFound>(
                Arg.Any<HydrateCharacter>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .ReturnsForAnyArgs(Task.FromResult(characterResponse));

        var claimsPrincipalFactory = Substitute.For<IClaimsPrincipalFactory>();
        claimsPrincipalFactory.Create(Arg.Any<IDictionary<string, object>>())
            .Returns(new ClaimsPrincipal(new ClaimsIdentity("test")));

        var characterFactory = Substitute.For<ICharacterFactory>();
        characterFactory.Create(Arg.Any<IGameSession>(), Arg.Any<IGameClient>()).Returns(Substitute.For<ICharacter>());

        var characterServiceSubstitute = characterService ?? new TestCharacterService(addResult: true);

        var characterHydrationServiceSubstitute = characterHydrationService ?? Substitute.For<ICharacterHydrationService>();
        if (characterHydrationService == null)
        {
            characterHydrationServiceSubstitute.HydrateAsync(Arg.Any<ICharacter>(), Arg.Any<CharacterModel>())
                .Returns(Task.FromResult(true));
        }

        return new AuthenticationService(
            NullLogger<AuthenticationService>.Instance,
            mapper,
            characterServiceSubstitute,
            characterFactory,
            characterHydrationServiceSubstitute,
            Substitute.For<ICharacterPersistenceService>(),
            Substitute.For<ICharacterLogoutService>(),
            gameSessionService,
            signInUserRequestClient,
            getUserInfoRequestClient,
            Substitute.For<IRequestClient<RevokeTokenRequestMessage>>(),
            getCharacterRequestClient ?? hydrateRequestClient,
            claimsPrincipalFactory,
            CreateContextAccessor(connectionId),
            Substitute.For<IGameMediator>(),
            new ResiliencePipelineBuilder().Build(),
            new ResiliencePipelineBuilder().Build());
    }

    private static SignInRequest CreateSignInRequest() => new()
    {
        Login = "login",
        Password = "password",
        GameClient = Substitute.For<IGameClient>()
    };

    private static IRaidoCallerContextAccessor CreateContextAccessor(string connectionId = "connection")
    {
        var context = Substitute.For<RaidoCallerContext>();
        context.ConnectionId.Returns(connectionId);
        context.RemoteIPEndPoint.Returns(new IPEndPoint(IPAddress.Loopback, 43594));
        context.Features.Returns(new FeatureCollection());

        var accessor = Substitute.For<IRaidoCallerContextAccessor>();
        accessor.Context.Returns(context);
        return accessor;
    }

    private static CharacterHydrated CreateCharacterHydrated() => new()
    {
        MasterId = 42,
        CorrelationId = Guid.NewGuid(),
        Appearance = null!,
        Details = null!,
        Statistics = null!,
        ItemCollection = null!,
        Familiar = null!,
        Music = null!,
        Farming = null!,
        Slayer = null!,
        Notes = null!,
        Profile = null!,
        ItemAppearanceCollection = null!,
        State = null!
    };

    private static IGameWorldSession CreateSession(string connectionId, string claimId)
    {
        var session = Substitute.For<IGameWorldSession>();
        session.MasterId.Returns(42u);
        session.ConnectionId.Returns(connectionId);
        session.SessionClaimId.Returns(claimId);
        return session;
    }

    private static IGameSession CreateLobbySession(string connectionId)
    {
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns(connectionId);
        session.MasterId.Returns(42u);
        return session;
    }

    private static Response<T> CreateResponse<T>(T message) where T : class
    {
        var response = Substitute.For<Response<T>>();
        response.Message.Returns(message);
        ((Response)response).Message.Returns(message);
        return response;
    }

    private sealed class TestCharacterService : ICharacterService
    {
        private readonly bool _addResult;

        public TestCharacterService(bool addResult) => _addResult = addResult;

        public int AddCallCount { get; private set; }

        public ValueTask<bool> AddAsync(ICharacter character)
        {
            AddCallCount++;
            return ValueTask.FromResult(_addResult);
        }

        public ValueTask<bool> RemoveAsync(ICharacter character) => ValueTask.FromResult(false);

        public ValueTask<int> CountAsync() => ValueTask.FromResult(0);

        public ValueTask<ICharacter?> FindByIndex(int index) => ValueTask.FromResult<ICharacter?>(null);

        public ValueTask<ICharacter?> FindByMasterId(uint masterId) => ValueTask.FromResult<ICharacter?>(null);

        public async IAsyncEnumerable<ICharacter> FindAll()
        {
            yield break;
        }
    }

    private sealed class TrackingHydrationService : ICharacterHydrationService
    {
        public int Calls { get; private set; }

        public Task<bool> HydrateAsync(ICharacter character, CharacterModel model)
        {
            Calls++;
            return Task.FromResult(true);
        }
    }

    private sealed class TestGameSessionClaimStore : IGameSessionClaimStore
    {
        public Task<bool> TryClaimAsync(uint masterId, string claimId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> ReleaseAsync(uint masterId, string claimId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> RenewAsync(uint masterId, string claimId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> ExecuteIfOwnerAsync(uint masterId, string claimId, Func<CancellationToken, Task<bool>> action, CancellationToken cancellationToken = default) => action(cancellationToken);
    }

    private sealed class ReleaseFalseClaimStore : IGameSessionClaimStore
    {
        private readonly object _sync = new();
        private readonly Dictionary<uint, string> _claims = new();

        public int TryClaimCount { get; private set; }

        public Task<bool> TryClaimAsync(uint masterId, string claimId, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                TryClaimCount++;
                return Task.FromResult(_claims.TryAdd(masterId, claimId));
            }
        }

        public Task<bool> ReleaseAsync(uint masterId, string claimId, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                _claims.Remove(masterId);
                return Task.FromResult(false);
            }
        }

        public Task<bool> RenewAsync(uint masterId, string claimId, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                return Task.FromResult(_claims.TryGetValue(masterId, out var current) && current == claimId);
            }
        }

        public async Task<bool> ExecuteIfOwnerAsync(uint masterId, string claimId, Func<CancellationToken, Task<bool>> action, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                if (!_claims.TryGetValue(masterId, out var current) || current != claimId)
                {
                    return false;
                }
            }

            return await action(cancellationToken);
        }
    }

    private sealed class BarrierGameSessionClaimStore : IGameSessionClaimStore
    {
        private readonly object _sync = new();
        private readonly Dictionary<uint, string> _claims = new();
        private readonly TaskCompletionSource<bool> _bothAttempts = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _releaseAttempts = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _attempts;

        public Task WaitForBothClaimAttemptsAsync() => _bothAttempts.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public void ReleaseClaimAttempts() => _releaseAttempts.TrySetResult(true);

        public async Task<bool> TryClaimAsync(uint masterId, string claimId, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _attempts) == 2)
            {
                _bothAttempts.TrySetResult(true);
            }

            await _releaseAttempts.Task.WaitAsync(TimeSpan.FromSeconds(5));
            lock (_sync)
            {
                if (_claims.ContainsKey(masterId))
                {
                    return false;
                }

                _claims.Add(masterId, claimId);
                return true;
            }
        }

        public Task<bool> ReleaseAsync(uint masterId, string claimId, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                return Task.FromResult(_claims.Remove(masterId));
            }
        }

        public Task<bool> RenewAsync(uint masterId, string claimId, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                return Task.FromResult(_claims.TryGetValue(masterId, out var current) && current == claimId);
            }
        }

        public async Task<bool> ExecuteIfOwnerAsync(uint masterId, string claimId, Func<CancellationToken, Task<bool>> action, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                if (!_claims.TryGetValue(masterId, out var current) || current != claimId)
                {
                    return false;
                }
            }

            return await action(cancellationToken);
        }
    }

    private static Response<T1, T2> CreateResponse<T1, T2>(T1 message)
        where T1 : class
        where T2 : class
    {
        var firstResponse = CreateResponse(message);
        var secondResponseTask = new TaskCompletionSource<Response<T2>>().Task;
        var constructor = typeof(Response<T1, T2>)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single();
        var response = (Response<T1, T2>)constructor.Invoke(
            [Task.FromResult(firstResponse), secondResponseTask]);
        object boxedResponse = response;
        typeof(Response<T1, T2>)
            .GetField("_response", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(boxedResponse, firstResponse);
        response = (Response<T1, T2>)boxedResponse;
        return response;
    }
}
