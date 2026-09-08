using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Areas.Lumbridge.Npcs;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Providers;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<INpcScriptTypeCatalog>();
        Assert.IsTrue(catalog.ScriptTypes.All(type => type.Assembly == typeof(Startup).Assembly));

        var validationServices = new ServiceCollection();
        validationServices.AddSingleton<INpcScriptTypeCatalog>(catalog);
        validationServices.AddSingleton<IServiceDescriptorProvider>(
            new ServiceDescriptorProvider(validationServices));
        validationServices.AddScoped<INpcScriptFactory, NpcScriptMetaDataFactory>();
        using var validationProvider = validationServices.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var validationScope = validationProvider.CreateScope();
        var factory = validationScope.ServiceProvider.GetRequiredService<INpcScriptFactory>();
        var scripts = new List<(int npcId, Type scriptType)>();

        await foreach (var script in factory.GetScripts())
        {
            scripts.Add(script);
        }

        Assert.IsTrue(scripts.All(script => script.scriptType.Assembly == typeof(Startup).Assembly));
        Assert.IsTrue(scripts.Contains((705, typeof(MeleeInstructor))));
    }

    [TestMethod]
    public async Task MetadataFactory_DeduplicatesTypesAcrossCatalogs()
    {
        var services = new ServiceCollection();
        var catalog = new NpcScriptTypeCatalog([typeof(MeleeInstructor), typeof(MeleeInstructor)]);
        var factory = new NpcScriptMetaDataFactory(new ServiceDescriptorProvider(services), [catalog, catalog]);

        var scripts = new List<(int npcId, Type scriptType)>();
        await foreach (var script in factory.GetScripts())
        {
            scripts.Add(script);
        }

        Assert.AreEqual(1, scripts.Count(script => script == (705, typeof(MeleeInstructor))));
    }

    [TestMethod]
    public void Catalog_RetainsLoadableTypesFromPartiallyLoadableAssembly()
    {
        var catalog = NpcScriptTypeCatalog.FromAssembly(new PartiallyLoadableAssembly());

        Assert.IsTrue(catalog.ScriptTypes.Contains(typeof(MeleeInstructor)));
    }

    private sealed class PartiallyLoadableAssembly : Assembly
    {
        public override Type[] GetTypes() => throw new ReflectionTypeLoadException(
            [typeof(MeleeInstructor), null!],
            [new TypeLoadException("test type could not be loaded")]);
    }
}
