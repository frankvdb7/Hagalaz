using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Areas.Lumbridge.Npcs;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Game.Scripts.Tests;

[TestClass]
public sealed class NpcScriptRegistrationTests
{
    [TestMethod]
    public async Task Configure_DoesNotRegisterOwnerAwareScripts_AndMetadataFactoryDiscoversThem()
    {
        var services = new ServiceCollection();
        new Startup().Configure(services);

        Assert.IsFalse(services.Any(descriptor =>
            descriptor.ImplementationType?.IsAssignableTo(typeof(INpcScript)) == true));

        var factory = new NpcScriptMetaDataFactory(new ServiceDescriptorProvider(services));
        var scripts = new List<(int npcId, Type scriptType)>();

        await foreach (var script in factory.GetScripts())
        {
            scripts.Add(script);
        }

        Assert.IsTrue(scripts.Contains((705, typeof(MeleeInstructor))));
    }
}
