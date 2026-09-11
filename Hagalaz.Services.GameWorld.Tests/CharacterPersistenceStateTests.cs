using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
using Hagalaz.Services.GameWorld.Services;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterPersistenceStateTests
{
    [TestMethod]
    public void Acknowledge_RequiresExactCorrelationAndRevision()
    {
        var state = new CharacterPersistenceState();
        var receipt = CreateReceipt(Guid.NewGuid(), 7);
        state.MarkPending(42, "fingerprint", receipt);

        state.Acknowledge(42, Guid.NewGuid(), 7, CharacterPersistenceOutcome.Committed);
        Assert.IsFalse(state.IsPersisted(42, "fingerprint"));
        state.Acknowledge(42, receipt.CorrelationId, 6, CharacterPersistenceOutcome.Committed);
        Assert.IsFalse(state.IsPersisted(42, "fingerprint"));

        state.Acknowledge(42, receipt.CorrelationId, receipt.SnapshotRevision, CharacterPersistenceOutcome.Committed);
        Assert.IsTrue(state.IsPersisted(42, "fingerprint"));
    }

    [TestMethod]
    public void Acknowledge_ConflictLeavesExactPendingSnapshotForRetry()
    {
        var state = new CharacterPersistenceState();
        var receipt = CreateReceipt(Guid.NewGuid(), 7);
        state.MarkPending(42, "fingerprint", receipt);

        state.Acknowledge(42, receipt.CorrelationId, receipt.SnapshotRevision, CharacterPersistenceOutcome.Conflict);

        Assert.IsFalse(state.IsPersisted(42, "fingerprint"));
        state.Acknowledge(42, receipt.CorrelationId, receipt.SnapshotRevision, CharacterPersistenceOutcome.Committed);
        Assert.IsTrue(state.IsPersisted(42, "fingerprint"));
    }

    [TestMethod]
    public void Acknowledge_DuplicateIsIdempotent()
    {
        var state = new CharacterPersistenceState();
        var receipt = CreateReceipt(Guid.NewGuid(), 7);
        state.MarkPending(42, "fingerprint", receipt);

        state.Acknowledge(42, receipt.CorrelationId, receipt.SnapshotRevision, CharacterPersistenceOutcome.Duplicate);
        state.Acknowledge(42, receipt.CorrelationId, receipt.SnapshotRevision, CharacterPersistenceOutcome.Duplicate);

        Assert.IsTrue(state.IsPersisted(42, "fingerprint"));
    }

    [TestMethod]
    public void NextRevision_IsSeededAndRemainsMonotonic()
    {
        var state = new CharacterPersistenceState();
        state.InitializeRevision(42, 500);

        Assert.AreEqual(501L, state.NextRevision(42));
        state.InitializeRevision(42, 100);
        Assert.AreEqual(502L, state.NextRevision(42));
    }

    [TestMethod]
    public void InitializeRevision_RejectsNegativePersistedRevision()
    {
        var state = new CharacterPersistenceState();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => state.InitializeRevision(42, -1));
    }

    [TestMethod]
    public async Task NextRevision_ConcurrentCallsRemainUniqueAndMonotonic()
    {
        var state = new CharacterPersistenceState();
        state.InitializeRevision(42, 100);

        var revisions = await Task.WhenAll(
            Enumerable.Range(0, 32).Select(_ => Task.Run(() => state.NextRevision(42))));

        Assert.AreEqual(revisions.Length, revisions.Distinct().Count());
        Assert.AreEqual(101L, revisions.Min());
        Assert.AreEqual(132L, revisions.Max());
    }

    [TestMethod]
    public async Task AcquireAsync_ForSameCharacter_RemainsSerializedAndRetiresLockEntry()
    {
        var state = new CharacterPersistenceState();
        using var firstHandle = await state.AcquireAsync(42, CancellationToken.None);
        var secondHandleTask = state.AcquireAsync(42, CancellationToken.None);

        Assert.IsFalse(secondHandleTask.IsCompleted);
        firstHandle.Dispose();
        using var secondHandle = await secondHandleTask;
        secondHandle.Dispose();

        Assert.AreEqual(0, state.LockCount);
    }

    private static CharacterPersistenceReceipt CreateReceipt(Guid correlationId, long revision) =>
        new(42, correlationId, revision);
}
