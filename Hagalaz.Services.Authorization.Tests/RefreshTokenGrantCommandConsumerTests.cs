using System.Security.Claims;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Authorization.Mediator.Commands;
using Hagalaz.Services.Authorization.Mediator.Consumers;
using Hagalaz.Services.Authorization.Model;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

namespace Hagalaz.Services.Authorization.Tests;

[TestClass]
public sealed class RefreshTokenGrantCommandConsumerTests
{
    [TestMethod]
    public async Task Consume_KeepsOriginalScopesAndResourcesOnRefreshedPrincipal()
    {
        var refreshPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(OpenIddictConstants.Claims.Subject, "1")], "refresh"));
        refreshPrincipal.SetScopes("cache_api", OpenIddictConstants.Scopes.Roles);
        refreshPrincipal.SetResources("cache-service-1", "hagalaz-web-admin");

        var authentication = new Mock<IAuthenticationService>();
        authentication
            .Setup(service => service.AuthenticateAsync(It.IsAny<HttpContext>(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(refreshPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)));
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(authentication.Object).BuildServiceProvider()
        };
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction { Request = new OpenIddictRequest() }
        });

        var user = new Character();
        var signIn = new Mock<SignInManager<Character>>(
            new Mock<UserManager<Character>>(Mock.Of<IUserStore<Character>>(), null!, null!, null!, null!, null!, null!, null!, null!).Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<Character>>(),
            null!, null!, null!, null!);
        signIn.Setup(manager => manager.ValidateSecurityStampAsync(refreshPrincipal)).ReturnsAsync(user);
        signIn.Setup(manager => manager.CanSignInAsync(user)).ReturnsAsync(true);
        signIn.Setup(manager => manager.CreateUserPrincipalAsync(user)).ReturnsAsync(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(OpenIddictConstants.Claims.Role, "SystemAdministrator")], "user")));

        RefreshTokenGrantResult? result = null;
        var context = new Mock<ConsumeContext<RefreshTokenGrantCommand>>();
        context.SetupGet(consume => consume.Message).Returns(new RefreshTokenGrantCommand());
        context.Setup(consume => consume.RespondAsync(It.IsAny<RefreshTokenGrantResult>()))
            .Callback<RefreshTokenGrantResult>(response => result = response)
            .Returns(Task.CompletedTask);

        await new RefreshTokenGrantCommandConsumer(Mock.Of<IHttpContextAccessor>(accessor => accessor.HttpContext == httpContext), signIn.Object)
            .Consume(context.Object);

        Assert.IsNotNull(result?.User);
        CollectionAssert.AreEquivalent(new[] { "cache_api", OpenIddictConstants.Scopes.Roles }, result.User.GetScopes().ToArray());
        CollectionAssert.AreEquivalent(new[] { "cache-service-1", "hagalaz-web-admin" }, result.User.GetResources().ToArray());
        Assert.IsTrue(result.User.FindFirst(OpenIddictConstants.Claims.Role)!.GetDestinations().Contains(OpenIddictConstants.Destinations.AccessToken));
    }
}
