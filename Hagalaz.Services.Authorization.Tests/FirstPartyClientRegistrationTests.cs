using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Services.Authorization.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OpenIddict.Abstractions;
using OpenIddictConstants = OpenIddict.Abstractions.OpenIddictConstants;
using HagalazScopes = Hagalaz.Authorization.Constants.Scopes;

namespace Hagalaz.Services.Authorization.Tests
{
    [TestClass]
    public sealed class FirstPartyClientRegistrationTests
    {
        [TestMethod]
        public async Task RegisterIfMissingAsync_WhenAllClientsAreMissing_CreatesClientsWithSeparatePermissions()
        {
            // Arrange
            var applicationManager = new Mock<IOpenIddictApplicationManager>();
            applicationManager
                .Setup(manager => manager.FindByClientIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<object?>(null));

            var createdDescriptors = new Dictionary<string, OpenIddictApplicationDescriptor>();
            applicationManager
                .Setup(manager => manager.CreateAsync(
                    It.IsAny<OpenIddictApplicationDescriptor>(),
                    It.IsAny<CancellationToken>()))
                .Callback<OpenIddictApplicationDescriptor, CancellationToken>((descriptor, _) =>
                    createdDescriptors.Add(descriptor.ClientId!, descriptor))
                .Returns(() => ValueTask.FromResult<object>(new object()));

            var registration = CreateRegistration(applicationManager.Object);

            // Act
            await registration.RegisterIfMissingAsync();

            // Assert
            Assert.AreEqual(3, createdDescriptors.Count);
            AssertClientDescriptor(createdDescriptors["storm-app"], "storm-app", HagalazScopes.CharactersApi);
            AssertClientDescriptor(
                createdDescriptors["storm-admin"],
                "storm-admin",
                HagalazScopes.CharactersApi,
                HagalazScopes.CacheApi);
            AssertCacheServiceClientDescriptor(createdDescriptors["cache-service-1"]);
        }

        [TestMethod]
        public async Task RegisterIfMissingAsync_WhenNormalClientHasCachePermission_RemovesOnlyThatPermission()
        {
            // Arrange
            var existingNormalClient = new object();
            var existingCacheServiceClient = new object();
            var applicationManager = new Mock<IOpenIddictApplicationManager>();
            applicationManager
                .Setup(manager => manager.FindByClientIdAsync("storm-app", It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<object?>(existingNormalClient));
            applicationManager
                .Setup(manager => manager.FindByClientIdAsync("storm-admin", It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<object?>(null));
            applicationManager
                .Setup(manager => manager.FindByClientIdAsync("cache-service-1", It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<object?>(existingCacheServiceClient));
            applicationManager
                .Setup(manager => manager.ValidateClientSecretAsync(
                    existingCacheServiceClient,
                    "cache-service-test-secret",
                    It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult(true));
            applicationManager
                .Setup(manager => manager.PopulateAsync(
                    It.IsAny<OpenIddictApplicationDescriptor>(),
                    existingNormalClient,
                    It.IsAny<CancellationToken>()))
                .Callback<OpenIddictApplicationDescriptor, object, CancellationToken>((descriptor, _, _) =>
                {
                    descriptor.ApplicationType = OpenIddictConstants.ApplicationTypes.Web;
                    descriptor.ClientId = "storm-app";
                    descriptor.ClientType = OpenIddictConstants.ClientTypes.Public;
                    descriptor.ClientSecret = "preserved-secret";
                    descriptor.DisplayName = "Existing normal client";
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + HagalazScopes.CharactersApi);
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + HagalazScopes.CacheApi);
                })
                .Returns(() => new ValueTask());

            OpenIddictApplicationDescriptor? updatedDescriptor = null;
            applicationManager
                .Setup(manager => manager.UpdateAsync(
                    existingNormalClient,
                    It.IsAny<OpenIddictApplicationDescriptor>(),
                    It.IsAny<CancellationToken>()))
                .Callback<object, OpenIddictApplicationDescriptor, CancellationToken>((_, descriptor, _) =>
                    updatedDescriptor = descriptor)
                .Returns(() => new ValueTask());
            applicationManager
                .Setup(manager => manager.CreateAsync(
                    It.IsAny<OpenIddictApplicationDescriptor>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<object>(new object()));

            var registration = CreateRegistration(applicationManager.Object);

            // Act
            await registration.RegisterIfMissingAsync();

            // Assert
            Assert.IsNotNull(updatedDescriptor);
            Assert.AreEqual("storm-app", updatedDescriptor.ClientId);
            Assert.AreEqual("Existing normal client", updatedDescriptor.DisplayName);
            Assert.AreEqual("preserved-secret", updatedDescriptor.ClientSecret);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    OpenIddictConstants.Permissions.Prefixes.Scope + HagalazScopes.CharactersApi
                },
                updatedDescriptor.Permissions.ToArray());
            applicationManager.Verify(
                manager => manager.UpdateAsync(
                    existingNormalClient,
                    It.IsAny<OpenIddictApplicationDescriptor>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            applicationManager.Verify(
                manager => manager.CreateAsync(
                    It.Is<OpenIddictApplicationDescriptor>(descriptor => descriptor.ClientId == "storm-admin"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task RegisterIfMissingAsync_WhenBothClientsExistWithoutScopeDrift_LeavesThemUnchanged()
        {
            // Arrange
            var existingNormalClient = new object();
            var existingAdminClient = new object();
            var existingCacheServiceClient = new object();
            var applicationManager = new Mock<IOpenIddictApplicationManager>();
            applicationManager
                .Setup(manager => manager.FindByClientIdAsync("storm-app", It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<object?>(existingNormalClient));
            applicationManager
                .Setup(manager => manager.FindByClientIdAsync("storm-admin", It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<object?>(existingAdminClient));
            applicationManager
                .Setup(manager => manager.FindByClientIdAsync("cache-service-1", It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<object?>(existingCacheServiceClient));
            applicationManager
                .Setup(manager => manager.ValidateClientSecretAsync(
                    existingCacheServiceClient,
                    "cache-service-test-secret",
                    It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult(true));
            applicationManager
                .Setup(manager => manager.PopulateAsync(
                    It.IsAny<OpenIddictApplicationDescriptor>(),
                    existingNormalClient,
                    It.IsAny<CancellationToken>()))
                .Callback<OpenIddictApplicationDescriptor, object, CancellationToken>((descriptor, _, _) =>
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + HagalazScopes.CharactersApi))
                .Returns(() => new ValueTask());

            var registration = CreateRegistration(applicationManager.Object);

            // Act
            await registration.RegisterIfMissingAsync();

            // Assert
            applicationManager.Verify(
                manager => manager.CreateAsync(
                    It.IsAny<OpenIddictApplicationDescriptor>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            applicationManager.Verify(
                manager => manager.UpdateAsync(
                    It.IsAny<object>(),
                    It.IsAny<OpenIddictApplicationDescriptor>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            applicationManager.Verify(
                manager => manager.PopulateAsync(
                    It.IsAny<OpenIddictApplicationDescriptor>(),
                    existingNormalClient,
                    It.IsAny<CancellationToken>()),
                Times.Once);
            applicationManager.Verify(
                manager => manager.FindByClientIdAsync("storm-admin", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task RegisterIfMissingAsync_WhenCacheClientSecretChanged_UpdatesSecretAndPreservesPermissions()
        {
            var existingNormalClient = new object();
            var existingAdminClient = new object();
            var existingCacheServiceClient = new object();
            var applicationManager = new Mock<IOpenIddictApplicationManager>();
            applicationManager
                .Setup(manager => manager.FindByClientIdAsync("storm-app", It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<object?>(existingNormalClient));
            applicationManager
                .Setup(manager => manager.FindByClientIdAsync("storm-admin", It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<object?>(existingAdminClient));
            applicationManager
                .Setup(manager => manager.FindByClientIdAsync("cache-service-1", It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<object?>(existingCacheServiceClient));
            applicationManager
                .Setup(manager => manager.ValidateClientSecretAsync(
                    existingCacheServiceClient,
                    "cache-service-test-secret",
                    It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult(false));
            applicationManager
                .Setup(manager => manager.PopulateAsync(
                    It.IsAny<OpenIddictApplicationDescriptor>(),
                    existingNormalClient,
                    It.IsAny<CancellationToken>()))
                .Callback<OpenIddictApplicationDescriptor, object, CancellationToken>((descriptor, _, _) =>
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + HagalazScopes.CharactersApi))
                .Returns(() => new ValueTask());
            applicationManager
                .Setup(manager => manager.PopulateAsync(
                    It.IsAny<OpenIddictApplicationDescriptor>(),
                    existingCacheServiceClient,
                    It.IsAny<CancellationToken>()))
                .Callback<OpenIddictApplicationDescriptor, object, CancellationToken>((descriptor, _, _) =>
                {
                    descriptor.ApplicationType = OpenIddictConstants.ApplicationTypes.Web;
                    descriptor.ClientId = "cache-service-1";
                    descriptor.ClientType = OpenIddictConstants.ClientTypes.Confidential;
                    descriptor.DisplayName = "Hagalaz Cache Service";
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
                })
                .Returns(() => new ValueTask());

            OpenIddictApplicationDescriptor? updatedDescriptor = null;
            applicationManager
                .Setup(manager => manager.UpdateAsync(
                    existingCacheServiceClient,
                    It.IsAny<OpenIddictApplicationDescriptor>(),
                    It.IsAny<CancellationToken>()))
                .Callback<object, OpenIddictApplicationDescriptor, CancellationToken>((_, descriptor, _) =>
                    updatedDescriptor = descriptor)
                .Returns(() => new ValueTask());

            var registration = CreateRegistration(applicationManager.Object);

            await registration.RegisterIfMissingAsync();

            Assert.IsNotNull(updatedDescriptor);
            Assert.AreEqual("cache-service-test-secret", updatedDescriptor.ClientSecret);
            Assert.AreEqual(OpenIddictConstants.ApplicationTypes.Web, updatedDescriptor.ApplicationType);
            Assert.AreEqual(OpenIddictConstants.ClientTypes.Confidential, updatedDescriptor.ClientType);
            CollectionAssert.AreEquivalent(
                new[] { OpenIddictConstants.Permissions.Endpoints.Introspection },
                updatedDescriptor.Permissions.ToArray());
            applicationManager.Verify(
                manager => manager.UpdateAsync(
                    existingCacheServiceClient,
                    It.IsAny<OpenIddictApplicationDescriptor>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            applicationManager.Verify(
                manager => manager.CreateAsync(
                    It.IsAny<OpenIddictApplicationDescriptor>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static void AssertClientDescriptor(
            OpenIddictApplicationDescriptor descriptor,
            string clientId,
            params string[] apiScopes)
        {
            Assert.AreEqual(OpenIddictConstants.ApplicationTypes.Web, descriptor.ApplicationType);
            Assert.AreEqual(clientId, descriptor.ClientId);
            Assert.AreEqual(OpenIddictConstants.ClientTypes.Public, descriptor.ClientType);
            Assert.IsNull(descriptor.ClientSecret);

            var expectedPermissions = new List<string>
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.GrantTypes.Password,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Email,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Profile,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Roles
            };

            foreach (var scope in apiScopes)
            {
                expectedPermissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
            }

            CollectionAssert.AreEquivalent(expectedPermissions, descriptor.Permissions.ToArray());
        }

        private static void AssertCacheServiceClientDescriptor(OpenIddictApplicationDescriptor descriptor)
        {
            Assert.AreEqual(OpenIddictConstants.ApplicationTypes.Web, descriptor.ApplicationType);
            Assert.AreEqual("cache-service-1", descriptor.ClientId);
            Assert.AreEqual(OpenIddictConstants.ClientTypes.Confidential, descriptor.ClientType);
            Assert.AreEqual("cache-service-test-secret", descriptor.ClientSecret);
            CollectionAssert.AreEquivalent(
                new[] { OpenIddictConstants.Permissions.Endpoints.Introspection },
                descriptor.Permissions.ToArray());
        }

        private static FirstPartyClientRegistration CreateRegistration(IOpenIddictApplicationManager applicationManager)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Security:Introspection:ClientSecret"] = "cache-service-test-secret"
                })
                .Build();

            return new FirstPartyClientRegistration(applicationManager, configuration);
        }
    }
}
