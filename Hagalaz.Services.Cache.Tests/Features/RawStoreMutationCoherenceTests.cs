using System.Net;
using System.Net.Http.Json;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types;
using Hagalaz.Services.Cache.Features;
using Hagalaz.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public sealed class RawStoreMutationCoherenceTests
{
    [TestMethod]
    public async Task ConcurrentArchiveMemberUpdates_PreserveChangesToDifferentMembers()
    {
        var memoryStore = new MemoryFileStore();
        var objectCodec = new ObjectTypeCodec();
        var referenceTableCodec = new ReferenceTableCodec();
        memoryStore.Set(255, 16, EncodeReferenceTable(referenceTableCodec, memberCount: 2, mapObjectToLastMember: true));
        memoryStore.Set(16, 1, EncodeContainer(EncodeArchive(
            EncodeObject(objectCodec, 258, "initial-0"),
            EncodeObject(objectCodec, 259, "initial-1"))));

        using var firstArchiveReadEntered = new ManualResetEventSlim();
        using var releaseFirstArchiveRead = new ManualResetEventSlim();
        var store = new BlockingArchiveReadFileStore(memoryStore, firstArchiveReadEntered, releaseFirstArchiveRead);
        var services = new ServiceCollection();
        services.AddSingleton<IFileStore>(store);
        services.AddGameCache();
        using var serviceProvider = services.BuildServiceProvider();
        var cacheApi = serviceProvider.GetRequiredService<ICacheAPI>();
        _ = cacheApi.ReadReferenceTable(16);

        using var secondAttempted = new ManualResetEventSlim();

        var firstMutation = Task.Run(() => cacheApi.ReplaceArchiveMember(16, 1, 2, EncodeObject(objectCodec, 258, "updated-0")));

        Task secondMutation = Task.CompletedTask;
        try
        {
            Assert.IsTrue(firstArchiveReadEntered.Wait(TimeSpan.FromSeconds(5)), "The first mutation should begin reading the archive.");

            secondMutation = Task.Run(() =>
            {
                secondAttempted.Set();
                cacheApi.ReplaceArchiveMember(16, 1, 3, EncodeObject(objectCodec, 259, "updated-1"));
            });

            Assert.IsTrue(secondAttempted.Wait(TimeSpan.FromSeconds(5)), "The second mutation should start.");
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            Assert.AreEqual(1, store.ArchiveReadAttempts, "A same-archive mutation must wait for the current archive read-modify-write to finish.");
        }
        finally
        {
            releaseFirstArchiveRead.Set();
            await Task.WhenAll(firstMutation, secondMutation);
        }

        using var firstMember = cacheApi.Read(16, 1, 0);
        using var secondMember = cacheApi.Read(16, 1, 1);
        Assert.AreEqual("updated-0", objectCodec.Decode(258, firstMember).Name);
        Assert.AreEqual("updated-1", objectCodec.Decode(259, secondMember).Name);
    }

    [TestMethod]
    public async Task ObjectNameMutation_PersistsReadableMultiMemberArchive()
    {
        var store = new MemoryFileStore();
        var objectCodec = new ObjectTypeCodec();
        var referenceTableCodec = new ReferenceTableCodec();
        var initialObject = EncodeGatewayObject("initial");
        var initialArchive = EncodeContainer(EncodeArchive(
            EncodeObject(objectCodec, 258, "sibling"),
            initialObject));
        store.Set(255, 16, EncodeReferenceTable(referenceTableCodec, memberCount: 2, mapObjectToLastMember: true));
        store.Set(16, 1, initialArchive);

        using (var originalStream = new MemoryStream(initialObject))
        {
            var original = (ObjectType)objectCodec.Decode(259, originalStream);
            using var encodedRoundTrip = objectCodec.Encode(original);
            using var roundTripStream = new MemoryStream(encodedRoundTrip.ToArray());
            AssertGatewayRawFields((ObjectType)objectCodec.Decode(259, roundTripStream));
        }

        await using var app = await CacheServiceTestHost.StartAsync(store, cacheGroup => cacheGroup.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        const string updatedName = "updated café";

        Assert.AreEqual("initial", await ReadObjectNameAsync(client, 259));

        using var response = await client.PostAsJsonAsync(
            "/api/v1/cache/types/objects/259/name",
            new TypeMutationEndpoints.SetObjectNameRequest(updatedName));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());

        Assert.IsFalse(initialArchive.SequenceEqual(store.GetBytes(16, 1)), "The updated archive should be persisted to the file store.");
        var cacheApi = app.Services.GetRequiredService<ICacheAPI>();
        using (var persistedMember = cacheApi.Read(16, 1, 1))
        {
            var persistedObject = (ObjectType)objectCodec.Decode(259, persistedMember);
            Assert.AreEqual(updatedName, persistedObject.Name);
            AssertGatewayRawFields(persistedObject);
        }

        Assert.AreEqual(updatedName, await ReadObjectNameAsync(client, 259));
        Assert.AreEqual("sibling", await ReadObjectNameAsync(client, 258));
    }

    [TestMethod]
    public async Task ObjectNameMutation_Revision742FixturePreservesDefinitionAndArchiveSibling()
    {
        var store = new MemoryFileStore();
        var objectCodec = new ObjectTypeCodec();
        var referenceTableCodec = new ReferenceTableCodec();
        // Captured from index 16, archive 0, member 1 in the revision-742 cache.
        var originalObjectBytes = Convert.FromHexString(
            "4100644200644300641E53656172636800180C8628061614151C16181518159C199A15A11D1C152521211529151801010A013C2A02437261746500BE00386700");
        using var originalObjectStream = new MemoryStream(originalObjectBytes);
        var originalObject = (ObjectType)objectCodec.Decode(1, originalObjectStream);
        Assert.AreEqual("Crate", originalObject.Name);
        Assert.AreEqual("Search", originalObject.Actions[0]);
        var initialArchive = EncodeContainer(EncodeArchive(EncodeObject(objectCodec, 0, "sibling"), originalObjectBytes));
        var initialReferenceTable = EncodeSingleArchiveReferenceTable(referenceTableCodec, archiveId: 0, memberCount: 2);
        store.Set(255, 16, initialReferenceTable);
        store.Set(16, 0, initialArchive);

        await using var app = await CacheServiceTestHost.StartAsync(store, cacheGroup => cacheGroup.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync(
            "/api/v1/cache/types/objects/1/name",
            new TypeMutationEndpoints.SetObjectNameRequest("Edited crate"));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.IsFalse(initialArchive.SequenceEqual(store.GetBytes(16, 0)));

        var cacheApi = app.Services.GetRequiredService<ICacheAPI>();
        using (var persistedMember = cacheApi.Read(16, 0, 1))
        {
            var persistedObject = (ObjectType)objectCodec.Decode(1, persistedMember);
            Assert.AreEqual("Edited crate", persistedObject.Name);
            CollectionAssert.AreEqual(originalObject.Actions, persistedObject.Actions);
            Assert.AreEqual(originalObject.Opcode190To195Payloads![0], persistedObject.Opcode190To195Payloads![0]);
        }

        using var siblingMember = cacheApi.Read(16, 0, 0);
        Assert.AreEqual("sibling", objectCodec.Decode(0, siblingMember).Name);
    }

    [TestMethod]
    public async Task ObjectNameMutation_RejectsInvalidSingleByteStringsWithoutWriting()
    {
        var store = CreateObjectMutationStore(out var initialArchive, out var initialReferenceTable);

        await using var app = await CacheServiceTestHost.StartAsync(store, cacheGroup => cacheGroup.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        foreach (var invalidName in new[] { "before\0after", "outside Latin-1 \u0100" })
        {
            using var response = await client.PostAsJsonAsync(
                "/api/v1/cache/types/objects/259/name",
                new TypeMutationEndpoints.SetObjectNameRequest(invalidName));

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, await response.Content.ReadAsStringAsync());
            CollectionAssert.AreEqual(initialArchive, store.GetBytes(16, 1));
            CollectionAssert.AreEqual(initialReferenceTable, store.GetBytes(255, 16));
        }
    }

    [TestMethod]
    public async Task ObjectNameMutation_DoesNotPersistWhenEncodedDefinitionCannotBeDecoded()
    {
        var store = CreateObjectMutationStore(out var initialArchive, out var initialReferenceTable);

        await using var app = await CacheServiceTestHost.StartAsync(
            store,
            cacheGroup => cacheGroup.MapTypeEndpoints(),
            services => services.AddSingleton<IObjectTypeCodec>(new InvalidEncodedObjectTypeCodec()));
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/cache/types/objects/259/name",
            new TypeMutationEndpoints.SetObjectNameRequest("updated"));

        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        CollectionAssert.AreEqual(initialArchive, store.GetBytes(16, 1));
        CollectionAssert.AreEqual(initialReferenceTable, store.GetBytes(255, 16));
    }

    [TestMethod]
    public async Task NpcCombatLevelMutation_PreservesRecognizedOpcodePayloads()
    {
        var store = new MemoryFileStore();
        var npcCodec = new NpcTypeCodec();
        var referenceTableCodec = new ReferenceTableCodec();
        var originalNpc = EncodeNpcWithRecognizedPayloads();
        store.Set(255, 18, EncodeNpcReferenceTable(referenceTableCodec));
        store.Set(18, 0, EncodeContainer(EncodeArchive(EncodeNpc("sibling"), originalNpc)));

        await using var app = await CacheServiceTestHost.StartAsync(store, cacheGroup => cacheGroup.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/cache/types/npcs/1/combat-level",
            new TypeMutationEndpoints.SetNpcCombatLevelRequest(77));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        var cacheApi = app.Services.GetRequiredService<ICacheAPI>();
        using var persistedMember = cacheApi.Read(18, 0, 1);
        var persistedNpc = (NpcType)npcCodec.Decode(1, persistedMember);
        Assert.AreEqual(77, persistedNpc.CombatLevel);
        Assert.AreEqual((ushort)0x1234, persistedNpc.Opcode44Payload);
        Assert.AreEqual((ushort)0xABCD, persistedNpc.Opcode45Payload);
        CollectionAssert.AreEqual(
            new ushort?[] { 1, 2, 3, ushort.MaxValue, 5, 6 },
            persistedNpc.Opcode170To175Payloads);
        Assert.IsTrue(persistedNpc.HasAnIntArrayArray1449);
        CollectionAssert.AreEqual(new[] { -1, 2, -3 }, persistedNpc.AnIntArrayArray1449[0]);
    }

    [TestMethod]
    public async Task NpcCombatLevelMutation_Revision742FixturePreservesDefinitionAndArchiveSibling()
    {
        var store = new MemoryFileStore();
        var npcCodec = new NpcTypeCodec();
        var referenceTableCodec = new ReferenceTableCodec();
        // Captured from index 18, archive 0, member 0 in the revision-742 cache.
        var originalNpcBytes = Convert.FromHexString(
            "7F05925F00000C011E54616C6B2D746F0098427579206361706573002805140D1A8E192B0619151C061919A3061315940613290200FF012400FB01060248616E7300010680009D897071707C80009DB2705F708A3C0180009A977702AA002C00");
        using var originalNpcStream = new MemoryStream(originalNpcBytes);
        var originalNpc = (NpcType)npcCodec.Decode(0, originalNpcStream);
        Assert.AreEqual((ushort)44, originalNpc.Opcode170To175Payloads![0]);
        var initialArchive = EncodeContainer(EncodeArchive(originalNpcBytes, EncodeNpc("sibling")));
        var initialReferenceTable = EncodeNpcReferenceTable(referenceTableCodec);
        store.Set(255, 18, initialReferenceTable);
        store.Set(18, 0, initialArchive);

        await using var app = await CacheServiceTestHost.StartAsync(store, cacheGroup => cacheGroup.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync(
            "/api/v1/cache/types/npcs/0/combat-level",
            new TypeMutationEndpoints.SetNpcCombatLevelRequest(77));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.IsFalse(initialArchive.SequenceEqual(store.GetBytes(18, 0)));

        var cacheApi = app.Services.GetRequiredService<ICacheAPI>();
        using (var persistedMember = cacheApi.Read(18, 0, 0))
        {
            var persistedNpc = (NpcType)npcCodec.Decode(0, persistedMember);
            Assert.AreEqual(77, persistedNpc.CombatLevel);
            Assert.AreEqual(originalNpc.Name, persistedNpc.Name);
            CollectionAssert.AreEqual(originalNpc.Actions, persistedNpc.Actions);
            CollectionAssert.AreEqual(originalNpc.Opcode170To175Payloads, persistedNpc.Opcode170To175Payloads);
        }

        using var siblingMember = cacheApi.Read(18, 0, 1);
        Assert.AreEqual("sibling", npcCodec.Decode(1, siblingMember).Name);
    }

    [TestMethod]
    public async Task NpcCombatLevelMutation_RejectsOutOfRangeValueWithoutChangingCacheFiles()
    {
        var store = new MemoryFileStore();
        var referenceTableCodec = new ReferenceTableCodec();
        var initialReferenceTable = EncodeNpcReferenceTable(referenceTableCodec);
        var initialArchive = EncodeContainer(EncodeArchive(EncodeNpc("sibling"), EncodeNpcWithRecognizedPayloads()));
        store.Set(255, 18, initialReferenceTable);
        store.Set(18, 0, initialArchive);

        await using var app = await CacheServiceTestHost.StartAsync(store, cacheGroup => cacheGroup.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/cache/types/npcs/1/combat-level",
            new TypeMutationEndpoints.SetNpcCombatLevelRequest(65536));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, await response.Content.ReadAsStringAsync());
        CollectionAssert.AreEqual(initialArchive, store.GetBytes(18, 0));
        CollectionAssert.AreEqual(initialReferenceTable, store.GetBytes(255, 18));
    }

    [TestMethod]
    public async Task NpcCombatLevelMutation_AllowsOmittedCombatLevel()
    {
        var store = new MemoryFileStore();
        var npcCodec = new NpcTypeCodec();
        var referenceTableCodec = new ReferenceTableCodec();
        using var originalNpc = npcCodec.Encode(new NpcType(1) { CombatLevel = 42 });
        store.Set(255, 18, EncodeNpcReferenceTable(referenceTableCodec));
        store.Set(18, 0, EncodeContainer(EncodeArchive(EncodeNpc("sibling"), originalNpc.ToArray())));

        await using var app = await CacheServiceTestHost.StartAsync(store, cacheGroup => cacheGroup.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/cache/types/npcs/1/combat-level",
            new TypeMutationEndpoints.SetNpcCombatLevelRequest(-1));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        var cacheApi = app.Services.GetRequiredService<ICacheAPI>();
        using var persistedMember = cacheApi.Read(18, 0, 1);
        var persistedNpc = (NpcType)npcCodec.Decode(1, persistedMember);
        Assert.AreEqual(-1, persistedNpc.CombatLevel);
    }

    [TestMethod]
    public async Task RawStoreMutations_RefreshObjectTypeReadsWithinSameServiceInstance()
    {
        var store = new MemoryFileStore();
        var objectCodec = new ObjectTypeCodec();
        var referenceTableCodec = new ReferenceTableCodec();
        var initialArchive = EncodeContainer(EncodeArchive(
            EncodeObject(objectCodec, "initial-0"),
            EncodeObject(objectCodec, "initial-1")));
        store.Set(255, 16, EncodeReferenceTable(referenceTableCodec, memberCount: 1, mapObjectToLastMember: false));
        store.Set(16, 1, initialArchive);
        store.Set(16, 2, EncodeContainer(EncodeArchive(
            EncodeObject(objectCodec, "copy-0"),
            EncodeObject(objectCodec, "copy-1"))));

        await using var app = await CacheServiceTestHost.StartAsync(store, cacheGroup =>
        {
            cacheGroup.MapStoreMutationEndpoints();
            cacheGroup.MapTypeEndpoints();
        });
        using var client = CacheServiceTestHost.CreateClient(app);

        Assert.AreEqual("initial-0", await ReadObjectNameAsync(client));

        var updatedReferenceTable = EncodeReferenceTable(referenceTableCodec, memberCount: 2, mapObjectToLastMember: true);
        await PostRawWriteAsync(client, "/api/v1/cache/store/indexes/255/files/16/actions/write-raw", updatedReferenceTable);
        Assert.AreEqual("initial-1", await ReadObjectNameAsync(client));

        var replacementArchive = EncodeContainer(EncodeArchive(
            EncodeObject(objectCodec, "raw-write-0"),
            EncodeObject(objectCodec, "raw-write-1")));
        await PostRawWriteAsync(client, "/api/v1/cache/store/indexes/16/files/1/actions/write-raw", replacementArchive);
        Assert.AreEqual("raw-write-1", await ReadObjectNameAsync(client));
        CollectionAssert.AreEqual(replacementArchive, store.GetBytes(16, 1));

        var copySourceArchive = EncodeContainer(EncodeArchive(
            EncodeObject(objectCodec, "copy-0"),
            EncodeObject(objectCodec, "copy-1")));
        var copyResponse = await client.PostAsJsonAsync(
            "/api/v1/cache/store/indexes/16/files/1/actions/copy-raw",
            new StoreMutationEndpoints.CopyRawFileRequest(16, 2));
        Assert.AreEqual(HttpStatusCode.OK, copyResponse.StatusCode, await copyResponse.Content.ReadAsStringAsync());

        Assert.AreEqual("copy-1", await ReadObjectNameAsync(client));
        CollectionAssert.AreEqual(copySourceArchive, store.GetBytes(16, 1));

        var copiedReferenceTable = EncodeReferenceTable(referenceTableCodec, memberCount: 1, mapObjectToLastMember: false);
        store.Set(255, 17, copiedReferenceTable);
        var referenceTableCopyResponse = await client.PostAsJsonAsync(
            "/api/v1/cache/store/indexes/255/files/16/actions/copy-raw",
            new StoreMutationEndpoints.CopyRawFileRequest(255, 17));
        Assert.AreEqual(HttpStatusCode.OK, referenceTableCopyResponse.StatusCode, await referenceTableCopyResponse.Content.ReadAsStringAsync());
        var cacheApi = app.Services.GetRequiredService<ICacheAPI>();
        Assert.IsNotNull(cacheApi.ReadReferenceTable(16).GetEntry(1, 3));
        Assert.AreEqual("copy-0", await ReadObjectNameAsync(client));
        CollectionAssert.AreEqual(copiedReferenceTable, store.GetBytes(255, 16));
    }

    [TestMethod]
    public async Task RawMutationEndpoints_RejectNegativeDestinationIds()
    {
        var store = new MemoryFileStore();
        store.Set(0, 0, [1]);

        await using var app = await CacheServiceTestHost.StartAsync(store, cacheGroup => cacheGroup.MapStoreMutationEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var writeRequest = new StoreMutationEndpoints.WriteRawFileRequest(Convert.ToBase64String([2]));
        var copyRequest = new StoreMutationEndpoints.CopyRawFileRequest(0, 0);

        foreach (var path in new[]
        {
            "/api/v1/cache/store/indexes/-1/files/0/actions/write-raw",
            "/api/v1/cache/store/indexes/0/files/-1/actions/write-raw"
        })
        {
            using var response = await client.PostAsJsonAsync(path, writeRequest);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        foreach (var path in new[]
        {
            "/api/v1/cache/store/indexes/-1/files/0/actions/copy-raw",
            "/api/v1/cache/store/indexes/0/files/-1/actions/copy-raw"
        })
        {
            using var response = await client.PostAsJsonAsync(path, copyRequest);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, await response.Content.ReadAsStringAsync());
        }
    }

    [TestMethod]
    public async Task ConfigDefinitionMutation_RejectsUnrepresentableValuesWithoutChangingCacheFiles()
    {
        var store = new MemoryFileStore();
        var configCodec = new ConfigDefinitionCodec();
        var referenceTableCodec = new ReferenceTableCodec();
        using var encodedConfig = configCodec.Encode(new ConfigDefinition(0) { DefaultValue = 123, ValueType = 'A' });
        var initialArchive = EncodeContainer(EncodeArchive(encodedConfig.ToArray()));
        var initialReferenceTable = EncodeConfigReferenceTable(referenceTableCodec);
        store.Set(255, 2, initialReferenceTable);
        store.Set(2, 16, initialArchive);

        await using var app = await CacheServiceTestHost.StartAsync(store, cacheGroup => cacheGroup.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        var initialDefinition = await client.GetFromJsonAsync<TypeEndpoints.ConfigDefinitionResponse>("/api/v1/cache/types/config-definitions/0");
        Assert.IsNotNull(initialDefinition);
        Assert.AreEqual(123, initialDefinition.DefaultValue);
        Assert.AreEqual('A', initialDefinition.ValueType);

        var invalidRequests = new[]
        {
            new TypeMutationEndpoints.UpdateConfigDefinitionRequest(-1, 'A'),
            new TypeMutationEndpoints.UpdateConfigDefinitionRequest(65536, 'A'),
            new TypeMutationEndpoints.UpdateConfigDefinitionRequest(123, '\x80'),
            new TypeMutationEndpoints.UpdateConfigDefinitionRequest(123, '\u0100')
        };

        foreach (var request in invalidRequests)
        {
            using var response = await client.PostAsJsonAsync("/api/v1/cache/types/config-definitions/0", request);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, await response.Content.ReadAsStringAsync());
            CollectionAssert.AreEqual(initialArchive, store.GetBytes(2, 16));
            CollectionAssert.AreEqual(initialReferenceTable, store.GetBytes(255, 2));
        }
    }

    [TestMethod]
    public async Task ConfigDefinitionMutation_PersistsCodecRoundTripAndPreservesArchiveSibling()
    {
        var store = new MemoryFileStore();
        var configCodec = new ConfigDefinitionCodec();
        var referenceTableCodec = new ReferenceTableCodec();
        using var sibling = configCodec.Encode(new ConfigDefinition(0) { DefaultValue = 123, ValueType = 'A' });
        using var originalTarget = configCodec.Encode(new ConfigDefinition(1) { DefaultValue = 456, ValueType = 'B' });
        var initialArchive = EncodeContainer(EncodeArchive(sibling.ToArray(), originalTarget.ToArray()));
        store.Set(255, 2, EncodeConfigReferenceTable(referenceTableCodec, memberCount: 2));
        store.Set(2, 16, initialArchive);

        await using var app = await CacheServiceTestHost.StartAsync(store, cacheGroup => cacheGroup.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync(
            "/api/v1/cache/types/config-definitions/1",
            new TypeMutationEndpoints.UpdateConfigDefinitionRequest(ushort.MaxValue, 'Z'));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.IsFalse(initialArchive.SequenceEqual(store.GetBytes(2, 16)));

        var cacheApi = app.Services.GetRequiredService<ICacheAPI>();
        using (var persistedTarget = cacheApi.Read(2, 16, 1))
        {
            var decoded = configCodec.Decode(1, persistedTarget);
            Assert.AreEqual((int)ushort.MaxValue, decoded.DefaultValue);
            Assert.AreEqual('Z', decoded.ValueType);
        }

        using var persistedSibling = cacheApi.Read(2, 16, 0);
        var unchangedSibling = configCodec.Decode(0, persistedSibling);
        Assert.AreEqual(123, unchangedSibling.DefaultValue);
        Assert.AreEqual('A', unchangedSibling.ValueType);
    }

    [TestMethod]
    public async Task VarpBitMutation_PersistsCodecRoundTripAndPreservesArchiveSibling()
    {
        var store = new MemoryFileStore();
        var codec = new VarpBitDefinitionCodec();
        var referenceTableCodec = new ReferenceTableCodec();
        using var sibling = codec.Encode(new VarpBitDefinition(0) { ConfigID = 123, BitOffset = 4, BitLength = 8 });
        using var originalTarget = codec.Encode(new VarpBitDefinition(1) { ConfigID = 456, BitOffset = 2, BitLength = 6 });
        var initialArchive = EncodeContainer(EncodeArchive(sibling.ToArray(), originalTarget.ToArray()));
        store.Set(255, 22, EncodeSingleArchiveReferenceTable(referenceTableCodec, archiveId: 0, memberCount: 2));
        store.Set(22, 0, initialArchive);

        await using var app = await CacheServiceTestHost.StartAsync(store, cacheGroup => cacheGroup.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync(
            "/api/v1/cache/types/varp-bits/1",
            new TypeMutationEndpoints.UpdateVarpBitDefinitionRequest(789, 9, 7));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.IsFalse(initialArchive.SequenceEqual(store.GetBytes(22, 0)));

        var cacheApi = app.Services.GetRequiredService<ICacheAPI>();
        using (var persistedTarget = cacheApi.Read(22, 0, 1))
        {
            var decoded = codec.Decode(1, persistedTarget);
            Assert.AreEqual((short)789, decoded.ConfigID);
            Assert.AreEqual((byte)7, decoded.BitOffset);
            Assert.AreEqual((byte)9, decoded.BitLength);
        }

        using var persistedSibling = cacheApi.Read(22, 0, 0);
        var unchangedSibling = codec.Decode(0, persistedSibling);
        Assert.AreEqual((short)123, unchangedSibling.ConfigID);
        Assert.AreEqual((byte)4, unchangedSibling.BitOffset);
        Assert.AreEqual((byte)8, unchangedSibling.BitLength);
    }

    private static async Task<string> ReadObjectNameAsync(HttpClient client, int id = 259)
    {
        using var httpResponse = await client.GetAsync($"/api/v1/cache/types/objects/{id}");
        var responseBody = await httpResponse.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, httpResponse.StatusCode, responseBody);
        var response = System.Text.Json.JsonSerializer.Deserialize<TypeEndpoints.ObjectTypeResponse>(
            responseBody,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.IsNotNull(response);
        return response.Name;
    }

    private static MemoryFileStore CreateObjectMutationStore(out byte[] initialArchive, out byte[] initialReferenceTable)
    {
        var store = new MemoryFileStore();
        var referenceTableCodec = new ReferenceTableCodec();
        initialArchive = EncodeContainer(EncodeArchive(
            EncodeObject(new ObjectTypeCodec(), 258, "sibling"),
            EncodeGatewayObject("initial")));
        initialReferenceTable = EncodeReferenceTable(referenceTableCodec, memberCount: 2, mapObjectToLastMember: true);
        store.Set(255, 16, initialReferenceTable);
        store.Set(16, 1, initialArchive);
        return store;
    }

    private static async Task PostRawWriteAsync(HttpClient client, string path, byte[] bytes)
    {
        var response = await client.PostAsJsonAsync(path, new StoreMutationEndpoints.WriteRawFileRequest(Convert.ToBase64String(bytes)));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private static byte[] EncodeObject(ObjectTypeCodec codec, int id, string name)
    {
        using var encoded = codec.Encode(new ObjectType(id) { Name = name });
        return encoded.ToArray();
    }

    private static byte[] EncodeObject(ObjectTypeCodec codec, string name) => EncodeObject(codec, 259, name);

    private static byte[] EncodeGatewayObject(string name)
    {
        using var stream = new MemoryStream();
        stream.WriteByte(44);
        stream.WriteShort(0x1234);
        stream.WriteByte(45);
        stream.WriteShort(0xABCD);
        for (var opcode = 190; opcode < 196; opcode++)
        {
            stream.WriteByte((byte)opcode);
            stream.WriteShort(opcode - 180);
        }
        stream.WriteByte(30);
        stream.WriteString("Entrée");
        stream.WriteByte(2);
        stream.WriteString(name);
        stream.WriteByte(74);
        stream.WriteByte(0);
        return stream.ToArray();
    }

    private static void AssertGatewayRawFields(ObjectType objectType)
    {
        Assert.AreEqual(-1, objectType.Interactable);
        Assert.AreEqual(-1, objectType.SupportItemsFlag);
        Assert.AreEqual(2, objectType.ClipType);
        Assert.IsTrue(objectType.Solid);
        Assert.IsTrue(objectType.Gateway);
        Assert.AreEqual("Entrée", objectType.Actions[0]);
        Assert.AreEqual((ushort)0x1234, objectType.Opcode44Payload);
        Assert.AreEqual((ushort)0xABCD, objectType.Opcode45Payload);
        CollectionAssert.AreEqual(new ushort?[] { 10, 11, 12, 13, 14, 15 }, objectType.Opcode190To195Payloads);
    }

    private static byte[] EncodeNpcWithRecognizedPayloads() =>
    [
        121, 1, 0, 0xFF, 2, 0xFD,
        44, 0x12, 0x34,
        45, 0xAB, 0xCD,
        170, 0, 1,
        171, 0, 2,
        172, 0, 3,
        173, 0xFF, 0xFF,
        174, 0, 5,
        175, 0, 6,
        0
    ];

    private static byte[] EncodeNpc(string name)
    {
        using var encoded = new MemoryStream();
        encoded.WriteByte(2);
        encoded.WriteString(name);
        encoded.WriteByte(0);
        return encoded.ToArray();
    }

    private sealed class InvalidEncodedObjectTypeCodec : IObjectTypeCodec
    {
        private readonly ObjectTypeCodec _decoder = new();

        public IObjectType Decode(int id, MemoryStream stream) => _decoder.Decode(id, stream);

        public MemoryStream Encode(IObjectType instance) => new([2]);
    }

    private static byte[] EncodeArchive(params byte[][] members)
    {
        using var archive = new MemoryStream();
        foreach (var member in members)
        {
            archive.Write(member);
        }

        var previousMemberLength = 0;
        foreach (var member in members)
        {
            archive.WriteInt(member.Length - previousMemberLength);
            previousMemberLength = member.Length;
        }

        archive.WriteByte(1);
        return archive.ToArray();
    }

    private static byte[] EncodeReferenceTable(ReferenceTableCodec codec, int memberCount, bool mapObjectToLastMember)
    {
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 2);

        var firstArchive = new ReferenceTableEntry(0);
        firstArchive.InitializeEntries(1);
        firstArchive.AddEntry(0, new ReferenceTableChildEntry(0));
        table.AddEntry(0, firstArchive);

        var objectArchive = new ReferenceTableEntry(1);
        objectArchive.InitializeEntries(memberCount);
        if (mapObjectToLastMember)
        {
            objectArchive.AddEntry(2, new ReferenceTableChildEntry(0));
            objectArchive.AddEntry(3, new ReferenceTableChildEntry(1));
        }
        else
        {
            objectArchive.AddEntry(3, new ReferenceTableChildEntry(0));
        }

        table.AddEntry(1, objectArchive);
        using var encoded = codec.Encode(table);
        return EncodeContainer(encoded.ToArray());
    }

    private static byte[] EncodeConfigReferenceTable(ReferenceTableCodec codec, int memberCount = 1) =>
        EncodeSingleArchiveReferenceTable(codec, archiveId: 16, memberCount);

    private static byte[] EncodeNpcReferenceTable(ReferenceTableCodec codec) =>
        EncodeSingleArchiveReferenceTable(codec, archiveId: 0, memberCount: 2);

    private static byte[] EncodeSingleArchiveReferenceTable(ReferenceTableCodec codec, int archiveId, int memberCount)
    {
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 1);
        var archive = new ReferenceTableEntry(archiveId);
        archive.InitializeEntries(memberCount);
        for (var memberIndex = 0; memberIndex < memberCount; memberIndex++)
        {
            archive.AddEntry(memberIndex, new ReferenceTableChildEntry(memberIndex));
        }
        table.AddEntry(archiveId, archive);
        using var encoded = codec.Encode(table);
        return EncodeContainer(encoded.ToArray());
    }

    private static byte[] EncodeContainer(byte[] data)
    {
        using var container = new Container(new MemoryStream(data));
        return container.Encode();
    }

    private sealed class BlockingArchiveReadFileStore(
        IFileStore inner,
        ManualResetEventSlim firstArchiveReadEntered,
        ManualResetEventSlim releaseFirstArchiveRead) : IFileStore
    {
        private int _archiveReadAttempts;

        public int IndexFileCount => inner.IndexFileCount;

        public int ArchiveReadAttempts => Volatile.Read(ref _archiveReadAttempts);

        public int GetFileCount(int indexId) => inner.GetFileCount(indexId);

        public MemoryStream Read(int indexId, int fileId)
        {
            if (indexId == 16 && fileId == 1)
            {
                if (Interlocked.Increment(ref _archiveReadAttempts) == 1)
                {
                    firstArchiveReadEntered.Set();
                    if (!releaseFirstArchiveRead.Wait(TimeSpan.FromSeconds(5)))
                    {
                        throw new TimeoutException("The first archive read was not released.");
                    }
                }
            }

            return inner.Read(indexId, fileId);
        }

        public void Write(int indexId, int fileId, MemoryStream data) => inner.Write(indexId, fileId, data);

        public void Dispose() => inner.Dispose();
    }

}
