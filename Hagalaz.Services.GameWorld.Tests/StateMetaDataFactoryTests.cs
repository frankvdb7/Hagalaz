using System;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Providers;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class StateMetaDataFactoryTests
{
    [TestMethod]
    public async Task GetStates_RejectsPersistentStateWithoutStableMetadata()
    {
        var services = new ServiceCollection();
        services.AddTransient<MissingMetadataState>();
        var descriptorProvider = Substitute.For<IServiceDescriptorProvider>();
        descriptorProvider.GetServiceDescriptors().Returns(services);
        var factory = new StateMetaDataFactory(descriptorProvider);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in factory.GetStates())
            {
            }
        });

        StringAssert.Contains(exception.Message, nameof(MissingMetadataState));
        StringAssert.Contains(exception.Message, nameof(StateMetaDataAttribute));
    }

    private sealed class MissingMetadataState : State, IPersistentState
    {
    }
}
