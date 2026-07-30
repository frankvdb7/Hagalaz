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
        state.MarkPending(42, "fingerprint", 7);

        Assert.IsFalse(state.IsPersisted(42, "fingerprint"));

        state.Acknowledge(42, 6);
        Assert.IsFalse(state.IsPersisted(42, "fingerprint"));

        state.Acknowledge(42, 7);
        Assert.IsTrue(state.IsPersisted(42, "fingerprint"));
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
