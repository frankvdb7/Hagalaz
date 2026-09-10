using System.Reflection;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Areas.Lumbridge.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hagalaz.Game.Scripts.Tests;

[TestClass]
public sealed class NpcScriptRegistrationTests
{
    [TestMethod]
    public async Task Configure_DoesNotRegisterOwnerAwareScripts_AndMetadataFactoryDiscoversPluginScripts()
    {
        var services = new ServiceCollection();
        new Startup().Configure(services);

        Assert.IsFalse(services.Any(descriptor =>
            descriptor.ImplementationType?.IsAssignableTo(typeof(INpcScript)) == true));

        using var provider = services.BuildServiceProvider();
        Assert.IsNull(provider.GetService<INpcScript>());

        var validationServices = new ServiceCollection();
        validationServices.AddSingleton<IServiceDescriptorProvider>(new ServiceDescriptorProvider(validationServices));
        validationServices.AddSingleton<Assembly>(typeof(Startup).Assembly);
        validationServices.AddLogging();
        validationServices.AddScoped<NpcScriptMetaDataFactory>();
        using var validationProvider = validationServices.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var validationScope = validationProvider.CreateScope();
        _ = validationScope.ServiceProvider.GetRequiredService<NpcScriptMetaDataFactory>();

        var scripts = await DiscoverScripts(typeof(Startup).Assembly);

        Assert.IsTrue(scripts.All(script => script.scriptType.Assembly == typeof(Startup).Assembly));
        Assert.IsTrue(scripts.Contains((705, typeof(MeleeInstructor))));
        Assert.IsTrue(scripts.All(script => script.scriptType.IsClass && !script.scriptType.IsAbstract));
        Assert.IsFalse(scripts.Any(script => script.scriptType == typeof(INpcScript)));
        Assert.IsFalse(scripts.Any(script => script.scriptType == typeof(NpcScriptBase)));
    }

    [TestMethod]
    public async Task MetadataFactory_DeduplicatesAssembliesAndTypes()
    {
        var scripts = await DiscoverScripts(typeof(Startup).Assembly, typeof(Startup).Assembly);

        Assert.AreEqual(1, scripts.Count(script => script == (705, typeof(MeleeInstructor))));
    }

    [TestMethod]
    public async Task MetadataFactory_UsesLoadableTypesFromPartiallyLoadableAssembly()
    {
        var scripts = await DiscoverScripts(new PartiallyLoadableAssembly());

        Assert.IsTrue(scripts.Contains((705, typeof(MeleeInstructor))));
    }

    [TestMethod]
    public async Task MetadataFactory_IgnoresScriptsWithoutNpcMetadata()
    {
        var scripts = await DiscoverScripts(typeof(DefaultNpcScript).Assembly);

        Assert.IsFalse(scripts.Any(script => script.scriptType == typeof(DefaultNpcScript)));
    }

    [TestMethod]
    public async Task MetadataFactory_ObservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var services = new ServiceCollection();
        services.AddSingleton<Assembly>(typeof(Startup).Assembly);
        var factory = new NpcScriptMetaDataFactory(
            new ServiceDescriptorProvider(services),
            NullLogger<NpcScriptMetaDataFactory>.Instance);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in factory.GetScripts(cancellation.Token))
            {
            }
        });
    }

    private static async Task<List<(int npcId, Type scriptType)>> DiscoverScripts(params Assembly[] assemblies)
    {
        var services = new ServiceCollection();
        foreach (var assembly in assemblies)
        {
            services.AddSingleton<Assembly>(assembly);
        }

        var factory = new NpcScriptMetaDataFactory(
            new ServiceDescriptorProvider(services),
            NullLogger<NpcScriptMetaDataFactory>.Instance);
        var scripts = new List<(int npcId, Type scriptType)>();

        await foreach (var script in factory.GetScripts())
        {
            scripts.Add(script);
        }

        return scripts;
    }

    private sealed class PartiallyLoadableAssembly : Assembly
    {
        public override Type[] GetTypes() => throw new ReflectionTypeLoadException(
            [typeof(MeleeInstructor), null!],
            [new TypeLoadException("test type could not be loaded")]);
    }
}
