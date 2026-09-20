using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Characters.Messages;
using Hagalaz.Data;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using Hagalaz.Services.GameWorld.Store;
using MassTransit;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldSessionAdmissionPersistenceOwnershipTests
{
    [TestMethod]
    public async Task AdmitAsync_WhenDuplicateCharacterIsRejected_PreservesExistingPersistenceOwner()
    {
        await using var fixture = CreateFixture(capacity: 2, existingCharacter: true, sessionGeneration: 2);

        var result = await fixture.Service.AdmitAsync(
            CreateSignInRequest(),
            fixture.Context,
            42,
            new AuthenticationProperties());

        Assert.IsFalse(result.Succeeded);
        Assert.AreSame(fixture.ExistingCharacter, await fixture.CharacterStore.FindByIdAsync(42));
        Assert.AreEqual(1, await fixture.CharacterStore.CountAsync());
        Assert.IsFalse(fixture.PersistenceState.TryGetPending(42, out _));
        Assert.AreEqual(101L, fixture.PersistenceState.NextRevision(42));
        fixture.CandidateCharacter.Received(1).Destroy();
        await fixture.GameSessionService.Received(1).RemoveSession(fixture.Session, CancellationToken.None);
        await fixture.GameSessionService.DidNotReceive().CommitWorldSession(
            Arg.Any<IGameSession>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task AdmitAsync_WhenCharacterStoreIsAtCapacity_DoesNotInitializePersistence()
    {
        var persistence = Substitute.For<ICharacterPersistenceService>();

        await using var fixture = CreateFixture(
            capacity: 1,
            existingCharacter: true,
            sessionGeneration: 2,
            existingMasterId: 99,
            persistenceService: persistence);

        var result = await fixture.Service.AdmitAsync(
            CreateSignInRequest(),
            fixture.Context,
            42,
            new AuthenticationProperties());

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(1, await fixture.CharacterStore.CountAsync());
        Assert.AreSame(fixture.ExistingCharacter, await fixture.CharacterStore.FindByIdAsync(99));
        persistence.DidNotReceive().InitializeRevision(Arg.Any<uint>(), Arg.Any<long>(), Arg.Any<long>());
        Assert.AreEqual(1L, fixture.PersistenceState.NextRevision(42));
        fixture.CandidateCharacter.Received(1).Destroy();
        await fixture.GameSessionService.Received(1).RemoveSession(fixture.Session, CancellationToken.None);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task AdmitAsync_WhenDehydrationCapturesRegisteredCharacterBeforeInitialization_PreservesMonotonicOwnership()
    {
        var registered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueAdmission = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var persistence = Substitute.For<ICharacterPersistenceService>();
        persistence.PersistAsync(
                42,
                Arg.Any<CharacterModel>(),
                false,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CharacterPersistenceReceipt?>(null));

        await using var fixture = CreateFixture(
            capacity: 2,
            existingCharacter: false,
            sessionGeneration: 2,
            persistenceService: persistence,
            characterServiceDecorator: service => new GatedCharacterService(service, registered, continueAdmission));
        persistence
            .When(service => service.InitializeRevision(Arg.Any<uint>(), Arg.Any<long>(), Arg.Any<long>()))
            .Do(callInfo => fixture.PersistenceState.InitializeRevision(
                callInfo.Arg<uint>(),
                callInfo.ArgAt<long>(1),
                callInfo.ArgAt<long>(2)));

        var admission = fixture.Service.AdmitAsync(
            CreateSignInRequest(),
            fixture.Context,
            42,
            new AuthenticationProperties()).AsTask();

        await registered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsFalse(admission.IsCompleted);
        Assert.AreSame(fixture.CandidateCharacter, await fixture.CharacterStore.FindByIdAsync(42));

        var dehydration = Substitute.For<ICharacterDehydrationService>();
        dehydration.Dehydrate(fixture.CandidateCharacter).Returns(new CharacterModel());
        var logout = Substitute.For<ICharacterLogoutService>();
        logout.RecoverPendingLogoutsAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        logout.IsPendingLogout(42).Returns(false);

        using var provider = new ServiceCollection()
            .AddScoped<ICharacterPersistenceService>(_ => persistence)
            .AddScoped<ICharacterDehydrationService>(_ => dehydration)
            .AddScoped<ICharacterLogoutService>(_ => logout)
            .AddSingleton(fixture.PersistenceState)
            .BuildServiceProvider();
        var worker = new CharacterDehydrationWorkerService(
            NullLogger<CharacterDehydrationWorkerService>.Instance,
            provider,
            fixture.CharacterStore,
            new InlineTaskScheduler());

        try
        {
            await worker.FlushAsync(force: false, CancellationToken.None);

            dehydration.Received(1).Dehydrate(fixture.CandidateCharacter);
            await persistence.Received(1).PersistAsync(
                42,
                Arg.Is<CharacterModel>(model => model.SnapshotRevision == 1),
                false,
                Arg.Any<CancellationToken>());
        }
        finally
        {
            continueAdmission.TrySetResult(true);
        }

        Assert.IsTrue((await admission).Succeeded);

        persistence.Received(1).InitializeRevision(42, 100, 2);
        Assert.IsFalse(fixture.PersistenceState.TryGetPending(42, out _));
        Assert.IsFalse(fixture.PersistenceState.Release(42, 1));
        Assert.AreEqual(101L, fixture.PersistenceState.NextRevision(42));
    }

    private static SignInRequest CreateSignInRequest() => new()
    {
        GameClient = Substitute.For<IGameClient>()
    };

    private static AdmissionFixture CreateFixture(
        int capacity,
        bool existingCharacter,
        long sessionGeneration,
        ICharacterPersistenceService? persistenceService = null,
        Func<ICharacterService, ICharacterService>? characterServiceDecorator = null,
        uint existingMasterId = 42)
    {
        var characterStore = new CharacterStore(Options.Create(new GameServerOptions
        {
            ClientRevision = 1,
            ClientRevisionPatch = 0,
            AuthenticationToken = "test",
            Limits = { MaxConcurrentConnections = capacity }
        }));
        var entityStore = new EntityStore();
        var characterService = new CharacterService(characterStore, entityStore);
        var existing = existingCharacter ? CreateCharacter(existingMasterId) : null;
        if (existing is not null)
        {
            Assert.IsTrue(characterService.AddAsync(existing).AsTask().GetAwaiter().GetResult());
        }

        var candidate = CreateCharacter(42);
        var persistenceState = new CharacterPersistenceState();
        var dbContext = Substitute.For<HagalazDbContext>(CreateDbContextOptions());
        var persistence = persistenceService ?? new CharacterPersistenceService(
            NullLogger<CharacterPersistenceService>.Instance,
            Substitute.For<IMapper>(),
            Substitute.For<IPublishEndpoint>(),
            dbContext,
            persistenceState);
        if (existing is not null && existingMasterId == 42)
        {
            persistence.InitializeRevision(42, 100, 1);
        }

        var mapper = Substitute.For<IMapper>();
        mapper.Map<CharacterModel>(Arg.Any<CharacterHydrated>())
            .Returns(new CharacterModel { SnapshotRevision = 100 });
        mapper.Map<HydratedClaims>(Arg.Any<AuthenticationProperties>())
            .Returns(new HydratedClaims());

        var characterFactory = Substitute.For<ICharacterFactory>();
        characterFactory.Create(Arg.Any<IGameSession>(), Arg.Any<IGameClient>()).Returns(candidate);
        var hydration = Substitute.For<ICharacterHydrationService>();
        hydration.HydrateAsync(candidate, Arg.Any<CharacterModel>()).Returns(Task.FromResult(true));

        var session = Substitute.For<IGameWorldSession>();
        session.MasterId.Returns(42u);
        session.ConnectionId.Returns("duplicate-connection");
        session.SessionGeneration.Returns(sessionGeneration);

        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.TryAddWorldSession(42, "duplicate-connection", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(IGameSession?, bool)>((session, true)));
        gameSessionService.CommitWorldSession(session, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        gameSessionService.RemoveSession(session, Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var hydrated = new CharacterHydrated
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
            State = null!,
            SnapshotRevision = 100
        };
        var hydrateClient = Substitute.For<IRequestClient<HydrateCharacter>>();
        var hydrateResponse = CreateResponse<CharacterHydrated, CharacterNotFound>(hydrated);
        hydrateClient.GetResponse<CharacterHydrated, CharacterNotFound>(
                Arg.Any<HydrateCharacter>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .ReturnsForAnyArgs(Task.FromResult(hydrateResponse));

        var context = Substitute.For<RaidoCallerContext>();
        context.ConnectionId.Returns("duplicate-connection");
        context.Features.Returns(new FeatureCollection());
        var workerExecution = Substitute.For<IGameWorkerExecution>();
        workerExecution.ExecutionCompleted.Returns(Task.FromResult(true));

        var service = new WorldSessionAdmissionService(
            NullLogger<WorldSessionAdmissionService>.Instance,
            mapper,
            characterServiceDecorator?.Invoke(characterService) ?? characterService,
            characterFactory,
            hydration,
            persistence,
            gameSessionService,
            hydrateClient,
            new InlineTaskScheduler(),
            workerExecution);

        return new AdmissionFixture(
            service,
            context,
            gameSessionService,
            session,
            characterStore,
            existing,
            candidate,
            persistenceState,
            dbContext);
    }

    private static ICharacter CreateCharacter(uint masterId)
    {
        var character = EntityTestFactory.Create<ICharacter>();
        character.MasterId.Returns(masterId);
        return character;
    }

    private static DbContextOptions<HagalazDbContext> CreateDbContextOptions() =>
        new DbContextOptionsBuilder<HagalazDbContext>()
            .UseMySQL("Server=localhost;Database=hagalaz;User=root;Password=;")
            .Options;

    private static Response<T1, T2> CreateResponse<T1, T2>(T1 message)
        where T1 : class
        where T2 : class
    {
        var firstResponse = Substitute.For<Response<T1>>();
        firstResponse.Message.Returns(message);
        ((Response)firstResponse).Message.Returns(message);
        var constructor = typeof(Response<T1, T2>)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)[0];
        var response = (Response<T1, T2>)constructor.Invoke([
            Task.FromResult(firstResponse),
            new TaskCompletionSource<Response<T2>>().Task]);
        typeof(Response<T1, T2>)
            .GetField("_response", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(response, firstResponse);
        return response;
    }

    private sealed record AdmissionFixture(
        WorldSessionAdmissionService Service,
        RaidoCallerContext Context,
        IGameSessionService GameSessionService,
        IGameSession Session,
        CharacterStore CharacterStore,
        ICharacter? ExistingCharacter,
        ICharacter CandidateCharacter,
        CharacterPersistenceState PersistenceState,
        HagalazDbContext DbContext) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await DbContext.DisposeAsync();
    }

    private sealed class InlineTaskScheduler : IRsTaskService
    {
        public void Schedule(ITaskItem action) => action.Tick();
        public void Tick() { }
    }

    private sealed class GatedCharacterService : ICharacterService
    {
        private readonly ICharacterService _inner;
        private readonly TaskCompletionSource<bool> _registered;
        private readonly TaskCompletionSource<bool> _continueAdmission;

        public GatedCharacterService(
            ICharacterService inner,
            TaskCompletionSource<bool> registered,
            TaskCompletionSource<bool> continueAdmission)
        {
            _inner = inner;
            _registered = registered;
            _continueAdmission = continueAdmission;
        }

        public async ValueTask<bool> AddAsync(ICharacter character)
        {
            var added = await _inner.AddAsync(character);
            if (!added)
            {
                return false;
            }

            _registered.TrySetResult(true);
            await _continueAdmission.Task;
            return true;
        }

        public ValueTask<int> CountAsync() => _inner.CountAsync();

        public ValueTask<ICharacter?> FindByIndex(int index) => _inner.FindByIndex(index);

        public ValueTask<ICharacter?> FindByMasterId(uint masterId) => _inner.FindByMasterId(masterId);

        public bool Remove(ICharacter character) => _inner.Remove(character);

        public ValueTask<bool> RemoveAsync(ICharacter character) => _inner.RemoveAsync(character);
    }
}
