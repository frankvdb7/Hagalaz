using System.Net;
using System.Net.Http.Json;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
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
        var store = new MemoryFileStore();
        var objectCodec = new ObjectTypeCodec();
        var referenceTableCodec = new ReferenceTableCodec();
        store.Set(255, 16, EncodeReferenceTable(referenceTableCodec, memberCount: 2, mapObjectToLastMember: true));
        store.Set(16, 1, EncodeContainer(EncodeArchive(
            EncodeObject(objectCodec, 258, "initial-0"),
            EncodeObject(objectCodec, 259, "initial-1"))));

        var services = new ServiceCollection();
        services.AddSingleton<IFileStore>(store);
        services.AddGameCache();
        using var serviceProvider = services.BuildServiceProvider();
        var cacheApi = serviceProvider.GetRequiredService<ICacheAPI>();

        using var firstEntered = new ManualResetEventSlim();
        using var releaseFirst = new ManualResetEventSlim();
        using var secondAttempted = new ManualResetEventSlim();
        using var secondEntered = new ManualResetEventSlim();

        var firstMutation = Task.Run(() => cacheApi.UpdateArchiveMember(16, 1, 2, (archive, memberIndex) =>
        {
            var entries = archive.Entries ?? throw new InvalidOperationException("Archive entries are not available.");
            entries[memberIndex] = new MemoryStream(EncodeObject(objectCodec, 258, "updated-0"));
            firstEntered.Set();
            if (!releaseFirst.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException("The first archive mutation was not released.");
            }

            return TypeMutationUtilities.BuildSingleChunkArchive(entries);
        }));

        Task secondMutation = Task.CompletedTask;
        try
        {
            Assert.IsTrue(firstEntered.Wait(TimeSpan.FromSeconds(5)), "The first mutation should enter the archive update callback.");

            secondMutation = Task.Run(() =>
            {
                secondAttempted.Set();
                cacheApi.UpdateArchiveMember(16, 1, 3, (archive, memberIndex) =>
                {
                    secondEntered.Set();
                    var entries = archive.Entries ?? throw new InvalidOperationException("Archive entries are not available.");
                    entries[memberIndex] = new MemoryStream(EncodeObject(objectCodec, 259, "updated-1"));
                    return TypeMutationUtilities.BuildSingleChunkArchive(entries);
                });
            });

            Assert.IsTrue(secondAttempted.Wait(TimeSpan.FromSeconds(5)), "The second mutation should start.");
            Assert.IsFalse(secondEntered.Wait(TimeSpan.FromMilliseconds(200)), "A same-archive mutation must wait for the current update to finish.");
        }
        finally
        {
            releaseFirst.Set();
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
        var initialArchive = EncodeContainer(EncodeArchive(
            EncodeObject(objectCodec, 258, "sibling"),
            EncodeObject(objectCodec, 259, "initial")));
        store.Set(255, 16, EncodeReferenceTable(referenceTableCodec, memberCount: 2, mapObjectToLastMember: true));
        store.Set(16, 1, initialArchive);

        await using var app = await CacheServiceTestHost.StartAsync(store, cacheGroup => cacheGroup.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        Assert.AreEqual("initial", await ReadObjectNameAsync(client, 259));

        using var response = await client.PostAsJsonAsync(
            "/api/v1/cache/types/objects/259/name",
            new TypeMutationEndpoints.SetObjectNameRequest("updated"));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());

        Assert.IsFalse(initialArchive.SequenceEqual(store.GetBytes(16, 1)), "The updated archive should be persisted to the file store.");
        Assert.AreEqual("updated", await ReadObjectNameAsync(client, 259));
        Assert.AreEqual("sibling", await ReadObjectNameAsync(client, 258));
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

        var copiedReferenceTable = EncodeReferenceTable(referenceTableCodec, memberCount: 2, mapObjectToLastMember: false);
        store.Set(255, 17, copiedReferenceTable);
        var referenceTableCopyResponse = await client.PostAsJsonAsync(
            "/api/v1/cache/store/indexes/255/files/16/actions/copy-raw",
            new StoreMutationEndpoints.CopyRawFileRequest(255, 17));
        Assert.AreEqual(HttpStatusCode.OK, referenceTableCopyResponse.StatusCode, await referenceTableCopyResponse.Content.ReadAsStringAsync());
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

    private static async Task<string> ReadObjectNameAsync(HttpClient client, int id = 259)
    {
        var response = await client.GetFromJsonAsync<TypeEndpoints.ObjectTypeResponse>($"/api/v1/cache/types/objects/{id}");
        Assert.IsNotNull(response);
        return response.Name;
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

    private static byte[] EncodeContainer(byte[] data)
    {
        using var container = new Container(new MemoryStream(data));
        return container.Encode();
    }

}
