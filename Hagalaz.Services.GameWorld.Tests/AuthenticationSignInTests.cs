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
    public async Task SignInLobbyAsync_WhenUserInfoThrows_RevokesTheIssuedAuthorization()
    {
        var revokeClient = CreateSuccessfulRevokeClient();
        var userInfoClient = Substitute.For<IRequestClient<GetUserInfoRequestMessage>>();
        var failure = new InvalidOperationException("userinfo unavailable");
        userInfoClient
            .GetResponse<GetUserInfoResponseMessage>(Arg.Any<GetUserInfoRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(Task.FromException<Response<GetUserInfoResponseMessage>>(failure));
        var contextAccessor = CreateContextAccessor();
        var service = CreateAuthenticationService(
            Substitute.For<IGameSessionService>(),
            contextAccessor: contextAccessor,
            userInfoRequestClient: userInfoClient,
            revokeTokenRequestClient: revokeClient);

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.SignInLobbyAsync(CreateSignInRequest()).AsTask());

        Assert.AreSame(failure, actual);
        await revokeClient.Received(1).GetResponse<RevokeTokenResponseMessage>(
            Arg.Is<RevokeTokenRequestMessage>(message =>
                message.ClientId == Constants.OAuth.LobbyClientId &&
                message.Subject == "42" &&
                message.AuthorizationId == "authorization-id"),
            Arg.Is<CancellationToken>(token => !token.IsCancellationRequested),
            Arg.Any<RequestTimeout>());
        Assert.IsNull(contextAccessor.Context.Features.Get<IAuthenticationFeature>());
        Assert.IsNull(contextAccessor.Context.Features.Get<PendingAuthorizationCleanup>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenUserInfoThrowsAndRevocationFails_RetainsPendingAuthorizationCleanup()
    {
        var revokeClient = CreateFailingRevokeClient(new InvalidOperationException("authorization service unavailable"));
        var userInfoClient = Substitute.For<IRequestClient<GetUserInfoRequestMessage>>();
        userInfoClient
            .GetResponse<GetUserInfoResponseMessage>(Arg.Any<GetUserInfoRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(Task.FromException<Response<GetUserInfoResponseMessage>>(new InvalidOperationException("userinfo unavailable")));
        var contextAccessor = CreateContextAccessor();
        var service = CreateAuthenticationService(
            Substitute.For<IGameSessionService>(),
            contextAccessor: contextAccessor,
            userInfoRequestClient: userInfoClient,
            revokeTokenRequestClient: revokeClient);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.SignInLobbyAsync(CreateSignInRequest()).AsTask());

        var pending = contextAccessor.Context.Features.Get<PendingAuthorizationCleanup>();
        Assert.IsNotNull(pending);
        Assert.AreEqual(Constants.OAuth.LobbyClientId, pending.ClientId);
        Assert.AreEqual("42", pending.Subject);
        Assert.AreEqual("authorization-id", pending.AuthorizationId);
        Assert.IsNull(contextAccessor.Context.Features.Get<IAuthenticationFeature>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenUserInfoClaimsAreNull_RevokesTheIssuedAuthorization()
    {
        var revokeClient = CreateSuccessfulRevokeClient();
        var userInfoClient = Substitute.For<IRequestClient<GetUserInfoRequestMessage>>();
        var userInfoResponse = CreateResponse(new GetUserInfoResponseMessage { Succeeded = true });
        userInfoClient
            .GetResponse<GetUserInfoResponseMessage>(Arg.Any<GetUserInfoRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(Task.FromResult(userInfoResponse));
        var contextAccessor = CreateContextAccessor();
        var service = CreateAuthenticationService(
            Substitute.For<IGameSessionService>(),
            contextAccessor: contextAccessor,
            userInfoRequestClient: userInfoClient,
            revokeTokenRequestClient: revokeClient);

        var result = await service.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        await revokeClient.Received(1).GetResponse<RevokeTokenResponseMessage>(
            Arg.Is<RevokeTokenRequestMessage>(message => message.AuthorizationId == "authorization-id"),
            Arg.Is<CancellationToken>(token => !token.IsCancellationRequested),
            Arg.Any<RequestTimeout>());
        Assert.IsNull(contextAccessor.Context.Features.Get<PendingAuthorizationCleanup>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenUserInfoClaimsAreNullAndRevocationFails_RetainsPendingAuthorizationCleanup()
    {
        var revokeClient = CreateFailingRevokeClient(new InvalidOperationException("authorization service unavailable"));
        var contextAccessor = CreateContextAccessor();
        var service = CreateAuthenticationService(
            Substitute.For<IGameSessionService>(),
            contextAccessor: contextAccessor,
            userInfoRequestClient: CreateUserInfoClient(null),
            revokeTokenRequestClient: revokeClient);

        var result = await service.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(
            "authorization-id",
            contextAccessor.Context.Features.Get<PendingAuthorizationCleanup>()!.AuthorizationId);
        await revokeClient.Received(1).GetResponse<RevokeTokenResponseMessage>(
            Arg.Is<RevokeTokenRequestMessage>(message => message.AuthorizationId == "authorization-id"),
            Arg.Is<CancellationToken>(token => !token.IsCancellationRequested),
            Arg.Any<RequestTimeout>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenUserInfoSubjectIsMissing_RevokesTheIssuedAuthorization()
    {
        var revokeClient = CreateSuccessfulRevokeClient();
        var contextAccessor = CreateContextAccessor();
        var service = CreateAuthenticationService(
            Substitute.For<IGameSessionService>(),
            contextAccessor: contextAccessor,
            userInfoRequestClient: CreateUserInfoClient(new Dictionary<string, object>()),
            revokeTokenRequestClient: revokeClient);

        var result = await service.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        await revokeClient.Received(1).GetResponse<RevokeTokenResponseMessage>(
            Arg.Is<RevokeTokenRequestMessage>(message =>
                message.Subject == "42" && message.AuthorizationId == "authorization-id"),
            Arg.Is<CancellationToken>(token => !token.IsCancellationRequested),
            Arg.Any<RequestTimeout>());
        Assert.IsNull(contextAccessor.Context.Features.Get<IAuthenticationFeature>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenUserInfoSubjectIsMalformed_RevokesTheIssuedAuthorization()
    {
        var revokeClient = CreateSuccessfulRevokeClient();
        var gameSessionService = Substitute.For<IGameSessionService>();
        var service = CreateAuthenticationService(
            gameSessionService,
            userInfoRequestClient: CreateUserInfoClient(new Dictionary<string, object>
            {
                [Claims.Subject] = "not-a-number"
            }),
            revokeTokenRequestClient: revokeClient);

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        await revokeClient.Received(1).GetResponse<RevokeTokenResponseMessage>(
            Arg.Is<RevokeTokenRequestMessage>(message =>
                message.Subject == "42" && message.AuthorizationId == "authorization-id"),
            Arg.Is<CancellationToken>(token => !token.IsCancellationRequested),
            Arg.Any<RequestTimeout>());
        await gameSessionService.DidNotReceive().TryAddWorldSession(
            Arg.Any<uint>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenIssuedSubjectIsMalformedAndRevocationFails_RetainsIssuedSubjectInPendingCleanup()
    {
        var revokeClient = CreateFailingRevokeClient(new InvalidOperationException("authorization service unavailable"));
        var contextAccessor = CreateContextAccessor();
        var principalFactory = Substitute.For<IClaimsPrincipalFactory>();
        principalFactory.Create(Arg.Any<IDictionary<string, object>>())
            .Returns(new ClaimsPrincipal(new ClaimsIdentity()));
        var service = CreateAuthenticationService(
            Substitute.For<IGameSessionService>(),
            contextAccessor: contextAccessor,
            userInfoRequestClient: CreateUserInfoClient(new Dictionary<string, object>
            {
                [Claims.Subject] = "not-a-number"
            }),
            revokeTokenRequestClient: revokeClient,
            claimsPrincipalFactory: principalFactory,
            signInResponseMessage: new SignInUserResponseMessage
            {
                Succeeded = true,
                IdToken = "id-token",
                AccessToken = "access-token",
                Scope = "openid",
                ExpireDate = DateTimeOffset.UtcNow.AddMinutes(5),
                TokenType = "Bearer",
                AuthorizationId = "authorization-b",
                Subject = "subject-b"
            });

        var result = await service.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        var pending = contextAccessor.Context.Features.Get<PendingAuthorizationCleanup>();
        Assert.IsNotNull(pending);
        Assert.AreEqual("subject-b", pending.Subject);
        Assert.AreEqual("authorization-b", pending.AuthorizationId);
        await revokeClient.Received(1).GetResponse<RevokeTokenResponseMessage>(
            Arg.Is<RevokeTokenRequestMessage>(message =>
                message.Subject == "subject-b" && message.AuthorizationId == "authorization-b"),
            Arg.Is<CancellationToken>(token => !token.IsCancellationRequested),
            Arg.Any<RequestTimeout>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenPrincipalIsUnauthenticated_RevokesTheIssuedAuthorization()
    {
        var revokeClient = CreateSuccessfulRevokeClient();
        var principalFactory = Substitute.For<IClaimsPrincipalFactory>();
        principalFactory.Create(Arg.Any<IDictionary<string, object>>())
            .Returns(new ClaimsPrincipal(new ClaimsIdentity()));
        var contextAccessor = CreateContextAccessor();
        var service = CreateAuthenticationService(
            Substitute.For<IGameSessionService>(),
            contextAccessor: contextAccessor,
            revokeTokenRequestClient: revokeClient,
            claimsPrincipalFactory: principalFactory);

        var result = await service.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        await revokeClient.Received(1).GetResponse<RevokeTokenResponseMessage>(
            Arg.Is<RevokeTokenRequestMessage>(message => message.AuthorizationId == "authorization-id"),
            Arg.Is<CancellationToken>(token => !token.IsCancellationRequested),
            Arg.Any<RequestTimeout>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenPrincipalIsUnauthenticatedAndRevocationFails_RetainsPendingAuthorizationCleanup()
    {
        var revokeClient = CreateFailingRevokeClient(new InvalidOperationException("authorization service unavailable"));
        var contextAccessor = CreateContextAccessor();
        var principalFactory = Substitute.For<IClaimsPrincipalFactory>();
        principalFactory.Create(Arg.Any<IDictionary<string, object>>())
            .Returns(new ClaimsPrincipal(new ClaimsIdentity()));
        var service = CreateAuthenticationService(
            Substitute.For<IGameSessionService>(),
            contextAccessor: contextAccessor,
            revokeTokenRequestClient: revokeClient,
            claimsPrincipalFactory: principalFactory);

        var result = await service.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(
            "authorization-id",
            contextAccessor.Context.Features.Get<PendingAuthorizationCleanup>()!.AuthorizationId);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenCommittedAuthorizationExists_BlocksReplacementIssuance()
    {
        var revokeClient = Substitute.For<IRequestClient<RevokeTokenRequestMessage>>();
        var signInClient = Substitute.For<IRequestClient<SignInUserRequestMessage>>();
        var contextAccessor = CreateContextAccessor();
        var committedAuthentication = new AuthenticationFeature
        {
            AuthenticationProperties = new AuthenticationProperties
            {
                ClientId = "client-a",
                AuthorizationId = "authorization-a",
                Claims = new Dictionary<string, object> { [Claims.Subject] = "42" }
            }
        };
        contextAccessor.Context.Features.Set<IAuthenticationFeature>(committedAuthentication);
        var service = CreateAuthenticationService(
            Substitute.For<IGameSessionService>(),
            contextAccessor: contextAccessor,
            revokeTokenRequestClient: revokeClient,
            signInUserRequestClient: signInClient);

        var result = await service.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsTrue(result.IsAlreadyLoggedOn);
        Assert.AreSame(committedAuthentication, contextAccessor.Context.Features.Get<IAuthenticationFeature>());
        await signInClient.DidNotReceive().GetResponse<SignInUserResponseMessage>(
            Arg.Any<SignInUserRequestMessage>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<RequestTimeout>());
        await revokeClient.DidNotReceive().GetResponse<RevokeTokenResponseMessage>(
            Arg.Any<RevokeTokenRequestMessage>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<RequestTimeout>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenPendingAuthorizationCleanupFails_BlocksReplacementIssuance()
    {
        var contextAccessor = CreateContextAccessor();
        var pending = new PendingAuthorizationCleanup(
            Constants.OAuth.LobbyClientId,
            "subject-a",
            "authorization-a");
        contextAccessor.Context.Features.Set(pending);
        var revokeClient = CreateFailingRevokeClient(new InvalidOperationException("authorization service unavailable"));
        var signInClient = Substitute.For<IRequestClient<SignInUserRequestMessage>>();
        var service = CreateAuthenticationService(
            Substitute.For<IGameSessionService>(),
            contextAccessor: contextAccessor,
            revokeTokenRequestClient: revokeClient,
            signInUserRequestClient: signInClient);

        var result = await service.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsTrue(result.IsAlreadyLoggedOn);
        Assert.AreSame(pending, contextAccessor.Context.Features.Get<PendingAuthorizationCleanup>());
        await signInClient.DidNotReceive().GetResponse<SignInUserResponseMessage>(
            Arg.Any<SignInUserRequestMessage>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<RequestTimeout>());
        await revokeClient.Received(1).GetResponse<RevokeTokenResponseMessage>(
            Arg.Is<RevokeTokenRequestMessage>(message =>
                message.ClientId == Constants.OAuth.LobbyClientId &&
                message.Subject == "subject-a" &&
                message.AuthorizationId == "authorization-a"),
            Arg.Any<CancellationToken>(),
            Arg.Any<RequestTimeout>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenPendingAuthorizationCleanupSucceeds_AllowsReplacementIssuance()
    {
        var contextAccessor = CreateContextAccessor();
        contextAccessor.Context.Features.Set(new PendingAuthorizationCleanup(
            Constants.OAuth.LobbyClientId,
            "subject-a",
            "authorization-a"));
        var revokeClient = CreateSuccessfulRevokeClient();
        var signInClient = Substitute.For<IRequestClient<SignInUserRequestMessage>>();
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.AddSession(42, "connection")
            .Returns(Task.FromResult<(IGameSession Session, bool Created)>((Substitute.For<IGameSession>(), Created: true)));
        var service = CreateAuthenticationService(
            gameSessionService,
            contextAccessor: contextAccessor,
            revokeTokenRequestClient: revokeClient,
            signInUserRequestClient: signInClient,
            signInResponseMessage: new SignInUserResponseMessage
            {
                Succeeded = true,
                IdToken = "id-token-b",
                AccessToken = "access-token-b",
                Scope = "openid",
                ExpireDate = DateTimeOffset.UtcNow.AddMinutes(5),
                TokenType = "Bearer",
                AuthorizationId = "authorization-b",
                Subject = "42"
            });

        var result = await service.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsTrue(result.Succeeded);
        Assert.IsNull(contextAccessor.Context.Features.Get<PendingAuthorizationCleanup>());
        await revokeClient.Received(1).GetResponse<RevokeTokenResponseMessage>(
            Arg.Is<RevokeTokenRequestMessage>(message =>
                message.ClientId == Constants.OAuth.LobbyClientId &&
                message.Subject == "subject-a" &&
                message.AuthorizationId == "authorization-a"),
            Arg.Any<CancellationToken>(),
            Arg.Any<RequestTimeout>());
        await signInClient.Received(1).GetResponse<SignInUserResponseMessage>(
            Arg.Any<SignInUserRequestMessage>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<RequestTimeout>());
        Assert.AreEqual(
            "authorization-b",
            contextAccessor.Context.Features.Get<IAuthenticationFeature>()!.AuthenticationProperties.AuthorizationId);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignOutAsync_AfterPendingAuthorizationRevocationFailure_RetriesExactAuthorization()
    {
        var contextAccessor = CreateContextAccessor();
        var revokeFailure = new InvalidOperationException("authorization service unavailable");
        var successfulRevokeResponse = CreateResponse(new RevokeTokenResponseMessage { Succeeded = true });
        var revokeClient = Substitute.For<IRequestClient<RevokeTokenRequestMessage>>();
        var responses = new Queue<Task<Response<RevokeTokenResponseMessage>>>(new[]
        {
            Task.FromException<Response<RevokeTokenResponseMessage>>(revokeFailure),
            Task.FromResult(successfulRevokeResponse)
        });
        revokeClient
            .GetResponse<RevokeTokenResponseMessage>(
                Arg.Any<RevokeTokenRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(_ => responses.Dequeue());
        var userInfoClient = Substitute.For<IRequestClient<GetUserInfoRequestMessage>>();
        userInfoClient
            .GetResponse<GetUserInfoResponseMessage>(Arg.Any<GetUserInfoRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(Task.FromException<Response<GetUserInfoResponseMessage>>(new InvalidOperationException("userinfo unavailable")));
        var service = CreateAuthenticationService(
            Substitute.For<IGameSessionService>(),
            contextAccessor: contextAccessor,
            userInfoRequestClient: userInfoClient,
            revokeTokenRequestClient: revokeClient);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.SignInLobbyAsync(CreateSignInRequest()).AsTask());
        Assert.IsNotNull(contextAccessor.Context.Features.Get<PendingAuthorizationCleanup>());

        await service.SignOutAsync();

        await revokeClient.Received(2).GetResponse<RevokeTokenResponseMessage>(
            Arg.Is<RevokeTokenRequestMessage>(message =>
                message.ClientId == Constants.OAuth.LobbyClientId &&
                message.Subject == "42" &&
                message.AuthorizationId == "authorization-id"),
            Arg.Is<CancellationToken>(token => !token.IsCancellationRequested),
            Arg.Any<RequestTimeout>());
        Assert.IsNull(contextAccessor.Context.Features.Get<PendingAuthorizationCleanup>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenAuthenticationCommits_RetainsTheExactAuthorizationWithoutCleanup()
    {
        var revokeClient = CreateSuccessfulRevokeClient();
        var gameSessionService = Substitute.For<IGameSessionService>();
        var session = Substitute.For<IGameSession>();
        gameSessionService.AddSession(42, "connection")
            .Returns(Task.FromResult<(IGameSession Session, bool Created)>((session, Created: true)));
        var contextAccessor = CreateContextAccessor();
        var service = CreateAuthenticationService(
            gameSessionService,
            contextAccessor: contextAccessor,
            revokeTokenRequestClient: revokeClient);

        var result = await service.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(
            "authorization-id",
            contextAccessor.Context.Features.Get<IAuthenticationFeature>()!.AuthenticationProperties.AuthorizationId);
        Assert.IsNull(contextAccessor.Context.Features.Get<PendingAuthorizationCleanup>());
        await revokeClient.DidNotReceive().GetResponse<RevokeTokenResponseMessage>(
            Arg.Any<RevokeTokenRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenExactRevocationThrows_RetainsTheAuthorizationHandle()
    {
        var revokeFailure = new InvalidOperationException("authorization service unavailable");
        var revokeClient = CreateFailingRevokeClient(revokeFailure);
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.AddSession(42, "connection")
            .Returns(Task.FromResult<(IGameSession Session, bool Created)>((Substitute.For<IGameSession>(), Created: false)));
        var contextAccessor = CreateContextAccessor();
        var service = CreateAuthenticationService(
            gameSessionService,
            contextAccessor: contextAccessor,
            revokeTokenRequestClient: revokeClient);

        var result = await service.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsTrue(result.IsAlreadyLoggedOn);
        Assert.AreEqual(
            "authorization-id",
            contextAccessor.Context.Features.Get<IAuthenticationFeature>()!.AuthenticationProperties.AuthorizationId);
        await revokeClient.Received(1).GetResponse<RevokeTokenResponseMessage>(
            Arg.Is<RevokeTokenRequestMessage>(message => message.AuthorizationId == "authorization-id"),
            Arg.Any<CancellationToken>(),
            Arg.Any<RequestTimeout>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenExactRevocationReportsFailure_RetainsTheAuthorizationHandle()
    {
        var revokeClient = CreateUnsuccessfulRevokeClient();
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.AddSession(42, "connection")
            .Returns(Task.FromResult<(IGameSession Session, bool Created)>((Substitute.For<IGameSession>(), Created: false)));
        var contextAccessor = CreateContextAccessor();
        var service = CreateAuthenticationService(
            gameSessionService,
            contextAccessor: contextAccessor,
            revokeTokenRequestClient: revokeClient);

        var result = await service.SignInLobbyAsync(CreateSignInRequest());

        Assert.IsTrue(result.IsAlreadyLoggedOn);
        Assert.AreEqual(
            "authorization-id",
            contextAccessor.Context.Features.Get<IAuthenticationFeature>()!.AuthenticationProperties.AuthorizationId);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenPostAuthenticationSetupFails_RevokesOnlyTheNewAuthorization()
    {
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.TryAddWorldSession(42, "connection")
            .Returns(Task.FromResult<(IGameSession? Session, bool Created)>((session, Created: true)));
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));
        var hydrationService = Substitute.For<ICharacterHydrationService>();
        hydrationService.HydrateAsync(Arg.Any<ICharacter>(), Arg.Any<CharacterModel>())
            .Returns(Task.FromResult(false));
        var revokeClient = CreateSuccessfulRevokeClient();

        var service = CreateAuthenticationService(
            gameSessionService,
            characterHydrationService: hydrationService,
            revokeTokenRequestClient: revokeClient);

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        await revokeClient.Received(1).GetResponse<RevokeTokenResponseMessage>(
            Arg.Is<RevokeTokenRequestMessage>(message =>
                message.ClientId == Constants.OAuth.WorldClientId &&
                message.Subject == "42" &&
                message.AuthorizationId == "authorization-id"),
            Arg.Is<CancellationToken>(token => !token.IsCancellationRequested),
            Arg.Any<RequestTimeout>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenPostAuthenticationRevocationFails_RetainsTheAuthorizationHandle()
    {
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        var gameSessionService = Substitute.For<IGameSessionService>();
        gameSessionService.TryAddWorldSession(42, "connection", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(IGameSession? Session, bool Created)>((session, Created: true)));
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));
        var hydrationService = Substitute.For<ICharacterHydrationService>();
        hydrationService.HydrateAsync(Arg.Any<ICharacter>(), Arg.Any<CharacterModel>())
            .Returns(Task.FromResult(false));
        var revokeFailure = new InvalidOperationException("authorization service unavailable");
        var revokeClient = CreateFailingRevokeClient(revokeFailure);
        var contextAccessor = CreateContextAccessor();
        var service = CreateAuthenticationService(
            gameSessionService,
            characterHydrationService: hydrationService,
            contextAccessor: contextAccessor,
            revokeTokenRequestClient: revokeClient);

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(
            "authorization-id",
            contextAccessor.Context.Features.Get<IAuthenticationFeature>()!.AuthenticationProperties.AuthorizationId);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInLobbyAsync_WhenUserInfoCancellationOccurs_CleansUpWithNonCanceledToken()
    {
        var revokeClient = CreateSuccessfulRevokeClient();
        var userInfoClient = Substitute.For<IRequestClient<GetUserInfoRequestMessage>>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        userInfoClient
            .GetResponse<GetUserInfoResponseMessage>(Arg.Any<GetUserInfoRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(Task.FromException<Response<GetUserInfoResponseMessage>>(
                new OperationCanceledException(cancellation.Token)));
        var service = CreateAuthenticationService(
            Substitute.For<IGameSessionService>(),
            userInfoRequestClient: userInfoClient,
            revokeTokenRequestClient: revokeClient);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => service.SignInLobbyAsync(CreateSignInRequest()).AsTask());

        await revokeClient.Received(1).GetResponse<RevokeTokenResponseMessage>(
            Arg.Any<RevokeTokenRequestMessage>(),
            Arg.Is<CancellationToken>(token => !token.IsCancellationRequested),
            Arg.Any<RequestTimeout>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task AuthenticateWorldReconnectAsync_ReturnsExistingSubjectWithoutCreatingWorldState()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var service = CreateAuthenticationService(gameSessionService, reconnectAuthenticated: true);

        var result = await service.AuthenticateWorldReconnectAsync(CreateReconnectAuthenticationRequest());

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(42u, result.MasterId);
        await gameSessionService.DidNotReceive().TryAddWorldSession(
            Arg.Any<uint>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task AuthenticateWorldReconnectAsync_UsesExplicitConnectionMetadataWithoutAmbientRaidoContext()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var validateClient = Substitute.For<IRequestClient<ValidateExistingAuthenticationRequestMessage>>();
        var validateResponse = CreateResponse(new ValidateExistingAuthenticationResponseMessage
        {
            Succeeded = true,
            Subject = "42"
        });
        validateClient
            .GetResponse<ValidateExistingAuthenticationResponseMessage>(
                Arg.Any<ValidateExistingAuthenticationRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .ReturnsForAnyArgs(Task.FromResult(validateResponse));
        var contextAccessor = Substitute.For<IRaidoCallerContextAccessor>();
        contextAccessor.Context.Returns(_ => throw new InvalidOperationException("Ambient Raido context must not be used."));
        var service = CreateAuthenticationService(
            gameSessionService,
            validateAuthenticationRequestClient: validateClient,
            contextAccessor: contextAccessor);
        var request = new WorldReconnectAuthenticationRequest(
            "login",
            "password",
            IPAddress.Parse("203.0.113.7"),
            "physical-reconnect");

        var result = await service.AuthenticateWorldReconnectAsync(request);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(42u, result.MasterId);
        await validateClient.Received(1).GetResponse<ValidateExistingAuthenticationResponseMessage>(
            Arg.Is<ValidateExistingAuthenticationRequestMessage>(message =>
                message!.RemoteIpAddress == "203.0.113.7" &&
                message.Login == "login" &&
                message.Password == "password"),
            Arg.Any<CancellationToken>(),
            Arg.Any<RequestTimeout>());
        await gameSessionService.DidNotReceive().TryAddWorldSession(
            Arg.Any<uint>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task AuthenticateWorldReconnectAsync_WhenRemoteAddressIsMissingLeavesRemoteIpAddressNull()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var validateClient = Substitute.For<IRequestClient<ValidateExistingAuthenticationRequestMessage>>();
        var validateResponse = CreateResponse(new ValidateExistingAuthenticationResponseMessage
        {
            Succeeded = true,
            Subject = "42"
        });
        validateClient
            .GetResponse<ValidateExistingAuthenticationResponseMessage>(
                Arg.Any<ValidateExistingAuthenticationRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .ReturnsForAnyArgs(Task.FromResult(validateResponse));
        var contextAccessor = Substitute.For<IRaidoCallerContextAccessor>();
        contextAccessor.Context.Returns(_ => throw new InvalidOperationException("Ambient Raido context must not be used."));
        var service = CreateAuthenticationService(
            gameSessionService,
            validateAuthenticationRequestClient: validateClient,
            contextAccessor: contextAccessor);

        var result = await service.AuthenticateWorldReconnectAsync(
            new WorldReconnectAuthenticationRequest("login", "password", null, "physical-reconnect"));

        Assert.IsTrue(result.Succeeded);
        await validateClient.Received(1).GetResponse<ValidateExistingAuthenticationResponseMessage>(
            Arg.Is<ValidateExistingAuthenticationRequestMessage>(message =>
                message!.RemoteIpAddress == null &&
                message.Login == "login" &&
                message.Password == "password"),
            Arg.Any<CancellationToken>(),
            Arg.Any<RequestTimeout>());
    }

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
    public async Task SignInWorldAsync_WhenCharacterHydrationFails_DestroysCreatedCharacter()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        gameSessionService.TryAddWorldSession(42, "connection")
            .Returns(Task.FromResult<(IGameSession? Session, bool Created)>((session, Created: true)));
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));
        var character = Substitute.For<ICharacter>();
        var hydrationService = Substitute.For<ICharacterHydrationService>();
        hydrationService.HydrateAsync(character, Arg.Any<CharacterModel>()).Returns(Task.FromResult(false));

        var service = CreateAuthenticationService(
            gameSessionService,
            characterHydrationService: hydrationService,
            characterFactory: CreateCharacterFactory(character));

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        character.Received(1).Destroy();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenCharacterHydrationThrows_DestroysCreatedCharacter()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        gameSessionService.TryAddWorldSession(42, "connection")
            .Returns(Task.FromResult<(IGameSession? Session, bool Created)>((session, Created: true)));
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));
        var character = Substitute.For<ICharacter>();
        var hydrationFailure = new InvalidOperationException("hydration failed");
        var hydrationService = Substitute.For<ICharacterHydrationService>();
        hydrationService.HydrateAsync(character, Arg.Any<CharacterModel>())
            .Returns(Task.FromException<bool>(hydrationFailure));

        var service = CreateAuthenticationService(
            gameSessionService,
            characterHydrationService: hydrationService,
            characterFactory: CreateCharacterFactory(character));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.SignInWorldAsync(CreateSignInRequest()).AsTask());

        Assert.AreSame(hydrationFailure, exception);
        character.Received(1).Destroy();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenCharacterRegistrationFails_DoesNotInitializePersistence()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        gameSessionService.TryAddWorldSession(42, "connection").Returns(Task.FromResult<(IGameSession? Session, bool Created)>((session, Created: true)));
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));

        var characterService = new TestCharacterService(addResult: false);
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        var character = Substitute.For<ICharacter>();

        var service = CreateAuthenticationService(
            gameSessionService,
            characterService: characterService,
            characterPersistenceService: persistenceService,
            snapshotRevision: 27,
            characterFactory: CreateCharacterFactory(character));

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        await gameSessionService.Received(1).RemoveSession(session);
        persistenceService.Received(1).InitializeRevision(42, 27);
        persistenceService.DidNotReceive().Forget(Arg.Any<CharacterPersistenceReceipt>());
        character.Received(1).Destroy();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenCharacterRegistrationFails_DoesNotProbeForAnotherCharacter()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        gameSessionService.TryAddWorldSession(42, "connection").Returns(Task.FromResult<(IGameSession? Session, bool Created)>((session, Created: true)));
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));

        var characterService = new TestCharacterService(addResult: false);
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        var service = CreateAuthenticationService(
            gameSessionService,
            characterService: characterService,
            characterPersistenceService: persistenceService,
            snapshotRevision: 27);

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(0, characterService.FindByMasterIdCallCount);
        persistenceService.Received(1).InitializeRevision(42, 27);
        persistenceService.DidNotReceive().Forget(Arg.Any<CharacterPersistenceReceipt>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenWorldSessionCommitFailsAndCharacterRemovalFails_RetainsHydratedRevisionState()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        gameSessionService.TryAddWorldSession(42, "connection").Returns(Task.FromResult<(IGameSession? Session, bool Created)>((session, Created: true)));
        gameSessionService.CommitWorldSession(session).Returns(Task.FromResult(false));
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));

        var character = Substitute.For<ICharacter>();
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        var service = CreateAuthenticationService(
            gameSessionService,
            characterPersistenceService: persistenceService,
            snapshotRevision: 27,
            characterFactory: CreateCharacterFactory(character));

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        persistenceService.DidNotReceive().Forget(Arg.Any<CharacterPersistenceReceipt>());
        character.DidNotReceive().Destroy();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenWorldSessionCommitFailsAndCharacterRemovalThrows_DoesNotDestroyCharacter()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        gameSessionService.TryAddWorldSession(42, "connection")
            .Returns(Task.FromResult<(IGameSession? Session, bool Created)>((session, Created: true)));
        gameSessionService.CommitWorldSession(session).Returns(Task.FromResult(false));
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));

        var character = Substitute.For<ICharacter>();
        var characterService = new TestCharacterService(
            addResult: true,
            removeFailure: new InvalidOperationException("character removal failed"));
        var service = CreateAuthenticationService(
            gameSessionService,
            characterService,
            snapshotRevision: 27,
            characterFactory: CreateCharacterFactory(character));

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        character.DidNotReceive().Destroy();
        await gameSessionService.Received(1).RemoveSession(session);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenInitializationFails_ReleasesLocalOwnershipBeforeRemoteRevocation()
    {
        var order = new List<string>();
        var gameSessionService = Substitute.For<IGameSessionService>();
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        gameSessionService.TryAddWorldSession(42, "connection")
            .Returns(Task.FromResult<(IGameSession? Session, bool Created)>((session, Created: true)));
        gameSessionService.CommitWorldSession(session).Returns(Task.FromResult(false));
        gameSessionService.RemoveSession(session)
            .Returns(_ =>
            {
                order.Add("remove-session");
                return Task.FromResult(true);
            });
        gameSessionService.RemoveLocalSession(session)
            .Returns(_ =>
            {
                order.Add("remove-local-session");
                return Task.FromResult(true);
            });

        var character = Substitute.For<ICharacter>();
        character.When(item => item.Destroy()).Do(_ => order.Add("destroy"));
        var characterService = new TestCharacterService(
            addResult: true,
            removeResult: true,
            onRemove: () => order.Add("remove-character"));
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        var revokeStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completeRevocation = new TaskCompletionSource<Response<RevokeTokenResponseMessage>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var revokeResponse = CreateResponse(new RevokeTokenResponseMessage { Succeeded = true });
        var revokeClient = Substitute.For<IRequestClient<RevokeTokenRequestMessage>>();
        revokeClient
            .GetResponse<RevokeTokenResponseMessage>(
                Arg.Any<RevokeTokenRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(_ =>
            {
                order.Add("revoke");
                revokeStarted.TrySetResult(true);
                return completeRevocation.Task;
            });
        var service = CreateAuthenticationService(
            gameSessionService,
            characterService,
            characterPersistenceService: persistenceService,
            revokeTokenRequestClient: revokeClient,
            characterFactory: CreateCharacterFactory(character));

        var signInTask = service.SignInWorldAsync(CreateSignInRequest()).AsTask();
        await revokeStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        CollectionAssert.AreEqual(
            new[] { "remove-character", "destroy", "remove-session", "remove-local-session", "revoke" },
            order);
        Assert.IsFalse(signInTask.IsCompleted);

        completeRevocation.TrySetResult(revokeResponse);
        var result = await signInTask;

        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenWorldSessionCommitFailsAndCharacterRemovalSucceeds_PreservesRevisionState()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        gameSessionService.TryAddWorldSession(42, "connection").Returns(Task.FromResult<(IGameSession? Session, bool Created)>((session, Created: true)));
        gameSessionService.CommitWorldSession(session).Returns(Task.FromResult(false));
        gameSessionService.RemoveSession(session).Returns(Task.FromResult(true));

        var characterService = new TestCharacterService(addResult: true, removeResult: true);
        var character = Substitute.For<ICharacter>();
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        var service = CreateAuthenticationService(
            gameSessionService,
            characterService,
            characterPersistenceService: persistenceService,
            snapshotRevision: 27,
            characterFactory: CreateCharacterFactory(character));

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsFalse(result.Succeeded);
        persistenceService.DidNotReceive().Forget(Arg.Any<CharacterPersistenceReceipt>());
        character.Received(1).Destroy();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task SignInWorldAsync_WhenClaimReleaseReturnsFalse_RemovesLocalSessionForLaterLogin()
    {
        var claims = new ReleaseFalseClaimStore();
        var factory = Substitute.For<IGameSessionFactory>();
        var initialSession = CreateSession("connection-1", "claim-1");
        var laterSession = CreateSession("connection-2", "claim-2");
        factory.CreateWorld(42, "connection-1", Arg.Any<long>()).Returns(initialSession);
        factory.CreateWorld(42, "connection-2", Arg.Any<long>()).Returns(laterSession);
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
        factory.Create(42, "lobby-connection", Arg.Any<long>()).Returns(lobbySession);
        factory.CreateWorld(42, "world-connection", Arg.Any<long>()).Returns(worldSession);
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
        factory.Create(42, "lobby-connection", Arg.Any<long>()).Returns(lobbySession);
        factory.CreateWorld(42, "world-connection", Arg.Any<long>()).Returns(worldSession);
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
        factory.Create(42, "lobby-connection", Arg.Any<long>()).Returns(lobbySession);
        factory.CreateWorld(42, "world-connection", Arg.Any<long>()).Returns(worldSession);
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
        factory.CreateWorld(42, "world-connection", Arg.Any<long>()).Returns(worldSession);
        factory.Create(42, "lobby-connection", Arg.Any<long>()).Returns(lobbySession);
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
        var firstSession = CreateSession("connection-1", "claim-1");
        var secondSession = CreateSession("connection-2", "claim-2");
        factory.CreateWorld(42, "connection-1", Arg.Any<long>()).Returns(firstSession);
        factory.CreateWorld(42, "connection-2", Arg.Any<long>()).Returns(secondSession);
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var store = new GameSessionStore();
        var gameSessionService = GameSessionTestDependencies.CreateService(store, store, factory, claims, terminator);
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
    }

    [TestMethod]
    public async Task GameSessionService_AddSession_ReportsOwnershipOnlyForNewSession()
    {
        var session = Substitute.For<IGameSession>();
        var gameSessionFactory = Substitute.For<IGameSessionFactory>();
        gameSessionFactory.Create(42, "connection", Arg.Any<long>()).Returns(session);
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
    public async Task SignInWorldAsync_WhenCharacterRegistrationSucceeds_InitializesRevisionAfterLocalOwnership()
    {
        var gameSessionService = Substitute.For<IGameSessionService>();
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        gameSessionService.TryAddWorldSession(42, "connection").Returns(Task.FromResult<(IGameSession? Session, bool Created)>((session, Created: true)));
        gameSessionService.CommitWorldSession(session).Returns(Task.FromResult(true));
        var registrationOrder = new List<string>();
        var characterService = new TestCharacterService(addResult: true, onAdd: () => registrationOrder.Add("add"));
        var characterHydrationService = Substitute.For<ICharacterHydrationService>();
        characterHydrationService.HydrateAsync(Arg.Any<ICharacter>(), Arg.Any<CharacterModel>()).Returns(Task.FromResult(true));
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        persistenceService.When(service => service.InitializeRevision(42u, 27L))
            .Do(_ => registrationOrder.Add("initialize"));

        var service = CreateAuthenticationService(
            gameSessionService,
            characterService,
            characterHydrationService,
            characterPersistenceService: persistenceService,
            snapshotRevision: 27);

        var result = await service.SignInWorldAsync(CreateSignInRequest());

        Assert.IsTrue(result.Succeeded);
        await characterHydrationService.Received(1).HydrateAsync(Arg.Any<ICharacter>(), Arg.Any<CharacterModel>());
        persistenceService.Received(1).InitializeRevision(42u, 27L);
        CollectionAssert.AreEqual(new[] { "initialize", "add" }, registrationOrder);
        Assert.AreEqual(1, characterService.AddCallCount);
        await gameSessionService.DidNotReceive().RemoveSession(Arg.Any<IGameSession>());
    }

    private static AuthenticationService CreateAuthenticationService(
        IGameSessionService gameSessionService,
        ICharacterService? characterService = null,
        ICharacterHydrationService? characterHydrationService = null,
        IRequestClient<HydrateCharacter>? getCharacterRequestClient = null,
        string connectionId = "connection",
        long snapshotRevision = 0,
        ICharacterPersistenceService? characterPersistenceService = null,
        bool reconnectAuthenticated = false,
        IRequestClient<ValidateExistingAuthenticationRequestMessage>? validateAuthenticationRequestClient = null,
        IRaidoCallerContextAccessor? contextAccessor = null,
        IRequestClient<GetUserInfoRequestMessage>? userInfoRequestClient = null,
        IRequestClient<RevokeTokenRequestMessage>? revokeTokenRequestClient = null,
        IClaimsPrincipalFactory? claimsPrincipalFactory = null,
        ICharacterFactory? characterFactory = null,
        SignInUserResponseMessage? signInResponseMessage = null,
        IRequestClient<SignInUserRequestMessage>? signInUserRequestClient = null)
    {
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CharacterModel>(Arg.Any<CharacterHydrated>()).Returns(new CharacterModel { SnapshotRevision = snapshotRevision });
        mapper.Map<HydratedClaims>(Arg.Any<AuthenticationProperties>()).Returns(new HydratedClaims());

        var signInResponse = CreateResponse(signInResponseMessage ?? new SignInUserResponseMessage
        {
            Succeeded = true,
            IsAuthenticated = false,
            IdToken = "id-token",
            AccessToken = "access-token",
            Scope = "openid",
            ExpireDate = DateTimeOffset.UtcNow.AddMinutes(5),
            TokenType = "Bearer",
            AuthorizationId = "authorization-id",
            Subject = "42"
        });
        signInUserRequestClient ??= Substitute.For<IRequestClient<SignInUserRequestMessage>>();
        signInUserRequestClient
            .GetResponse<SignInUserResponseMessage>(
                Arg.Any<SignInUserRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .ReturnsForAnyArgs(Task.FromResult(signInResponse));

        if (validateAuthenticationRequestClient is null)
        {
            validateAuthenticationRequestClient = Substitute.For<IRequestClient<ValidateExistingAuthenticationRequestMessage>>();
            var validateAuthenticationResponse = CreateResponse(new ValidateExistingAuthenticationResponseMessage
            {
                Succeeded = reconnectAuthenticated,
                Subject = reconnectAuthenticated ? "42" : null
            });
            validateAuthenticationRequestClient
                .GetResponse<ValidateExistingAuthenticationResponseMessage>(
                    Arg.Any<ValidateExistingAuthenticationRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
                .ReturnsForAnyArgs(Task.FromResult(validateAuthenticationResponse));
        }

        if (userInfoRequestClient is null)
        {
            var userInfoResponse = CreateResponse(new GetUserInfoResponseMessage
            {
                Succeeded = true,
                Claims = new Dictionary<string, object> { [Claims.Subject] = "42" }
            });
            userInfoRequestClient = Substitute.For<IRequestClient<GetUserInfoRequestMessage>>();
            userInfoRequestClient
                .GetResponse<GetUserInfoResponseMessage>(
                    Arg.Any<GetUserInfoRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
                .ReturnsForAnyArgs(Task.FromResult(userInfoResponse));
        }

        var characterResponse = CreateResponse<CharacterHydrated, CharacterNotFound>(CreateCharacterHydrated());
        var hydrateRequestClient = Substitute.For<IRequestClient<HydrateCharacter>>();
        hydrateRequestClient
            .GetResponse<CharacterHydrated, CharacterNotFound>(
                Arg.Any<HydrateCharacter>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .ReturnsForAnyArgs(Task.FromResult(characterResponse));

        if (claimsPrincipalFactory is null)
        {
            claimsPrincipalFactory = Substitute.For<IClaimsPrincipalFactory>();
            claimsPrincipalFactory.Create(Arg.Any<IDictionary<string, object>>())
                .Returns(new ClaimsPrincipal(new ClaimsIdentity("test")));
        }

        var characterFactorySubstitute = characterFactory ?? Substitute.For<ICharacterFactory>();
        if (characterFactory is null)
        {
            var defaultCharacter = Substitute.For<ICharacter>();
            characterFactorySubstitute.Create(Arg.Any<IGameSession>(), Arg.Any<IGameClient>()).Returns(defaultCharacter);
        }

        var characterServiceSubstitute = characterService ?? new TestCharacterService(addResult: true);

        var characterHydrationServiceSubstitute = characterHydrationService ?? Substitute.For<ICharacterHydrationService>();
        if (characterHydrationService == null)
        {
            characterHydrationServiceSubstitute.HydrateAsync(Arg.Any<ICharacter>(), Arg.Any<CharacterModel>())
                .Returns(Task.FromResult(true));
        }

        revokeTokenRequestClient ??= Substitute.For<IRequestClient<RevokeTokenRequestMessage>>();

        var persistenceService = characterPersistenceService ?? Substitute.For<ICharacterPersistenceService>();
        var worldSessionAdmissionService = new WorldSessionAdmissionService(
            NullLogger<WorldSessionAdmissionService>.Instance,
            mapper,
            characterServiceSubstitute,
            characterFactorySubstitute,
            characterHydrationServiceSubstitute,
            persistenceService,
            gameSessionService,
            getCharacterRequestClient ?? hydrateRequestClient);

        return new AuthenticationService(
            NullLogger<AuthenticationService>.Instance,
            characterServiceSubstitute,
            persistenceService,
            Substitute.For<ICharacterLogoutService>(),
            gameSessionService,
            worldSessionAdmissionService,
            signInUserRequestClient,
            validateAuthenticationRequestClient,
            userInfoRequestClient,
            revokeTokenRequestClient,
            claimsPrincipalFactory,
            contextAccessor ?? CreateContextAccessor(connectionId),
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

    private static ICharacterFactory CreateCharacterFactory(ICharacter character)
    {
        var factory = Substitute.For<ICharacterFactory>();
        factory.Create(Arg.Any<IGameSession>(), Arg.Any<IGameClient>()).Returns(character);
        return factory;
    }

    private static IRequestClient<RevokeTokenRequestMessage> CreateSuccessfulRevokeClient()
    {
        return CreateRevokeClient(Task.FromResult(CreateResponse(new RevokeTokenResponseMessage { Succeeded = true })));
    }

    private static IRequestClient<RevokeTokenRequestMessage> CreateFailingRevokeClient(Exception failure)
    {
        return CreateRevokeClient(Task.FromException<Response<RevokeTokenResponseMessage>>(failure));
    }

    private static IRequestClient<RevokeTokenRequestMessage> CreateUnsuccessfulRevokeClient()
    {
        return CreateRevokeClient(Task.FromResult(CreateResponse(new RevokeTokenResponseMessage
        {
            Succeeded = false,
            Error = "revoke failed"
        })));
    }

    private static IRequestClient<RevokeTokenRequestMessage> CreateRevokeClient(Task<Response<RevokeTokenResponseMessage>> response)
    {
        var client = Substitute.For<IRequestClient<RevokeTokenRequestMessage>>();
        client
            .GetResponse<RevokeTokenResponseMessage>(
                Arg.Any<RevokeTokenRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(response);
        return client;
    }

    private static IRequestClient<GetUserInfoRequestMessage> CreateUserInfoClient(IDictionary<string, object>? claims)
    {
        var client = Substitute.For<IRequestClient<GetUserInfoRequestMessage>>();
        var userInfoResponse = CreateResponse(new GetUserInfoResponseMessage
        {
            Succeeded = true,
            Claims = claims
        });
        client
            .GetResponse<GetUserInfoResponseMessage>(Arg.Any<GetUserInfoRequestMessage>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(Task.FromResult(userInfoResponse));
        return client;
    }

    private static WorldReconnectAuthenticationRequest CreateReconnectAuthenticationRequest() => new(
        "login",
        "password",
        IPAddress.Loopback,
        "connection");

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
        session.SessionGeneration.Returns(1L);
        return session;
    }

    private static IGameSession CreateLobbySession(string connectionId)
    {
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns(connectionId);
        session.MasterId.Returns(42u);
        session.SessionClaimId.Returns($"lobby-{connectionId}");
        session.SessionGeneration.Returns(1L);
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
        private readonly bool _removeResult;
        private readonly Action? _onAdd;
        private readonly Action? _onRemove;
        private readonly Exception? _removeFailure;
        private readonly ICharacter? _existingCharacter;

        public TestCharacterService(
            bool addResult,
            bool removeResult = false,
            Action? onAdd = null,
            ICharacter? existingCharacter = null,
            Action? onRemove = null,
            Exception? removeFailure = null)
        {
            _addResult = addResult;
            _removeResult = removeResult;
            _onAdd = onAdd;
            _onRemove = onRemove;
            _removeFailure = removeFailure;
            _existingCharacter = existingCharacter;
        }

        public int AddCallCount { get; private set; }

        public ValueTask<bool> AddAsync(ICharacter character)
        {
            AddCallCount++;
            _onAdd?.Invoke();
            return ValueTask.FromResult(_addResult);
        }

        public ValueTask<bool> RemoveAsync(ICharacter character)
        {
            _onRemove?.Invoke();
            if (_removeFailure is not null)
            {
                return ValueTask.FromException<bool>(_removeFailure);
            }

            return ValueTask.FromResult(_removeResult);
        }

        public ValueTask<int> CountAsync() => ValueTask.FromResult(0);

        public ValueTask<ICharacter?> FindByIndex(int index) => ValueTask.FromResult<ICharacter?>(null);

        public int FindByMasterIdCallCount { get; private set; }

        public ValueTask<ICharacter?> FindByMasterId(uint masterId)
        {
            FindByMasterIdCallCount++;
            return ValueTask.FromResult<ICharacter?>(_existingCharacter);
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
        public Task<long> AllocateSessionGenerationAsync(uint masterId, CancellationToken cancellationToken = default) => Task.FromResult(1L);
        public Task<bool> TryClaimAsync(uint masterId, string claimId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> ExecuteIfOwnerAndReplaceAsync(uint masterId, string ownerClaimId, string replacementClaimId, Func<CancellationToken, Task<bool>> action, CancellationToken cancellationToken = default) => action(cancellationToken);
        public Task<bool> ReleaseAsync(uint masterId, string claimId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> RenewAsync(uint masterId, string claimId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> ExecuteIfOwnerAsync(uint masterId, string claimId, Func<CancellationToken, Task<bool>> action, CancellationToken cancellationToken = default) => action(cancellationToken);
    }

    private sealed class ReleaseFalseClaimStore : IGameSessionClaimStore
    {
        private readonly object _sync = new();
        private readonly Dictionary<uint, string> _claims = new();

        public int TryClaimCount { get; private set; }

        public Task<long> AllocateSessionGenerationAsync(uint masterId, CancellationToken cancellationToken = default) => Task.FromResult(1L);

        public Task<bool> ExecuteIfOwnerAndReplaceAsync(uint masterId, string ownerClaimId, string replacementClaimId, Func<CancellationToken, Task<bool>> action, CancellationToken cancellationToken = default) => Task.FromResult(false);

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
                if (!_claims.TryGetValue(masterId, out var current) || current != claimId)
                {
                    return Task.FromResult(false);
                }

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

        public Task<long> AllocateSessionGenerationAsync(uint masterId, CancellationToken cancellationToken = default) => Task.FromResult(1L);

        public Task<bool> ExecuteIfOwnerAndReplaceAsync(uint masterId, string ownerClaimId, string replacementClaimId, Func<CancellationToken, Task<bool>> action, CancellationToken cancellationToken = default) => action(cancellationToken);

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
