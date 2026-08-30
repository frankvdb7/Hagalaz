using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using AutoMapper;
using Hagalaz.Authorization.Messages;
using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using Hagalaz.Services.GameWorld.Store;
using MassTransit;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Raido.Server;
using Testcontainers.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Locking.Distributed;

namespace Hagalaz.Services.GameWorld.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class FusionCacheGameSessionClaimIntegrationTests
{
    private static RedisContainer? _redis;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        _redis = new RedisBuilder("redis:7.4").Build();
        await _redis.StartAsync();
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        if (_redis != null)
        {
            await _redis.DisposeAsync();
        }
    }

    [TestMethod]
    [Timeout(120000)]
    public void Startup_ResolvesInterfaceAndConcreteStoreAsTheSameSingleton()
    {
        using var provider = CreateProvider();

        var concreteStore = provider.GetRequiredService<GameSessionStore>();
        var interfaceStore = provider.GetRequiredService<IGameSessionStore>();

        Assert.AreSame(concreteStore, interfaceStore);
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task TryClaimAsync_IsAtomicAcrossConcurrentWorldProviders()
    {
        await using var firstProvider = CreateProvider();
        await using var secondProvider = CreateProvider();
        var firstStore = firstProvider.GetRequiredService<IGameSessionClaimStore>();
        var secondStore = secondProvider.GetRequiredService<IGameSessionClaimStore>();
        await ClearClaimAsync(firstProvider, 42);

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = Task.Run(async () =>
        {
            await start.Task;
            return await firstStore.TryClaimAsync(42, "world-1");
        });
        var second = Task.Run(async () =>
        {
            await start.Task;
            return await secondStore.TryClaimAsync(42, "world-2");
        });
        start.SetResult();

        var results = await Task.WhenAll(first, second);

        Assert.AreEqual(1, results.Count(result => result));
        var cache = firstProvider.GetRequiredService<IFusionCache>();
        var current = await cache.TryGetAsync<string>(Key(42), EntryOptions());
        Assert.IsTrue(current.HasValue);
        Assert.IsTrue(current.Value is "world-1" or "world-2");
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task SignInWorldAsync_IsExclusiveAcrossIndependentProviders_AndClaimCanBeReclaimed()
    {
        const uint masterId = 47;
        await using var gate = new SignInRaceGate();
        var firstHarness = CreateSignInHarness("world-1", gate);
        var secondHarness = CreateSignInHarness("world-2", gate);
        await using var firstProvider = CreateProvider(firstHarness);
        await using var secondProvider = CreateProvider(secondHarness);
        await ClearClaimAsync(firstProvider, masterId);

        await using var firstScope = firstProvider.CreateAsyncScope();
        await using var secondScope = secondProvider.CreateAsyncScope();
        var firstAuthentication = firstScope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        var secondAuthentication = secondScope.ServiceProvider.GetRequiredService<IAuthenticationService>();

        var firstAttempt = firstAuthentication.SignInWorldAsync(CreateSignInRequest()).AsTask();
        var secondAttempt = secondAuthentication.SignInWorldAsync(CreateSignInRequest()).AsTask();
        var results = await Task.WhenAll(firstAttempt, secondAttempt);

        Assert.AreEqual(1, results.Count(result => result.Succeeded));
        Assert.AreEqual(1, results.Count(result => result.IsAlreadyLoggedOn));
        Assert.AreEqual(1, firstHarness.HydrationCalls + secondHarness.HydrationCalls);
        Assert.AreEqual(1, firstHarness.CharacterAddCalls + secondHarness.CharacterAddCalls);

        var winner = results[0].Succeeded ? firstAuthentication : secondAuthentication;
        await winner.SignOutAsync();
        Assert.IsFalse((await firstProvider.GetRequiredService<IFusionCache>()
            .TryGetAsync<string>(Key(masterId), EntryOptions())).HasValue);

        var reclaimHarness = CreateSignInHarness("world-reclaim", new SignInRaceGate(1));
        await using var reclaimProvider = CreateProvider(reclaimHarness);
        await using var reclaimScope = reclaimProvider.CreateAsyncScope();
        var reclaimAuthentication = reclaimScope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        var reclaimResult = await reclaimAuthentication.SignInWorldAsync(CreateSignInRequest());

        Assert.IsTrue(reclaimResult.Succeeded);
        Assert.AreEqual(1, reclaimHarness.HydrationCalls);
        Assert.AreEqual(1, reclaimHarness.CharacterAddCalls);
        await reclaimAuthentication.SignOutAsync();
        await ClearClaimAsync(reclaimProvider, masterId);
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task ExecuteIfOwnerAsync_HoldsClaimLockDuringPromotionCallback()
    {
        await using var firstProvider = CreateProvider();
        await using var secondProvider = CreateProvider();
        var firstStore = firstProvider.GetRequiredService<IGameSessionClaimStore>();
        var secondStore = secondProvider.GetRequiredService<IGameSessionClaimStore>();
        const uint masterId = 46;
        await ClearClaimAsync(firstProvider, masterId);

        Assert.IsTrue(await firstStore.TryClaimAsync(masterId, "world-1"));
        var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ownerOperation = firstStore.ExecuteIfOwnerAsync(masterId, "world-1", async _ =>
        {
            callbackStarted.SetResult();
            await releaseCallback.Task;
            return true;
        });
        await callbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var replacementOperation = secondStore.TryClaimAsync(masterId, "world-2");
        var completed = await Task.WhenAny(replacementOperation, Task.Delay(500));
        Assert.AreNotSame(replacementOperation, completed);

        releaseCallback.SetResult();
        Assert.IsTrue(await ownerOperation);
        Assert.IsFalse(await replacementOperation);
        Assert.IsTrue(await firstStore.ReleaseAsync(masterId, "world-1"));
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task ReleaseAndRenewAsync_RequireExactClaimOwner()
    {
        await using var firstProvider = CreateProvider();
        await using var secondProvider = CreateProvider();
        var firstStore = firstProvider.GetRequiredService<IGameSessionClaimStore>();
        var secondStore = secondProvider.GetRequiredService<IGameSessionClaimStore>();
        await ClearClaimAsync(firstProvider, 43);

        Assert.IsTrue(await firstStore.TryClaimAsync(43, "owner"));
        Assert.IsFalse(await secondStore.RenewAsync(43, "other-owner"));
        Assert.IsFalse(await secondStore.ReleaseAsync(43, "other-owner"));
        Assert.IsTrue((await secondProvider.GetRequiredService<IFusionCache>().TryGetAsync<string>(Key(43), EntryOptions())).HasValue);
        Assert.IsTrue(await secondStore.RenewAsync(43, "owner"));
        Assert.IsTrue(await secondStore.ReleaseAsync(43, "owner"));
        Assert.IsFalse((await firstProvider.GetRequiredService<IFusionCache>().TryGetAsync<string>(Key(43), EntryOptions())).HasValue);
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task ClaimExpiresAndCanBeReclaimedAfterWorldCrash()
    {
        await using var provider = CreateProvider();
        var store = provider.GetRequiredService<IGameSessionClaimStore>();
        var cache = provider.GetRequiredService<IFusionCache>();
        const uint masterId = 44;
        await ClearClaimAsync(provider, masterId);

        Assert.IsTrue(await store.TryClaimAsync(masterId, "crashed-world"));
        await cache.SetAsync(Key(masterId), "crashed-world", new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromMilliseconds(100),
            DistributedCacheDuration = TimeSpan.FromMilliseconds(100),
            IsFailSafeEnabled = false,
            SkipMemoryCacheRead = true,
            SkipMemoryCacheWrite = true
        });

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while ((await cache.TryGetAsync<string>(Key(masterId), EntryOptions(), timeout.Token)).HasValue)
        {
            await Task.Delay(25, timeout.Token);
        }

        Assert.IsTrue(await store.TryClaimAsync(masterId, "replacement-world"));
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task StartupResolvesFusionCacheDistributedClaimStoreFromConfiguredCacheConnection()
    {
        await using var provider = CreateProvider();

        Assert.IsNotNull(provider.GetRequiredService<IDistributedCache>());
        Assert.IsNotNull(provider.GetRequiredService<IFusionCache>());
        Assert.IsNotNull(provider.GetRequiredService<IFusionCacheDistributedLocker>());
        var claimStore = provider.GetRequiredService<IGameSessionClaimStore>();
        Assert.IsInstanceOfType<FusionCacheGameSessionClaimStore>(claimStore);
        Assert.IsTrue(await claimStore.TryClaimAsync(45, "startup-world"));
        Assert.IsTrue(await claimStore.ReleaseAsync(45, "startup-world"));
    }

    private static ServiceProvider CreateProvider(SignInHarness? harness = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:cache"] = _redis!.GetConnectionString(),
                ["HAGALAZ_WORLD_ID"] = "1",
                ["World:Name"] = "World 1",
                ["World:AdvertisedEndpoint:Host"] = "127.0.0.1",
                ["World:AdvertisedEndpoint:Port"] = "443"
            })
            .Build();
        var services = new ServiceCollection();
        new Hagalaz.Services.GameWorld.Startup(configuration).ConfigureServices(services);
        if (harness != null)
        {
            services.AddSingleton(harness.CharacterService);
            services.AddSingleton(harness.CharacterFactory);
            services.AddSingleton(harness.CharacterHydrationService);
            services.AddSingleton(harness.CharacterPersistenceService);
            services.AddSingleton(harness.CharacterLogoutService);
            services.AddSingleton(harness.Mapper);
            services.AddSingleton(harness.ClaimsPrincipalFactory);
            services.AddSingleton(harness.ContextAccessor);
            services.AddSingleton(harness.Mediator);
            services.AddSingleton(harness.SignInUserRequestClient);
            services.AddSingleton(harness.ValidateUserCredentialsRequestClient);
            services.AddSingleton(harness.GetUserInfoRequestClient);
            services.AddSingleton(harness.RevokeTokenRequestClient);
            services.AddSingleton(harness.GetCharacterRequestClient);
        }
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static SignInHarness CreateSignInHarness(string connectionId, SignInRaceGate gate)
    {
        var harness = new SignInHarness(connectionId);
        var mapper = harness.Mapper;
        mapper.Map<CharacterModel>(Arg.Any<CharacterHydrated>()).Returns(new CharacterModel());
        mapper.Map<HydratedClaims>(Arg.Any<AuthenticationProperties>()).Returns(new HydratedClaims());

        // NSubstitute requires observing the ValueTask returned by each member while configuring it.
#pragma warning disable CA2012
        SubstituteExtensions.Returns(harness.CharacterService.CountAsync(), ValueTask.FromResult(0));
        SubstituteExtensions.Returns(
            harness.CharacterService.AddAsync(Arg.Any<ICharacter>()),
            _ =>
            {
                Interlocked.Increment(ref harness.CharacterAddCalls);
                return ValueTask.FromResult(true);
            });
        SubstituteExtensions.Returns(harness.CharacterService.RemoveAsync(Arg.Any<ICharacter>()), ValueTask.FromResult(true));
#pragma warning restore CA2012
        harness.CharacterHydrationService.HydrateAsync(Arg.Any<ICharacter>(), Arg.Any<CharacterModel>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref harness.HydrationCalls);
                return Task.FromResult(true);
            });
        harness.CharacterFactory.Create(Arg.Any<IGameSession>(), Arg.Any<IGameClient>())
            .Returns(harness.Character);
        harness.PersistenceServiceSetup();

        var signInResponse = CreateResponse(new SignInUserResponseMessage
        {
            Succeeded = true,
            IdToken = "id-token",
            AccessToken = "access-token",
            Scope = "openid",
            ExpireDate = DateTimeOffset.UtcNow.AddMinutes(5),
            TokenType = "Bearer"
        });
        harness.SignInUserRequestClient
            .GetResponse<SignInUserResponseMessage>(Arg.Any<SignInUserRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(_ => gate.WaitForSignInAsync(signInResponse));
        var userInfoResponse = CreateResponse(new GetUserInfoResponseMessage
        {
            Succeeded = true,
            Claims = new Dictionary<string, object> { [OpenIddict.Abstractions.OpenIddictConstants.Claims.Subject] = "47" }
        });
        harness.GetUserInfoRequestClient
            .GetResponse<GetUserInfoResponseMessage>(Arg.Any<GetUserInfoRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .ReturnsForAnyArgs(Task.FromResult(userInfoResponse));
        var characterResponse = CreateResponse<CharacterHydrated, CharacterNotFound>(CreateCharacterHydrated());
        harness.GetCharacterRequestClient
            .GetResponse<CharacterHydrated, CharacterNotFound>(Arg.Any<HydrateCharacter>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .ReturnsForAnyArgs(Task.FromResult(characterResponse));
        var revokeTokenResponse = CreateResponse(new RevokeTokenResponseMessage { Succeeded = true });
        harness.RevokeTokenRequestClient
            .GetResponse<RevokeTokenResponseMessage>(Arg.Any<RevokeTokenRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .ReturnsForAnyArgs(Task.FromResult(revokeTokenResponse));

        return harness;
    }

    private static SignInRequest CreateSignInRequest() => new()
    {
        Login = "login",
        Password = "password",
        GameClient = Substitute.For<IGameClient>()
    };

    private static CharacterHydrated CreateCharacterHydrated() => new()
    {
        MasterId = 47,
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

    private static Response<T1, T2> CreateResponse<T1, T2>(T1 message)
        where T1 : class
        where T2 : class
    {
        var firstResponse = CreateResponse(message);
        var secondResponseTask = new TaskCompletionSource<Response<T2>>().Task;
        return new Response<T1, T2>(Task.FromResult(firstResponse), secondResponseTask);
    }

    private sealed class SignInRaceGate : IAsyncDisposable
    {
        private readonly int _participants;
        private readonly TaskCompletionSource _bothReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _ready;

        public SignInRaceGate(int participants = 2)
        {
            _participants = participants;
        }

        public async Task<Response<SignInUserResponseMessage>> WaitForSignInAsync(Response<SignInUserResponseMessage> response)
        {
            if (Interlocked.Increment(ref _ready) == _participants)
            {
                _bothReady.TrySetResult();
            }

            await _bothReady.Task;
            return response;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class SignInHarness
    {
        public SignInHarness(string connectionId)
        {
            var context = Substitute.For<RaidoCallerContext>();
            context.ConnectionId.Returns(connectionId);
            context.RemoteIPEndPoint.Returns(new IPEndPoint(IPAddress.Loopback, 43594));
            context.Features.Returns(new FeatureCollection());
            ContextAccessor.Context.Returns(context);
            ClaimsPrincipalFactory.Create(Arg.Any<IDictionary<string, object>>())
                .Returns(new ClaimsPrincipal(new ClaimsIdentity("integration")));
        }

        public ICharacterService CharacterService { get; } = Substitute.For<ICharacterService>();
        public ICharacterFactory CharacterFactory { get; } = Substitute.For<ICharacterFactory>();
        public ICharacterHydrationService CharacterHydrationService { get; } = Substitute.For<ICharacterHydrationService>();
        public ICharacterPersistenceService CharacterPersistenceService { get; } = Substitute.For<ICharacterPersistenceService>();
        public ICharacterLogoutService CharacterLogoutService { get; } = Substitute.For<ICharacterLogoutService>();
        public IMapper Mapper { get; } = Substitute.For<IMapper>();
        public IClaimsPrincipalFactory ClaimsPrincipalFactory { get; } = Substitute.For<IClaimsPrincipalFactory>();
        public IRaidoCallerContextAccessor ContextAccessor { get; } = Substitute.For<IRaidoCallerContextAccessor>();
        public IGameMediator Mediator { get; } = Substitute.For<IGameMediator>();
        public IRequestClient<SignInUserRequestMessage> SignInUserRequestClient { get; } = Substitute.For<IRequestClient<SignInUserRequestMessage>>();
        public IRequestClient<ValidateUserCredentialsRequestMessage> ValidateUserCredentialsRequestClient { get; } = Substitute.For<IRequestClient<ValidateUserCredentialsRequestMessage>>();
        public IRequestClient<GetUserInfoRequestMessage> GetUserInfoRequestClient { get; } = Substitute.For<IRequestClient<GetUserInfoRequestMessage>>();
        public IRequestClient<RevokeTokenRequestMessage> RevokeTokenRequestClient { get; } = Substitute.For<IRequestClient<RevokeTokenRequestMessage>>();
        public IRequestClient<HydrateCharacter> GetCharacterRequestClient { get; } = Substitute.For<IRequestClient<HydrateCharacter>>();
        public ICharacter Character { get; } = Substitute.For<ICharacter>();
        public int HydrationCalls;
        public int CharacterAddCalls;

        public void PersistenceServiceSetup()
        {
            CharacterPersistenceService.PersistAsync(Arg.Any<ICharacter>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            CharacterLogoutService.DetachAsync(Arg.Any<ICharacter>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
        }
    }

    private static async Task ClearClaimAsync(ServiceProvider provider, uint masterId) =>
        await provider.GetRequiredService<IFusionCache>().RemoveAsync(Key(masterId), EntryOptions());

    private static FusionCacheEntryOptions EntryOptions() => new()
    {
        IsFailSafeEnabled = false,
        SkipMemoryCacheRead = true,
        SkipMemoryCacheWrite = true
    };

    private static string Key(uint masterId) => $"hagalaz:game-session:{masterId}";
}
