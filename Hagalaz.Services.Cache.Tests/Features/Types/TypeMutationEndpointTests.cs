using System.IO;
using System.Net;
using System.Net.Http.Json;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types;
using Hagalaz.Services.Cache.Features;
using Hagalaz.Services.Cache.Features.Types;
using Hagalaz.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features.Types;

[TestClass]
public class TypeMutationEndpointTests
{
    private const int NpcIndex = 18;

    [TestMethod]
    public async Task Mutation_OfAMissingTypeReturnsNotFoundWithoutWriting()
    {
        var store = SeedNpc(new NpcType(0) { CombatLevel = 21 });
        var stored = Stored(store);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync("/api/v1/cache/types/npcs/1/combat-level", new { combatLevel = 40 });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, await response.Content.ReadAsStringAsync());
        CollectionAssert.AreEqual(stored, Stored(store));
    }

    [TestMethod]
    public async Task Mutation_OfAnUndecodableTypeReturnsUnprocessableWithoutWriting()
    {
        var store = SeedNpc(new NpcType(0) { CombatLevel = 21 });
        var stored = Stored(store);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints(),
            services => services.Replace(ServiceDescriptor.Transient<INpcTypeCodec, UndecodableNpcCodec>()));
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync("/api/v1/cache/types/npcs/0/combat-level", new { combatLevel = 40 });

        Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode, await response.Content.ReadAsStringAsync());
        CollectionAssert.AreEqual(stored, Stored(store));
    }

    [TestMethod]
    public void AddGameCache_RegistersAMutatorForEachEditableDefinitionType()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IFileStore>(new MemoryFileStore());
        services.AddGameCache();
        using var provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<TypeMutator<IItemType>>());
        Assert.IsNotNull(provider.GetRequiredService<TypeMutator<INpcType>>());
        Assert.IsNotNull(provider.GetRequiredService<TypeMutator<IObjectType>>());
        Assert.IsNotNull(provider.GetRequiredService<TypeMutator<ISpriteType>>());
        Assert.IsNotNull(provider.GetRequiredService<TypeMutator<IConfigDefinition>>());
        Assert.IsNotNull(provider.GetRequiredService<TypeMutator<IVarpBitDefinition>>());
    }

    [TestMethod]
    public void AddGameCache_RegistersTheDefinitionCodecs()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IFileStore>(new MemoryFileStore());
        services.AddGameCache();
        using var provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<ITypeCodec<IParticleEmitterType>>());
        Assert.IsNotNull(provider.GetRequiredService<ITypeCodec<IParticleEffectorType>>());
        Assert.IsNotNull(provider.GetRequiredService<ITypeCodec<IBillboardType>>());
        Assert.IsNotNull(provider.GetRequiredService<ITypeCodec<IRenderAnimationType>>());
        Assert.IsNotNull(provider.GetRequiredService<ITypeCodec<IMaterialTableType>>());
        Assert.IsNotNull(provider.GetRequiredService<ITypeCodec<IModelType>>());
        Assert.IsNotNull(provider.GetRequiredService<ITypeCodec<IAnimationSkeletonType>>());
        Assert.IsNotNull(provider.GetRequiredService<ITypeCodec<IAnimationFrameType>>());
    }

    private static byte[] Stored(MemoryFileStore store)
    {
        using var stream = store.Read(NpcIndex, 0);
        return stream.ToArray();
    }

    // NPC 0 is the only member of archive 0, so the archive's data is the member itself.
    private static MemoryFileStore SeedNpc(NpcType npc)
    {
        var store = new MemoryFileStore();
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 1);
        var entry = new ReferenceTableEntry(0);
        entry.InitializeEntries(1);
        entry.AddEntry(0, new ReferenceTableChildEntry(0));
        table.AddEntry(0, entry);
        using var encodedTable = new ReferenceTableCodec().Encode(table);
        using var reference = new Container(new MemoryStream(encodedTable.ToArray()));
        using var encodedNpc = new NpcTypeCodec().Encode(npc);
        using var data = new Container(new MemoryStream(encodedNpc.ToArray()));
        store.Set(255, NpcIndex, reference.Encode());
        store.Set(NpcIndex, 0, data.Encode());
        return store;
    }

    private sealed class UndecodableNpcCodec : INpcTypeCodec
    {
        public INpcType Decode(int id, MemoryStream stream) => throw new ArgumentOutOfRangeException(nameof(stream), "Unreadable test entry.");
        public MemoryStream Encode(INpcType instance) => new NpcTypeCodec().Encode(instance);
    }
}
