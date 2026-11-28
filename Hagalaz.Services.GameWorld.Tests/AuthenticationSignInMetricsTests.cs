using Hagalaz.Authorization.Messages;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;
using NSubstitute;
using Polly;

namespace Hagalaz.Services.GameWorld.Tests;

// The instruments are process-wide, so concurrent tests would contaminate the listener.
[DoNotParallelize]
public sealed partial class AuthenticationSignInTests
{
    private const string MeterName = "Hagalaz.Services.GameWorld";
    private const string SignInCounter = "hagalaz.gameworld.signin";
    private const string SignInDurationHistogram = "hagalaz.gameworld.signin.duration";

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenSuccessful_RecordsOneSuccessWithDuration()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.AddSession(42, "connection")
            .Returns(Task.FromResult<(IGameSession Session, bool Created)>((Substitute.For<IGameSession>(), Created: true)));
        var service = CreateAuthenticationService(gameSessionService);
        using var spy = new MeterListenerSpy(MeterName);

        var result = await service.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsTrue(result.Succeeded);
        AssertSingleSignIn(spy, "lobby", "success");
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenSessionOwnershipIsTaken_RecordsDuplicate()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.AddSession(42, "connection")
            .Returns(Task.FromResult<(IGameSession Session, bool Created)>((Substitute.For<IGameSession>(), Created: false)));
        var service = CreateAuthenticationService(gameSessionService, revokeTokenRequestClient: CreateSuccessfulRevokeClient());
        using var spy = new MeterListenerSpy(MeterName);

        await service.SignInLobbyAsync(CreateSignInRequest());

        AssertSingleSignIn(spy, "lobby", "duplicate");
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenCharacterLimitIsReached_RecordsFullOnce()
    {
        var characterService = Substitute.For<ICharacterService>();
        characterService.CountAsync().Returns(ValueTask.FromResult(2000));
        var service = CreateAuthenticationService(Substitute.For<IGameSessionService>(), characterService: characterService);
        using var spy = new MeterListenerSpy(MeterName);

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsTrue(result.IsFull);
        AssertSingleSignIn(spy, "world", "full");
    }

    [TestMethod]
    [Timeout(5000)]
    [DataRow(true, false, false, false, "rejected")]
    [DataRow(false, true, false, false, "rejected")]
    [DataRow(false, false, true, false, "rejected")]
    [DataRow(false, false, false, true, "duplicate")]
    [DataRow(false, false, false, false, "failed")]
    public async Task SignInWorldAsync_MapsRejectedSignInResultsOnce(
        bool disabled, bool invalidCredentials, bool lockedOut, bool authenticated, string expectedOutcome)
    {
        var service = CreateAuthenticationService(
            Substitute.For<IGameSessionService>(),
            signInResponseMessage: new SignInUserResponseMessage
            {
                Succeeded = false,
                IsDisabled = disabled,
                AreCredentialsInvalid = invalidCredentials,
                IsLockedOut = lockedOut,
                IsAuthenticated = authenticated
            });
        using var spy = new MeterListenerSpy(MeterName);

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        AssertSingleSignIn(spy, "world", expectedOutcome);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenSignInThrows_RecordsBoundedErrorAndRethrows()
    {
        var userInfoClient = Substitute.For<IRequestClient<GetUserInfoRequestMessage>>();
        userInfoClient
            .GetResponse<GetUserInfoResponseMessage>(Arg.Any<GetUserInfoRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(Task.FromException<Response<GetUserInfoResponseMessage>>(new InvalidOperationException("userinfo unavailable")));
        var service = CreateAuthenticationService(
            Substitute.For<IGameSessionService>(),
            userInfoRequestClient: userInfoClient,
            revokeTokenRequestClient: CreateSuccessfulRevokeClient());
        using var spy = new MeterListenerSpy(MeterName);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.SignInLobbyAsync(CreateSignInRequest()).AsTask());

        AssertSingleSignIn(spy, "lobby", "error");
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenCancelled_RecordsCancelled()
    {
        var userInfoClient = Substitute.For<IRequestClient<GetUserInfoRequestMessage>>();
        userInfoClient
            .GetResponse<GetUserInfoResponseMessage>(Arg.Any<GetUserInfoRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(Task.FromException<Response<GetUserInfoResponseMessage>>(new OperationCanceledException()));
        var service = CreateAuthenticationService(
            Substitute.For<IGameSessionService>(),
            userInfoRequestClient: userInfoClient,
            revokeTokenRequestClient: CreateSuccessfulRevokeClient());
        using var spy = new MeterListenerSpy(MeterName);

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.SignInWorldAsync(CreateSignInRequest()).AsTask());

        AssertSingleSignIn(spy, "world", "cancelled");
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenRateLimited_DoesNotRecordSignInOutcome()
    {
        using var limiter = new System.Threading.RateLimiting.ConcurrencyLimiter(
            new System.Threading.RateLimiting.ConcurrencyLimiterOptions { PermitLimit = 1, QueueLimit = 0 });
        using var exhaustedPermit = limiter.AttemptAcquire();
        var service = CreateAuthenticationService(
            Substitute.For<IGameSessionService>(),
            authLoginPipeline: new ResiliencePipelineBuilder().AddRateLimiter(limiter).Build());
        using var spy = new MeterListenerSpy(MeterName);

        await Assert.ThrowsExactlyAsync<Polly.RateLimiting.RateLimiterRejectedException>(
            () => service.SignInLobbyAsync(CreateSignInRequest()).AsTask());

        Assert.AreEqual(0, spy.Measurements.Count(m => m.InstrumentName is SignInCounter or SignInDurationHistogram));
    }

    private static void AssertSingleSignIn(MeterListenerSpy spy, string flow, string outcome)
    {
        var counter = spy.Measurements.Where(m => m.InstrumentName == SignInCounter).ToArray();
        var duration = spy.Measurements.Where(m => m.InstrumentName == SignInDurationHistogram).ToArray();
        Assert.AreEqual(1, counter.Length);
        Assert.AreEqual(1, duration.Length);
        Assert.AreEqual(1d, counter[0].Value);
        Assert.AreEqual("{signin}", counter[0].Unit);
        Assert.AreEqual("s", duration[0].Unit);
        Assert.IsTrue(duration[0].Value >= 0);
        foreach (var measurement in new[] { counter[0], duration[0] })
        {
            Assert.AreEqual(2, measurement.Tags.Count);
            Assert.AreEqual(flow, measurement.Tags["flow"]);
            Assert.AreEqual(outcome, measurement.Tags["outcome"]);
        }
    }
}
