using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Services.GameWorld.Services;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterPersistenceStateTests
{
    [TestMethod]
    public void Acknowledge_MarksOnlyMatchingPendingRevisionAsPersisted()
    {
        var state = new CharacterPersistenceState();
        var correlationId = Guid.NewGuid();
        state.MarkPending(42, correlationId, "fingerprint", 7);

        Assert.IsFalse(state.IsPersisted(42, "fingerprint"));

        state.Acknowledge(42, correlationId, 6);
        Assert.IsFalse(state.IsPersisted(42, "fingerprint"));

        state.Acknowledge(42, correlationId, 7);
        Assert.IsTrue(state.IsPersisted(42, "fingerprint"));
    }

    [TestMethod]
    public void Acknowledge_RequiresMatchingCorrelationIdAndRevision()
    {
        var state = new CharacterPersistenceState();
        var pendingCorrelationId = Guid.NewGuid();
        state.MarkPending(42, pendingCorrelationId, "fingerprint", 7);

        state.Acknowledge(42, Guid.NewGuid(), 7);

        Assert.IsFalse(state.IsPersistenceAcknowledged(42));
        Assert.IsFalse(state.IsPersisted(42, "fingerprint"));

        state.Acknowledge(42, pendingCorrelationId, 7);

        Assert.IsTrue(state.IsPersistenceAcknowledged(42));
        Assert.IsTrue(state.IsPersisted(42, "fingerprint"));
    }

    [TestMethod]
    public void NextRevision_IsSeededFromPersistedRevisionAndRemainsMonotonic()
    {
        var state = new CharacterPersistenceState();

        state.InitializeRevision(42, 500);

        Assert.AreEqual(501L, state.NextRevision(42));
        Assert.AreEqual(502L, state.NextRevision(42));

        state.InitializeRevision(42, 100);

        Assert.AreEqual(503L, state.NextRevision(42));
    }

    [TestMethod]
    public void InitializeRevision_RejectsNegativePersistedRevision()
    {
        var state = new CharacterPersistenceState();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => state.InitializeRevision(42, -1));
    }

    [TestMethod]
    public void Forget_RemovesRevisionAllocationState()
    {
        var state = new CharacterPersistenceState();
        state.InitializeRevision(42, 500);

        state.Forget(42);

        Assert.AreEqual(1L, state.NextRevision(42));
    }

    [TestMethod]
    public void RevisionAllocation_AfterClockRollback_RemainsAbovePersistedRevision()
    {
        var state = new CharacterPersistenceState();
        state.InitializeRevision(42, 900);

        // Revision allocation has no wall-clock input, so a clock rollback cannot
        // produce a revision below the hydrated persisted value.
        Assert.AreEqual(901L, state.NextRevision(42));
    }

    [TestMethod]
    public void RevisionAllocation_AfterProcessRestart_ResumesFromHydratedRevision()
    {
        var firstProcess = new CharacterPersistenceState();
        firstProcess.InitializeRevision(42, 10);
        Assert.AreEqual(11L, firstProcess.NextRevision(42));

        var restartedProcess = new CharacterPersistenceState();
        restartedProcess.InitializeRevision(42, 11);

        Assert.AreEqual(12L, restartedProcess.NextRevision(42));
    }

    [TestMethod]
    public void RevisionAllocation_AfterWorldMigration_UsesMigratedPersistedRevision()
    {
        var state = new CharacterPersistenceState();
        state.InitializeRevision(42, 10_000);

        Assert.AreEqual(10_001L, state.NextRevision(42));
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
    public void IsPersistenceAcknowledged_RemainsFalseUntilMatchingAcknowledgement()
    {
        var state = new CharacterPersistenceState();
        var correlationId = Guid.NewGuid();
        state.MarkPending(42, correlationId, "fingerprint", 7);

        Assert.IsFalse(state.IsPersistenceAcknowledged(42));

        state.Acknowledge(42, correlationId, 6);
        Assert.IsFalse(state.IsPersistenceAcknowledged(42));

        state.Acknowledge(42, correlationId, 7);
        Assert.IsTrue(state.IsPersistenceAcknowledged(42));
    }

    [TestMethod]
    public async Task AcquireAsync_AfterCharacterChurn_RetiresAllLockEntries()
    {
        var state = new CharacterPersistenceState();

        for (var masterId = 1u; masterId <= 1000; masterId++)
        {
            using var handle = await state.AcquireAsync(masterId, CancellationToken.None);
        }

        Assert.AreEqual(0, state.LockCount);
    }

    [TestMethod]
    public async Task AcquireAsync_ForSameCharacter_RemainsSerializedAndRetiresAfterLastHolder()
    {
        var state = new CharacterPersistenceState();
        using var firstHandle = await state.AcquireAsync(42, CancellationToken.None);
        var secondHandleTask = state.AcquireAsync(42, CancellationToken.None);

        Assert.IsFalse(secondHandleTask.IsCompleted);
        Assert.AreEqual(1, state.LockCount);

        firstHandle.Dispose();
        using var secondHandle = await secondHandleTask;
        Assert.AreEqual(1, state.LockCount);

        secondHandle.Dispose();
        Assert.AreEqual(0, state.LockCount);
    }
}
