using System;
using Hagalaz.Game.Messages;
using Hagalaz.Services.GameWorld.Services;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldRegistrationStoreTests
{
    [TestMethod]
    public void ObserveOnline_DifferentLiveInstancesReportConflict()
    {
        var store = new WorldRegistrationStore();
        var now = DateTimeOffset.UtcNow;

        store.ObserveOnline(CreateMessage("instance-a", 1, now.AddMinutes(-1), now.AddSeconds(30)), now);
        var update = store.ObserveOnline(CreateMessage("instance-b", 2, now, now.AddSeconds(30)), now);

        Assert.IsTrue(update.HasConflict);
        Assert.IsFalse(update.IsAvailable);
        Assert.IsTrue(store.HasConflict(1, "instance-a", now));
    }

    [TestMethod]
    public void ObserveOffline_OlderGenerationDoesNotRemoveReplacement()
    {
        var store = new WorldRegistrationStore();
        var now = DateTimeOffset.UtcNow;

        store.ObserveOnline(CreateMessage("instance-a", 1, now.AddMinutes(-1), now.AddSeconds(30)), now);
        store.ObserveOnline(CreateMessage("instance-b", 2, now, now.AddSeconds(30)), now);

        var update = store.ObserveOffline(new WorldOfflineMessage(1, "instance-a", 1), now);

        Assert.IsTrue(update.IsAvailable);
        Assert.AreEqual("instance-b", update.ActiveMessage!.InstanceId);
    }

    [TestMethod]
    public void Expire_RemovesMissedRenewal()
    {
        var store = new WorldRegistrationStore();
        var now = DateTimeOffset.UtcNow;
        store.ObserveOnline(CreateMessage("instance-a", 1, now, now.AddSeconds(1)), now);

        var updates = store.Expire(now.AddSeconds(2));

        Assert.AreEqual(1, updates.Count);
        Assert.IsFalse(updates[0].IsAvailable);
        Assert.IsFalse(store.IsLocalGenerationAvailable(1, "instance-a", now.AddSeconds(2)));
    }

    private static WorldOnlineMessage CreateMessage(string instanceId, long generation, DateTimeOffset startedAt, DateTimeOffset leaseExpiresAt) => new()
    {
        Id = 1,
        Name = "World 1",
        IpAddress = "127.0.0.1",
        Port = 43594,
        CharacterCount = 0,
        Settings = new WorldOnlineMessage.WorldSettings
        {
            IsMembersOnly = true,
            IsQuickChatEnabled = false,
            IsPvP = false,
            IsLootShareEnabled = false,
            IsHighLighted = false
        },
        Location = new WorldOnlineMessage.WorldLocation { Name = "Local", Flag = 0 },
        InstanceId = instanceId,
        Generation = generation,
        StartedAt = startedAt,
        LeaseExpiresAt = leaseExpiresAt
    };
}
