using System.Net;
using System.Reflection;
using System.Security.Claims;
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
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
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
        gameSessionService.AddSession(42, "connection").Returns(Task.FromResult(session));
        gameSessionService.RemoveSession("connection").Returns(Task.FromResult(true));

        var characterHydrationService = Substitute.For<ICharacterHydrationService>();
        characterHydrationService.HydrateAsync(Arg.Any<ICharacter>(), Arg.Any<CharacterModel>())
            .Returns(Task.FromResult(false));

        var service = CreateAuthenticationService(
            gameSessionService,
            characterHydrationService: characterHydrationService);

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        await gameSessionService.Received(1).RemoveSession("connection");
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenCharacterRegistrationFails_RemovesSession()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        gameSessionService.AddSession(42, "connection").Returns(Task.FromResult(session));
        gameSessionService.RemoveSession("connection").Returns(Task.FromResult(true));

        var characterService = new TestCharacterService(addResult: false);

        var service = CreateAuthenticationService(
            gameSessionService,
            characterService: characterService);

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        await gameSessionService.Received(1).RemoveSession("connection");
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenCharacterRegistrationSucceeds_KeepsSession()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        gameSessionService.AddSession(42, "connection").Returns(Task.FromResult(session));
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
        await gameSessionService.DidNotReceive().RemoveSession(Arg.Any<string>());
    }

    private static AuthenticationService CreateAuthenticationService(
        IGameSessionService gameSessionService,
        ICharacterService? characterService = null,
        ICharacterHydrationService? characterHydrationService = null)
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
        var getCharacterRequestClient = Substitute.For<IRequestClient<HydrateCharacter>>();
        getCharacterRequestClient
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
            getCharacterRequestClient,
            claimsPrincipalFactory,
            CreateContextAccessor(),
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

    private static IRaidoCallerContextAccessor CreateContextAccessor()
    {
        var context = Substitute.For<RaidoCallerContext>();
        context.ConnectionId.Returns("connection");
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
