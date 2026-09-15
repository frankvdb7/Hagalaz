using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldSessionAdmissionServiceTests
{
    [TestMethod]
    public async Task AdmitAsync_WhenAdmissionSucceeds_CommitsSessionAndPublishesFeatures()
    {
        var fixture = CreateFixture(commitResult: true);

        var result = await fixture.Service.AdmitAsync(
            CreateSignInRequest(),
            fixture.Context,
            42,
            new AuthenticationProperties());

        Assert.IsTrue(result.Succeeded);
        await fixture.GameSessionService.Received(1).CommitWorldSession(fixture.Session, Arg.Any<CancellationToken>());
        Assert.AreSame(fixture.Character, fixture.Context.Features.Get<ICharacterFeature>()!.Character);
        Assert.AreSame(fixture.Session, fixture.Context.Features.Get<Hagalaz.Services.GameWorld.Features.ISessionFeature>()!.Session);
    }

    [TestMethod]
    public async Task AdmitAsync_WhenHydrationRequestFails_PropagatesFailureAndRollsBackSession()
    {
        var fixture = CreateFixture(commitResult: true);
        var failure = new InvalidOperationException("hydrate request failed");
        fixture.HydrateClient.GetResponse<CharacterHydrated, CharacterNotFound>(
                Arg.Any<HydrateCharacter>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(Task.FromException<Response<CharacterHydrated, CharacterNotFound>>(failure));

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => fixture.Service.AdmitAsync(
            CreateSignInRequest(),
            fixture.Context,
            42,
            new AuthenticationProperties()).AsTask());

        Assert.AreSame(failure, actual);
        await fixture.GameSessionService.Received(1).RemoveSession(fixture.Session, CancellationToken.None);
    }

    [TestMethod]
    public async Task AdmitAsync_WhenHydrationRequestIsCanceled_PropagatesCancellationAndRollsBackSession()
    {
        var fixture = CreateFixture(commitResult: true);
        using var cancellation = new CancellationTokenSource();
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new TaskCompletionSource<Response<CharacterHydrated, CharacterNotFound>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.HydrateClient.GetResponse<CharacterHydrated, CharacterNotFound>(
                Arg.Any<HydrateCharacter>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(callInfo =>
            {
                var requestToken = callInfo.Arg<CancellationToken>();
                requestStarted.TrySetResult();
                requestToken.Register(() => response.TrySetCanceled(requestToken));
                return response.Task;
            });

        var admission = fixture.Service.AdmitAsync(
            CreateSignInRequest(),
            fixture.Context,
            42,
            new AuthenticationProperties(),
            cancellation.Token).AsTask();

        await requestStarted.Task;
        cancellation.Cancel();

        var actual = await Assert.ThrowsAsync<OperationCanceledException>(() => admission);

        Assert.IsTrue(cancellation.IsCancellationRequested);
        Assert.AreEqual(cancellation.Token, actual.CancellationToken);
        await fixture.GameSessionService.Received(1).RemoveSession(fixture.Session, CancellationToken.None);
    }

    [TestMethod]
    public async Task AdmitAsync_WhenCharacterIsNotFound_ReturnsFailureAndRollsBackSession()
    {
        var fixture = CreateFixture(commitResult: true);
        var notFoundResponse = CreateSecondResponse<CharacterHydrated, CharacterNotFound>(new CharacterNotFound(System.Guid.NewGuid(), 42));
        fixture.HydrateClient.GetResponse<CharacterHydrated, CharacterNotFound>(
                Arg.Any<HydrateCharacter>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(Task.FromResult(notFoundResponse));

        var result = await fixture.Service.AdmitAsync(
            CreateSignInRequest(),
            fixture.Context,
            42,
            new AuthenticationProperties());

        Assert.IsFalse(result.Succeeded);
        await fixture.GameSessionService.Received(1).RemoveSession(fixture.Session, CancellationToken.None);
    }

    [TestMethod]
    public async Task AdmitAsync_WhenCommitFails_RollsBackCharacterAndSessionReservation()
    {
        var fixture = CreateFixture(commitResult: false);

        var result = await fixture.Service.AdmitAsync(
            CreateSignInRequest(),
            fixture.Context,
            42,
            new AuthenticationProperties());

        Assert.IsFalse(result.Succeeded);
        await fixture.GameSessionService.Received(1).RemoveSession(fixture.Session, CancellationToken.None);
        fixture.CharacterStore.Received(1).Remove(fixture.Character);
        fixture.Character.Received(1).Destroy();
        fixture.PersistenceService.Received(1).InitializeRevision(42, 7);
        Assert.IsNull(fixture.Context.Features.Get<ICharacterFeature>());
    }

    [TestMethod]
    public async Task AdmitAsync_InitializesRevisionBeforePublishingCharacterOwnership()
    {
        var fixture = CreateFixture(commitResult: true);
        var order = new List<string>();
#pragma warning disable CA2012
        fixture.CharacterService
            .AddAsync(fixture.Character)
            .Returns(_ =>
            {
                order.Add("add");
                return ValueTask.FromResult(true);
            });
#pragma warning restore CA2012
        fixture.PersistenceService
            .When(service => service.InitializeRevision(42, 7))
            .Do(_ => order.Add("initialize"));

        var result = await fixture.Service.AdmitAsync(
            CreateSignInRequest(),
            fixture.Context,
            42,
            new AuthenticationProperties());

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEqual(new[] { "initialize", "add" }, order);
    }

    [TestMethod]
    public async Task AdmitAsync_WhenCharacterRegistrationThrows_DestroysUnregisteredCharacter()
    {
        var fixture = CreateFixture(commitResult: true);
        var failure = new InvalidOperationException("character registration failed");
#pragma warning disable CA2012
        fixture.CharacterService
            .AddAsync(fixture.Character)
            .Returns(_ => ValueTask.FromException<bool>(failure));
#pragma warning restore CA2012

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => fixture.Service.AdmitAsync(
                CreateSignInRequest(),
                fixture.Context,
                42,
                new AuthenticationProperties()).AsTask());

        Assert.AreSame(failure, exception);
        fixture.Character.Received(1).Destroy();
        fixture.PersistenceService.Received(1).InitializeRevision(42, 7);
    }

    [TestMethod]
    public async Task AdmitAsync_WhenCommitFails_DefersRegisteredCharacterCleanupToGameWorker()
    {
        var scheduler = new DeferredTaskScheduler();
        var fixture = CreateFixture(commitResult: false, scheduler);
        var admission = fixture.Service.AdmitAsync(
            CreateSignInRequest(),
            fixture.Context,
            42,
            new AuthenticationProperties()).AsTask();

        await scheduler.Scheduled.Task;
        fixture.Character.DidNotReceive().Destroy();

        scheduler.Tick();
        Assert.IsFalse((await admission).Succeeded);
        fixture.CharacterStore.Received(1).Remove(fixture.Character);
        fixture.Character.Received(1).Destroy();
    }

    private static Fixture CreateFixture(bool commitResult, IRsTaskService? scheduler = null)
    {
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CharacterModel>(Arg.Any<CharacterHydrated>()).Returns(new CharacterModel { SnapshotRevision = 7 });
        mapper.Map<HydratedClaims>(Arg.Any<AuthenticationProperties>()).Returns(new HydratedClaims());

        var character = Substitute.For<ICharacter>();
        var characterFactory = Substitute.For<ICharacterFactory>();
        characterFactory.Create(Arg.Any<IGameSession>(), Arg.Any<IGameClient>()).Returns(character);
        var characterService = Substitute.For<ICharacterService>();
#pragma warning disable CA2012, CS8620
        characterService.AddAsync(character).Returns(ValueTask.FromResult(true));
        characterService.RemoveAsync(character).Returns(ValueTask.FromResult(true));
#pragma warning restore CA2012, CS8620
        var hydration = Substitute.For<ICharacterHydrationService>();
        hydration.HydrateAsync(character, Arg.Any<CharacterModel>()).Returns(Task.FromResult(true));
        var characterStore = Substitute.For<ICharacterStore>();
        characterStore.Remove(character).Returns(true);
        var persistence = Substitute.For<ICharacterPersistenceService>();
        var sessionService = Substitute.For<IGameSessionService>();
        var session = Substitute.For<IGameWorldSession>();
        session.ConnectionId.Returns("connection");
        session.MasterId.Returns(42u);
        sessionService.TryAddWorldSession(42, "connection", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(IGameSession?, bool)>((session, true)));
        sessionService.CommitWorldSession(session, Arg.Any<CancellationToken>()).Returns(Task.FromResult(commitResult));
        sessionService.RemoveSession(session, Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var response = CreateResponse<CharacterHydrated, CharacterNotFound>(new CharacterHydrated
        {
            MasterId = 42,
            CorrelationId = System.Guid.NewGuid(),
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
        });
        var hydrateClient = Substitute.For<IRequestClient<HydrateCharacter>>();
        hydrateClient.GetResponse<CharacterHydrated, CharacterNotFound>(
                Arg.Any<HydrateCharacter>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(Task.FromResult(response));

        var context = Substitute.For<RaidoCallerContext>();
        context.ConnectionId.Returns("connection");
        context.Features.Returns(new FeatureCollection());
        var service = new WorldSessionAdmissionService(
            NullLogger<WorldSessionAdmissionService>.Instance,
            mapper,
            characterService,
            characterStore,
            characterFactory,
            hydration,
            persistence,
            sessionService,
            hydrateClient,
            scheduler ?? new InlineTaskScheduler());
        return new Fixture(service, context, sessionService, session, characterService, character, persistence, characterStore, hydrateClient);
    }

    private static SignInRequest CreateSignInRequest() => new()
    {
        GameClient = Substitute.For<IGameClient>()
    };

    private static Response<T1, T2> CreateResponse<T1, T2>(T1 message)
        where T1 : class
        where T2 : class
    {
        var firstResponse = Substitute.For<Response<T1>>();
        firstResponse.Message.Returns(message);
        ((Response)firstResponse).Message.Returns(message);
        var constructor = typeof(Response<T1, T2>)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single();
        var response = (Response<T1, T2>)constructor.Invoke([Task.FromResult(firstResponse), new TaskCompletionSource<Response<T2>>().Task]);
        typeof(Response<T1, T2>).GetField("_response", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(response, firstResponse);
        return response;
    }

    private static Response<T1, T2> CreateSecondResponse<T1, T2>(T2 message)
        where T1 : class
        where T2 : class
    {
        var secondResponse = Substitute.For<Response<T2>>();
        secondResponse.Message.Returns(message);
        ((Response)secondResponse).Message.Returns(message);
        var constructor = typeof(Response<T1, T2>)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single();
        var response = (Response<T1, T2>)constructor.Invoke([new TaskCompletionSource<Response<T1>>().Task, Task.FromResult(secondResponse)]);
        typeof(Response<T1, T2>).GetField("_response", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(response, secondResponse);
        return response;
    }

    private sealed record Fixture(
        WorldSessionAdmissionService Service,
        RaidoCallerContext Context,
        IGameSessionService GameSessionService,
        IGameSession Session,
        ICharacterService CharacterService,
        ICharacter Character,
        ICharacterPersistenceService PersistenceService,
        ICharacterStore CharacterStore,
        IRequestClient<HydrateCharacter> HydrateClient);

    private sealed class InlineTaskScheduler : IRsTaskService
    {
        public void Schedule(ITaskItem action) => action.Tick();
        public void Tick() { }
    }

    private sealed class DeferredTaskScheduler : IRsTaskService
    {
        public TaskCompletionSource<bool> Scheduled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private ITaskItem? _pending;

        public void Schedule(ITaskItem action)
        {
            _pending = action;
            Scheduled.TrySetResult(true);
        }

        public void Tick()
        {
            var pending = _pending ?? throw new InvalidOperationException("No task was scheduled.");
            _pending = null;
            pending.Tick();
        }
    }
}
