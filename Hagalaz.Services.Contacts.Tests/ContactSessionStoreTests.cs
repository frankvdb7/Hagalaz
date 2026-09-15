using Hagalaz.Services.Contacts.Store;
using Hagalaz.Services.Contacts.Store.Model;

namespace Hagalaz.Services.Contacts.Tests;

[TestClass]
public sealed class ContactSessionStoreTests
{
    [TestMethod]
    public void TrySetNewerSession_RejectsOlderOrEqualGeneration()
    {
        var store = new ContactSessionStore();
        var current = CreateSession(2, "current");

        Assert.IsTrue(store.TrySetNewerSession(current));
        Assert.IsFalse(store.TrySetNewerSession(CreateSession(1, "older")));
        Assert.IsFalse(store.TrySetNewerSession(CreateSession(2, "equal")));
        Assert.AreSame(current, store.GetOrDefault(current.MasterId));
    }

    [TestMethod]
    public void TryRemoveExact_RejectsStaleGenerationOrConnection()
    {
        var store = new ContactSessionStore();
        var current = CreateSession(3, "current");
        store.TrySetNewerSession(current);

        Assert.IsFalse(store.TryRemoveExact(CreateSession(2, "current")));
        Assert.IsFalse(store.TryRemoveExact(CreateSession(3, "stale-connection")));
        Assert.AreSame(current, store.GetOrDefault(current.MasterId));
    }

    [TestMethod]
    public async Task ConcurrentStaleAndNewerUpdatesKeepTheNewestGeneration()
    {
        var store = new ContactSessionStore();
        var initial = CreateSession(2, "initial");
        var stale = CreateSession(1, "stale");
        var newer = CreateSession(3, "newer");
        store.TrySetNewerSession(initial);

        await Task.WhenAll(
            Task.Run(() => store.TrySetNewerSession(stale)),
            Task.Run(() => store.TrySetNewerSession(newer)));

        Assert.AreSame(newer, store.GetOrDefault(initial.MasterId));
    }

    [TestMethod]
    public void RemoveSessionsForWorld_RemovesMatchingSessionsAtomicallyAndReturnsExactEntries()
    {
        var store = new ContactSessionStore();
        var first = CreateSession(1, "first", 1, 42);
        var otherWorld = CreateSession(1, "other-world", 2, 43);
        store.TrySetNewerSession(first);
        store.TrySetNewerSession(otherWorld);

        var removed = store.RemoveSessionsForWorld(1);

        Assert.HasCount(1, removed);
        Assert.AreSame(first, removed[0]);
        Assert.IsNull(store.GetOrDefault(first.MasterId));
        Assert.AreSame(otherWorld, store.GetOrDefault(otherWorld.MasterId));
    }

    private static ContactSessionContext CreateSession(long generation, string connectionId, int worldId = 1, uint masterId = 42) =>
        new(masterId, worldId, "World", generation, connectionId);
}
