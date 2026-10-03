using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Abstractions.Tests.Collections;

[TestClass]
public sealed class ItemContainerMutationBoundaryTests
{
    [TestMethod]
    public void TryTransferTo_InterfaceTypedBoundariesMoveExactQuantityAndPublishAfterCommit()
    {
        var sourceUpdates = 0;
        var destinationUpdates = 0;
        var source = new ItemContainer(StorageType.Normal, 2, _ => sourceUpdates++);
        var destination = new ItemContainer(StorageType.Normal, 2, _ => destinationUpdates++);
        var item = new TestItem(10, 7);
        Assert.IsTrue(source.Add(item));

        IItemContainerMutationBoundary sourceBoundary = source.Mutations;
        IItemContainerMutationBoundary destinationBoundary = destination.Mutations;
        Assert.IsTrue(sourceBoundary.TryTransferTo(destinationBoundary, item, 7, 0));

        Assert.AreEqual(0, source.TakenSlots);
        Assert.AreSame(item, destination[0]);
        Assert.AreEqual(7, destination[0]!.Count);
        Assert.AreEqual(2, sourceUpdates);
        Assert.AreEqual(1, destinationUpdates);
    }

    [TestMethod]
    public void TryTransferTo_InterfaceTypedBoundariesLeaveBothStoragesUnchangedOnFailure()
    {
        var sourceUpdates = 0;
        var destinationUpdates = 0;
        var source = new ItemContainer(StorageType.Normal, 2, _ => sourceUpdates++);
        var destination = new ItemContainer(StorageType.Normal, 1, _ => destinationUpdates++);
        var item = new TestItem(10, 2);
        var blockingItem = new TestItem(11, 1);
        Assert.IsTrue(source.Add(item));
        Assert.IsTrue(destination.Add(blockingItem));

        IItemContainerMutationBoundary sourceBoundary = source.Mutations;
        IItemContainerMutationBoundary destinationBoundary = destination.Mutations;
        Assert.IsFalse(sourceBoundary.TryTransferTo(destinationBoundary, item, 2, 0));

        Assert.AreSame(item, source[0]);
        Assert.AreEqual(2, source[0]!.Count);
        Assert.AreSame(blockingItem, destination[0]);
        Assert.AreEqual(1, sourceUpdates);
        Assert.AreEqual(1, destinationUpdates);
    }

    [TestMethod]
    public void Dispose_WithoutCommitRestoresReferencesCountsAndRevisionsOfAllStorage()
    {
        var publications = 0;
        var source = new ItemContainer(StorageType.Normal, 2, _ => publications++);
        var destination = new ItemContainer(StorageType.Normal, 2, _ => publications++);
        var item = new TestItem(20, 5);
        var existing = new TestItem(20, 4);
        Assert.IsTrue(source.Add(item));
        Assert.IsTrue(destination.Add(existing));
        publications = 0;
        var sourceEnumerator = source.GetEnumerator();
        var destinationEnumerator = destination.GetEnumerator();
        using (ItemContainerTransaction.Begin(source.Mutations, destination.Mutations))
        {
            Assert.IsTrue(source.Mutations.TryTransferTo(destination.Mutations, item, 3, 0));
            source.Clear(update: false); // Rollback must include mutations which intentionally suppress notification.
            Assert.AreEqual(7, existing.Count);
            Assert.AreEqual(0, publications);
        }
        Assert.AreSame(item, source[0]);
        Assert.AreEqual(5, item.Count);
        Assert.AreSame(existing, destination[0]);
        Assert.AreEqual(4, existing.Count);
        Assert.IsTrue(sourceEnumerator.MoveNext());
        Assert.AreSame(item, sourceEnumerator.Current);
        Assert.IsTrue(destinationEnumerator.MoveNext());
        Assert.AreSame(existing, destinationEnumerator.Current);
        Assert.AreEqual(0, publications);
    }

    [TestMethod]
    public void MutationException_DisposalRestoresStateAndPreservesException()
    {
        var source = new ItemContainer(StorageType.Normal, 2);
        var destination = new ItemContainer(StorageType.Normal, 2);
        var item = new TestItem(22, 5);
        Assert.IsTrue(source.Add(item));
        var original = new InvalidOperationException("Abort mutation.");
        var thrown = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            using var transaction = ItemContainerTransaction.Begin(source.Mutations, destination.Mutations);
            Assert.IsTrue(source.Mutations.TryTransferTo(destination.Mutations, item, 5));
            throw original;
        });
        Assert.AreSame(original, thrown);
        Assert.AreSame(item, source[0]);
        Assert.IsNull(destination[0]);
        Assert.AreEqual(5, item.Count);
        Assert.IsNull(Boundary(source).Storage.Transaction);
    }

    [TestMethod]
    public void Commit_PublishesOnceAfterUnlockAndMakesStoragePermanent()
    {
        var order = new List<string>();
        var first = new ItemContainer(StorageType.Normal, 2, _ => order.Add("first"));
        var second = new ItemContainer(StorageType.Normal, 2, _ => order.Add("second"));
        using var transaction = ItemContainerTransaction.Begin(second.Mutations, first.Mutations, second.Mutations);
        Assert.IsTrue(first.Add(new TestItem(23, 1)));
        Assert.IsTrue(second.Add(new TestItem(24, 1)));
        Boundary(first).DeferBeforePublication(() =>
        {
            AssertUnboundAndUnlocked(first, second);
            order.Add("hook");
        });
        Boundary(first).DeferAfterPublication(() =>
        {
            AssertUnboundAndUnlocked(first, second);
            Assert.AreEqual(1, first.TakenSlots);
            Assert.AreEqual(1, second.TakenSlots);
            order.Add("after1");
        });
        Boundary(second).DeferAfterPublication(() => order.Add("after2"));
        Assert.AreEqual(0, order.Count);
        transaction.Commit();
        transaction.Dispose();
        transaction.Dispose();
        Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Commit());
        CollectionAssert.AreEqual(new[] { "hook", "second", "first", "after1", "after2" }, order);
        Assert.AreEqual(1, first.TakenSlots);
        Assert.AreEqual(1, second.TakenSlots);
        AssertUnboundAndUnlocked(first, second);
    }

    [TestMethod]
    public void Dispose_TwiceIsInertAndCommitAfterDisposalIsInvalid()
    {
        var container = new ItemContainer(StorageType.Normal, 1);
        using var transaction = ItemContainerTransaction.Begin(container.Mutations);
        Assert.IsTrue(container.Add(new TestItem(25, 1)));
        transaction.Dispose();
        transaction.Dispose();
        Assert.IsNull(container[0]);
        Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Commit());
        AssertUnboundAndUnlocked(container);
    }

    [TestMethod]
    public void Commit_ChangedSlotsAreAggregatedAndFullChangesRemainFull()
    {
        var updates = new List<HashSet<int>?>();
        var container = new ItemContainer(StorageType.Normal, 3, slots => updates.Add(slots));
        using (var transaction = ItemContainerTransaction.Begin(container.Mutations))
        {
            Assert.IsTrue(container.Add(0, new TestItem(26, 1)));
            Assert.IsTrue(container.Add(2, new TestItem(27, 1)));
            Assert.AreEqual(0, updates.Count);
            transaction.Commit();
        }
        CollectionAssert.AreEquivalent(new[] { 0, 2 }, updates.Single()!.ToArray());
        updates.Clear();
        using (var transaction = ItemContainerTransaction.Begin(container.Mutations))
        {
            container.Clear(true);
            Assert.IsTrue(container.Add(0, new TestItem(28, 1)));
            transaction.Commit();
        }
        Assert.AreEqual(1, updates.Count);
        Assert.IsNull(updates[0]);
    }

    [TestMethod]
    public void Commit_EmptyRangeDoesNotPublishOrInvalidateEnumerator()
    {
        var publications = 0;
        var container = new ItemContainer(StorageType.Normal, 2, _ => publications++);
        var existing = new TestItem(31, 2);
        Assert.IsTrue(container.Add(existing));
        publications = 0;
        var enumerator = container.GetEnumerator();
        using var transaction = ItemContainerTransaction.Begin(container.Mutations);
        Assert.IsTrue(container.AddRange(Array.Empty<IItem?>()));
        transaction.Commit();
        Assert.AreEqual(0, publications);
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreSame(existing, enumerator.Current);
    }

    [TestMethod]
    public void Commit_RealMutationInvalidatesExistingEnumerators()
    {
        var source = new ItemContainer(StorageType.Normal, 2);
        var destination = new ItemContainer(StorageType.Normal, 2);
        var item = new TestItem(32, 1);
        Assert.IsTrue(source.Add(item));
        var sourceEnumerator = source.GetEnumerator();
        var destinationEnumerator = destination.GetEnumerator();
        using var transaction = ItemContainerTransaction.Begin(source.Mutations, destination.Mutations);
        Assert.IsTrue(source.Mutations.TryTransferTo(destination.Mutations, item, 1));
        transaction.Commit();
        Assert.ThrowsExactly<InvalidOperationException>(() => sourceEnumerator.MoveNext());
        Assert.ThrowsExactly<InvalidOperationException>(() => destinationEnumerator.MoveNext());
    }

    [TestMethod]
    public void Commit_ContainerFailureStopsLaterContainersAndAllAfterPublicationActionsWithoutRetryOrRollback()
    {
        var original = new InvalidOperationException("Publisher failed.");
        var calls = new List<string>();
        ItemContainer? first = null;
        ItemContainer? second = null;
        first = new ItemContainer(StorageType.Normal, 2, _ =>
        {
            AssertUnboundAndUnlocked(first!, second!);
            calls.Add("first");
            throw original;
        });
        second = new ItemContainer(StorageType.Normal, 2, _ => calls.Add("second"));
        using var transaction = ItemContainerTransaction.Begin(first.Mutations, second.Mutations);
        Assert.IsTrue(first.Add(new TestItem(33, 1)));
        Assert.IsTrue(second.Add(new TestItem(34, 1)));
        Boundary(first).DeferAfterPublication(() => calls.Add("after"));
        var thrown = Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Commit());
        Assert.AreSame(original, thrown);
        transaction.Dispose();
        transaction.Dispose();
        Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Commit());
        CollectionAssert.AreEqual(new[] { "first" }, calls);
        Assert.AreEqual(1, first.TakenSlots);
        Assert.AreEqual(1, second.TakenSlots);
    }

    [TestMethod]
    public void Commit_LaterContainerFailurePreservesEarlierPublicationAndSkipsLaterContainersAndActions()
    {
        var original = new InvalidOperationException("Second failed.");
        var calls = new List<string>();
        var first = new ItemContainer(StorageType.Normal, 1, _ => calls.Add("first"));
        var second = new ItemContainer(StorageType.Normal, 1, _ => { calls.Add("second"); throw original; });
        var third = new ItemContainer(StorageType.Normal, 1, _ => calls.Add("third"));
        using var transaction = ItemContainerTransaction.Begin(first.Mutations, second.Mutations, third.Mutations);
        Assert.IsTrue(first.Add(new TestItem(35, 1)));
        Assert.IsTrue(second.Add(new TestItem(36, 1)));
        Assert.IsTrue(third.Add(new TestItem(37, 1)));
        Boundary(first).DeferAfterPublication(() => calls.Add("after1"));
        Boundary(second).DeferAfterPublication(() => calls.Add("after2"));
        Assert.AreSame(original, Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Commit()));
        CollectionAssert.AreEqual(new[] { "first", "second" }, calls);
        transaction.Dispose();
        Assert.AreEqual(1, first.TakenSlots);
        Assert.AreEqual(1, second.TakenSlots);
        Assert.AreEqual(1, third.TakenSlots);
        AssertUnboundAndUnlocked(first, second, third);
    }

    [TestMethod]
    public void Commit_AfterPublicationFailureStopsLaterActionsAfterContainerPublication()
    {
        var calls = new List<string>();
        var original = new InvalidOperationException("After-publication action failed.");
        var container = new ItemContainer(StorageType.Normal, 1, _ => calls.Add("container"));
        using var transaction = ItemContainerTransaction.Begin(container.Mutations);
        Assert.IsTrue(container.Add(new TestItem(37, 1)));
        Boundary(container).DeferAfterPublication(() => calls.Add("after1"));
        Boundary(container).DeferAfterPublication(() => { AssertUnboundAndUnlocked(container); calls.Add("after2"); throw original; });
        Boundary(container).DeferAfterPublication(() => calls.Add("after3"));
        Assert.AreSame(original, Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Commit()));
        transaction.Dispose();
        Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Commit());
        CollectionAssert.AreEqual(new[] { "container", "after1", "after2" }, calls);
        Assert.AreEqual(1, container.TakenSlots);
    }

    [TestMethod]
    public void Commit_HookFailureStillPublishesAndRetainsOriginalFailure()
    {
        var calls = new List<string>();
        var original = new InvalidOperationException("Hook failed.");
        var container = new ItemContainer(StorageType.Normal, 1, _ => calls.Add("container"));
        using var transaction = ItemContainerTransaction.Begin(container.Mutations);
        Assert.IsTrue(container.Add(new TestItem(38, 1)));
        Boundary(container).DeferBeforePublication(() => { AssertUnboundAndUnlocked(container); calls.Add("hook"); throw original; });
        Boundary(container).DeferAfterPublication(() => calls.Add("after"));
        Assert.AreSame(original, Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Commit()));
        transaction.Dispose();
        Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Commit());
        CollectionAssert.AreEqual(new[] { "hook", "container", "after" }, calls);
        Assert.AreEqual(1, container.TakenSlots);
    }

    [TestMethod]
    public void Commit_HookAndPublicationFailureRetainBothOriginalExceptions()
    {
        var hookFailure = new InvalidOperationException("Hook failed.");
        var publicationFailure = new InvalidOperationException("Publisher failed.");
        var container = new ItemContainer(StorageType.Normal, 1, _ => throw publicationFailure);
        using var transaction = ItemContainerTransaction.Begin(container.Mutations);
        Assert.IsTrue(container.Add(new TestItem(39, 1)));
        Boundary(container).DeferBeforePublication(() => throw hookFailure);
        var thrown = Assert.ThrowsExactly<AggregateException>(() => transaction.Commit());
        CollectionAssert.AreEqual(new Exception[] { hookFailure, publicationFailure }, thrown.InnerExceptions.ToArray());
        Assert.IsNotNull(hookFailure.StackTrace);
        Assert.IsNotNull(publicationFailure.StackTrace);
        transaction.Dispose();
        Assert.AreEqual(1, container.TakenSlots);
        AssertUnboundAndUnlocked(container);
    }

    [TestMethod]
    public void Begin_RejectsInvalidArgumentsAndContributionsBeforeLocking()
    {
        var container = new ItemContainer(StorageType.Normal, 1);
        Assert.ThrowsExactly<ArgumentNullException>(() => ItemContainerTransaction.Begin(null!));
        Assert.ThrowsExactly<ArgumentException>(() => ItemContainerTransaction.Begin());
        Assert.ThrowsExactly<ArgumentNullException>(() => ItemContainerTransaction.Begin(container.Mutations, null!));
        Assert.ThrowsExactly<ArgumentException>(() => ItemContainerTransaction.Begin(container.Mutations, new ExternalParticipant()));
        Assert.ThrowsExactly<ArgumentException>(() => ItemContainerTransaction.Begin(container.Mutations, new CompositeParticipant([])));
        Assert.ThrowsExactly<ArgumentException>(() => ItemContainerTransaction.Begin(container.Mutations, new CompositeParticipant([null!])));
        AssertUnboundAndUnlocked(container);
        using var valid = ItemContainerTransaction.Begin(container.Mutations);
    }

    [TestMethod]
    public void Begin_ResolvesEveryContributionBeforeAcquiringFirstLock()
    {
        var container = new ItemContainer(StorageType.Normal, 1);
        var storage = Boundary(container).Storage;
        var invalid = new CompositeParticipant([])
        {
            OnResolve = () =>
            {
                Assert.IsFalse(Monitor.IsEntered(storage.MutationLock));
                Assert.IsNull(storage.Transaction);
            }
        };
        Assert.ThrowsExactly<ArgumentException>(() => ItemContainerTransaction.Begin(container.Mutations, invalid));
        AssertUnboundAndUnlocked(container);
    }

    [TestMethod]
    public void Begin_SnapshotFailureReleasesAllLocksAndNeverExposesBindings()
    {
        var calls = 0;
        var first = new ItemContainer(StorageType.Normal, 1, _ => calls++);
        var second = new ItemContainer(StorageType.Normal, 1, _ => calls++);
        var firstItem = new TestItem(40, 3);
        var failingItem = new TestItem(41, 4);
        Assert.IsTrue(first.Add(firstItem));
        Assert.IsTrue(second.Add(failingItem));
        calls = 0;
        var original = new InvalidOperationException("Count capture failed.");
        failingItem.OnCountRead = () =>
        {
            Assert.IsTrue(Monitor.IsEntered(Boundary(first).Storage.MutationLock));
            Assert.IsTrue(Monitor.IsEntered(Boundary(second).Storage.MutationLock));
            Assert.IsNull(Boundary(first).Storage.Transaction);
            Assert.IsNull(Boundary(second).Storage.Transaction);
            throw original;
        };
        Assert.AreSame(original, Assert.ThrowsExactly<InvalidOperationException>(() => ItemContainerTransaction.Begin(first.Mutations, second.Mutations)));
        failingItem.OnCountRead = null;
        AssertUnboundAndUnlocked(first, second);
        Assert.AreSame(firstItem, first[0]);
        Assert.AreSame(failingItem, second[0]);
        Assert.AreEqual(3, firstItem.Count);
        Assert.AreEqual(4, failingItem.Count);
        Assert.AreEqual(0, calls);
        using var fresh = ItemContainerTransaction.Begin(first.Mutations, second.Mutations);
    }

    [TestMethod]
    public void Begin_LockOrderIsDeterministicAndIndependentFromPublicationOrder()
    {
        var first = new ItemContainer(StorageType.Normal, 1);
        var second = new ItemContainer(StorageType.Normal, 1);
        using var transaction = ItemContainerTransaction.Begin(second.Mutations, first.Mutations);
        var lockOrder = (ItemContainerStorage[])typeof(ItemContainerTransaction)
            .GetField("_lockOrder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(transaction)!;
        CollectionAssert.AreEqual(new[] { Boundary(first).Storage, Boundary(second).Storage }, lockOrder);
        Assert.IsTrue(Monitor.IsEntered(lockOrder[0].MutationLock));
        Assert.IsTrue(Monitor.IsEntered(lockOrder[1].MutationLock));
    }

    [TestMethod]
    public void Begin_AliasesSnapshotAndPublishUnderlyingStorageOnceInFirstSeenOrder()
    {
        var calls = new List<string>();
        var first = new ItemContainer(StorageType.Normal, 1, _ => calls.Add("first"));
        var second = new ItemContainer(StorageType.Normal, 1, _ => calls.Add("second"));
        var item = new TestItem(42, 1);
        Assert.IsTrue(first.Add(item));
        calls.Clear();
        var reads = 0;
        item.OnCountRead = () => reads++;
        var composite = new CompositeParticipant([Boundary(first), Boundary(second)]);
        using var transaction = ItemContainerTransaction.Begin(first.Mutations, composite, composite, second.Mutations);
        Assert.AreEqual(1, reads);
        item.OnCountRead = null;
        first.Clear(true);
        Assert.IsTrue(second.Add(item));
        transaction.Commit();
        CollectionAssert.AreEqual(new[] { "first", "second" }, calls);
        AssertUnboundAndUnlocked(first, second);
    }

    [TestMethod]
    public void Transfer_PartialEnlistmentRejectsWithoutLockingMissingStorageOrStartingNestedScope()
    {
        var first = new ItemContainer(StorageType.Normal, 1);
        var second = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(43, 1);
        Assert.IsTrue(first.Add(item));
        using var transaction = ItemContainerTransaction.Begin(first.Mutations);
        Assert.ThrowsExactly<InvalidOperationException>(() => first.Mutations.TryTransferTo(second.Mutations, item, 1));
        Assert.AreSame(transaction, Boundary(first).Storage.Transaction);
        Assert.AreSame(item, first[0]);
        Assert.IsNull(second[0]);
        AssertUnboundAndUnlocked(second);
        Assert.ThrowsExactly<InvalidOperationException>(() => ItemContainerTransaction.Begin(first.Mutations, second.Mutations));
        AssertUnboundAndUnlocked(second);
    }

    [TestMethod]
    public void Transfer_DifferentActiveScopesRejectWithoutMutation()
    {
        var first = new ItemContainer(StorageType.Normal, 1);
        var second = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(44, 1);
        Assert.IsTrue(first.Add(item));
        using var firstScope = ItemContainerTransaction.Begin(first.Mutations);
        using var secondScope = ItemContainerTransaction.Begin(second.Mutations);
        Assert.ThrowsExactly<InvalidOperationException>(() => first.Mutations.TryTransferTo(second.Mutations, item, 1));
        Assert.AreSame(firstScope, Boundary(first).Storage.Transaction);
        Assert.AreSame(secondScope, Boundary(second).Storage.Transaction);
        Assert.AreSame(item, first[0]);
        Assert.IsNull(second[0]);
    }

    [TestMethod]
    public void Scope_WrongThreadCommitDisposeAndMutationRejectWithoutEndingScope()
    {
        var first = new ItemContainer(StorageType.Normal, 2);
        var second = new ItemContainer(StorageType.Normal, 2);
        var item = new TestItem(45, 1);
        Assert.IsTrue(first.Add(item));
        using var transaction = ItemContainerTransaction.Begin(first.Mutations, second.Mutations);
        OnOtherThread(() =>
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Commit());
            Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Dispose());
            Assert.ThrowsExactly<InvalidOperationException>(() => first.Add(new TestItem(46, 1)));
        });
        Assert.AreSame(transaction, Boundary(first).Storage.Transaction);
        Assert.AreSame(item, first[0]);
        Assert.AreEqual(1, first.TakenSlots);
        Assert.AreEqual(0, second.TakenSlots);
        transaction.Commit();
        OnOtherThread(() => Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Dispose()));
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void Begin_IndependentContenderWaitsForSameOrOverlappingStorage(bool overlapping, bool commit)
    {
        var first = new ItemContainer(StorageType.Normal, 1);
        var second = new ItemContainer(StorageType.Normal, 1);
        var third = new ItemContainer(StorageType.Normal, 1);
        using var attempted = new ManualResetEventSlim();
        using var completed = new ManualResetEventSlim();
        using var owner = ItemContainerTransaction.Begin(first.Mutations, second.Mutations);
        var required = overlapping
            ? new[] { Boundary(third), Boundary(second) }
            : new[] { Boundary(first) };
        var participant = new CompositeParticipant(required) { OnResolve = attempted.Set };
        Exception? failure = null;
        var worker = new Thread(() =>
        {
            try
            {
                using var contender = ItemContainerTransaction.Begin(participant);
                var lockOrder = (ItemContainerStorage[])typeof(ItemContainerTransaction)
                    .GetField("_lockOrder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(contender)!;
                CollectionAssert.AreEqual(required.Select(boundary => boundary.Storage)
                    .OrderBy(storage => storage.MutationOrder).ToArray(), lockOrder);
                foreach (var storage in lockOrder)
                {
                    Assert.IsTrue(Monitor.IsEntered(storage.MutationLock));
                    Assert.AreSame(contender, storage.Transaction);
                }
                contender.Commit();
            }
            catch (Exception exception) { failure = exception; }
            finally { completed.Set(); }
        }) { IsBackground = true };
        worker.Start();
        try
        {
            Assert.IsTrue(attempted.Wait(TimeSpan.FromSeconds(5)), "The contender must resolve its participants.");
            Assert.IsFalse(completed.Wait(TimeSpan.FromMilliseconds(100)),
                "The contender must wait, rather than reject the foreign binding or bypass its lock.");
            Assert.AreSame(owner, Boundary(second).Storage.Transaction);
        }
        finally
        {
            if (commit) owner.Commit();
            else owner.Dispose();
        }
        Assert.IsTrue(worker.Join(TimeSpan.FromSeconds(5)), "Ordered overlapping scopes must not deadlock.");
        if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        AssertUnboundAndUnlocked(first, second, third);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Commit_MultipleHookFailuresAreFlatAndRetainOriginalExceptions(bool publicationFails)
    {
        var hookFailures = Enumerable.Range(0, 3).Select(index => new InvalidOperationException($"Hook {index}")).ToArray();
        var publicationFailure = new InvalidOperationException("Publisher failed.");
        var calls = 0;
        var container = new ItemContainer(StorageType.Normal, 1, _ =>
        {
            calls++;
            if (publicationFails) throw publicationFailure;
        });
        using var transaction = ItemContainerTransaction.Begin(container.Mutations);
        Assert.IsTrue(container.Add(new TestItem(47, 1)));
        foreach (var failure in hookFailures)
            Boundary(container).DeferBeforePublication(() => { AssertUnboundAndUnlocked(container); throw failure; });
        var thrown = Assert.ThrowsExactly<AggregateException>(() => transaction.Commit());
        var expected = publicationFails ? hookFailures.Cast<Exception>().Append(publicationFailure).ToArray() : hookFailures;
        CollectionAssert.AreEqual(expected, thrown.InnerExceptions.ToArray());
        Assert.IsTrue(thrown.InnerExceptions.All(exception => exception.StackTrace != null));
        transaction.Dispose();
        transaction.Dispose();
        Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Commit());
        Assert.AreEqual(1, calls);
        Assert.AreEqual(1, container.TakenSlots);
        AssertUnboundAndUnlocked(container);
    }

    [TestMethod]
    public void Transaction_PublicApiIsOnlyOneScopeAndMarkerExposesNoMechanics()
    {
        var methods = typeof(ItemContainerTransaction).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        CollectionAssert.AreEquivalent(new[] { "Begin", "Commit", "Dispose" }, methods.Select(method => method.Name).ToArray());
        Assert.AreEqual(0, typeof(ItemContainerTransaction).GetConstructors().Length);
        Assert.AreEqual(0, typeof(ItemContainerTransaction).GetProperties().Length);
        Assert.AreEqual(0, typeof(IItemContainerTransactionParticipant).GetMembers().Length);
        Assert.IsNull(typeof(IItemContainerMutationBoundary).GetProperty("Storage"));
        Assert.IsNull(typeof(IItemContainerMutationBoundary).GetProperty("MutationLock"));
    }

    [TestMethod]
    public void ItemContainer_KeepsRawStorageAndStateHelpersInternal()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Assert.IsNull(typeof(ItemContainer).GetProperty("Storage", flags));
        foreach (var name in new[] { "RestoreItems", "SnapshotItems", "ReplaceState", "ExecuteUnderMutationLock" })
        {
            var method = typeof(ItemContainer).GetMethod(name, flags);
            Assert.IsNotNull(method, name);
            Assert.IsTrue(method.IsAssembly, name);
        }
    }

    private static ItemContainerMutationBoundary Boundary(ItemContainer container) => (ItemContainerMutationBoundary)container.Mutations;

    private static void AssertUnboundAndUnlocked(params ItemContainer[] containers)
    {
        foreach (var container in containers)
        {
            Assert.IsNull(Boundary(container).Storage.Transaction);
            var storage = Boundary(container).Storage;
            Assert.IsFalse(Monitor.IsEntered(storage.MutationLock));
            var acquired = false;
            OnOtherThread(() =>
            {
                acquired = Monitor.TryEnter(storage.MutationLock);
                if (acquired) Monitor.Exit(storage.MutationLock);
            });
            Assert.IsTrue(acquired);
        }
    }

    private static void OnOtherThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception exception) { failure = exception; }
        }) { IsBackground = true };
        thread.Start();
        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(5)), "The worker must reject misuse without blocking on owned locks.");
        if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private sealed class ExternalParticipant : IItemContainerTransactionParticipant { }
    private sealed class CompositeParticipant(IReadOnlyList<ItemContainerMutationBoundary> boundaries) : IItemContainerTransactionParticipantInternal
    {
        public Action? OnResolve { get; init; }
        public IReadOnlyList<ItemContainerMutationBoundary> Boundaries { get { OnResolve?.Invoke(); return boundaries; } }
    }

    [TestMethod]
    public void OrdinaryDomainInterfacesTransferBetweenInterfaceTypedContainers()
    {
        IItemContainer inventoryItems = new ItemContainer(StorageType.Normal, 2);
        IItemContainer bankItems = new ItemContainer(StorageType.AlwaysStack, 2);
        var inventory = Substitute.For<IInventoryContainer>();
        inventory.Items.Returns(inventoryItems);
        var bank = Substitute.For<IBankContainer>();
        bank.Items.Returns(bankItems);
        var item = new TestItem(30, 4);
        Assert.IsTrue(inventory.Items.Add(item));

        Assert.IsTrue(inventory.Items.Mutations.TryTransferTo(bank.Items.Mutations, item, 4, 0));

        Assert.IsNull(inventory.Items[0]);
        Assert.AreSame(item, bank.Items[0]);
    }

    [TestMethod]
    public void DomainAndInfrastructureInterfacesDoNotExposeConcreteContainerImplementations()
    {
        var ordinaryContainers = new[]
        {
            typeof(IInventoryContainer), typeof(IBankContainer), typeof(IRewardContainer),
            typeof(IFamiliarInventoryContainer), typeof(IShopStockContainer)
        };
        foreach (var contract in ordinaryContainers)
        {
            Assert.AreEqual(typeof(IItemContainer), contract.GetProperty("Items")!.PropertyType, contract.Name);
        }

        Assert.AreEqual(typeof(IItemContainerMutationBoundary), typeof(IItemContainer).GetProperty("Mutations")!.PropertyType);
        var domainContracts = ordinaryContainers.Concat([typeof(IEquipmentContainer), typeof(IMoneyPouchContainer)]);
        foreach (var contract in domainContracts)
        {
            foreach (var method in contract.GetMethods())
            {
                Assert.IsFalse(method.GetParameters().Any(parameter =>
                    parameter.ParameterType == typeof(ItemContainer) ||
                    parameter.ParameterType == typeof(ItemContainerMutationBoundary) ||
                    parameter.ParameterType == typeof(ItemContainerTransaction) ||
                    parameter.ParameterType == typeof(ItemContainerStorage)), contract.Name + "." + method.Name);
                Assert.IsFalse(method.ReturnType == typeof(ItemContainer) ||
                    method.ReturnType == typeof(ItemContainerMutationBoundary) ||
                    method.ReturnType == typeof(ItemContainerTransaction) ||
                    method.ReturnType == typeof(ItemContainerStorage), contract.Name + "." + method.Name);
            }
        }

        Assert.IsFalse(typeof(IEquipmentContainer).GetProperty("Items") is not null);
        Assert.IsFalse(typeof(IEquipmentContainer).GetProperty("Mutations") is not null);
        Assert.IsFalse(typeof(IMoneyPouchContainer).GetProperty("Items") is not null);
        Assert.AreEqual(typeof(IItemContainerTransactionParticipant), typeof(IMoneyPouchContainer).GetProperty("Mutations")!.PropertyType);
    }

    private sealed class TestItem : IItem
    {
        public int Id { get; }
        private int _count;
        public Action? OnCountRead { get; set; }
        public int Count { get { OnCountRead?.Invoke(); return _count; } set => _count = value; }
        public string Name => $"Item {Id}";
        public IItemDefinition ItemDefinition { get; }
        public IEquipmentDefinition EquipmentDefinition { get; } = Substitute.For<IEquipmentDefinition>();
        public IItemScript ItemScript { get; }
        public IEquipmentScript EquipmentScript { get; } = Substitute.For<IEquipmentScript>();
        public long[] ExtraData => [];

        public TestItem(int id, int count)
        {
            Id = id;
            Count = count;
            ItemDefinition = Substitute.For<IItemDefinition>();
            ItemDefinition.Stackable.Returns(true);
            ItemDefinition.Noted.Returns(false);
            ItemScript = Substitute.For<IItemScript>();
            ItemScript.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>())
                .Returns(call => call.ArgAt<IItem>(0).Id == call.ArgAt<IItem>(1).Id);
        }

        public IItem Clone() => Clone(1);
        public IItem Clone(int newCount) => new TestItem(Id, newCount);
        public bool Equals(IItem otherItem, bool ignoreCount = true) =>
            otherItem != null && Id == otherItem.Id && (ignoreCount || Count == otherItem.Count);
        public string? SerializeExtraData() => null;
    }
}
