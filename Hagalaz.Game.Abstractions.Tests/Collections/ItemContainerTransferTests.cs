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
public sealed class ItemContainerTransferTests
{
    [TestMethod]
    public void TryTransferTo_StandaloneInterfaceTypedContainersTrackBothChangedSlots()
    {
        var source = new ItemContainer(StorageType.Normal, 2);
        var destination = new ItemContainer(StorageType.Normal, 2);
        var item = new TestItem(10, 7);
        Assert.IsTrue(source.Add(item));
        source.ChangeTracker.GetSlots();

        IItemContainer sourceContract = source;
        IItemContainer destinationContract = destination;
        Assert.IsTrue(sourceContract.TryTransferTo(destinationContract, item, 7, 0));

        Assert.IsNull(source[0]);
        Assert.AreSame(item, destination[0]);
        Assert.AreEqual(7, destination[0]!.Count);
        CollectionAssert.AreEquivalent(new[] { 0 }, source.ChangeTracker.GetSlots()!.ToArray());
        CollectionAssert.AreEquivalent(new[] { 0 }, destination.ChangeTracker.GetSlots()!.ToArray());
        AssertUnboundAndUnlocked(source, destination);
    }

    [TestMethod]
    public void TryTransferTo_InterfaceTypedContainersLeaveBothStoragesUnchangedOnFailure()
    {
        var source = new ItemContainer(StorageType.Normal, 2);
        var destination = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(10, 2);
        var blockingItem = new TestItem(11, 1);
        Assert.IsTrue(source.Add(item));
        Assert.IsTrue(destination.Add(blockingItem));
        source.ChangeTracker.GetSlots();
        destination.ChangeTracker.GetSlots();

        Assert.IsFalse(source.TryTransferTo(destination, item, 2, 0));

        Assert.AreSame(item, source[0]);
        Assert.AreEqual(2, source[0]!.Count);
        Assert.AreSame(blockingItem, destination[0]);
        Assert.IsFalse(source.ChangeTracker.HasChanges);
        Assert.IsFalse(destination.ChangeTracker.HasChanges);
        AssertUnboundAndUnlocked(source, destination);
    }

    [TestMethod]
    public void TryTransferTo_ParticipatesInCallerTransactionAndPromotesChangesOnCommit()
    {
        var source = new ItemContainer(StorageType.Normal, 2);
        var destination = new ItemContainer(StorageType.Normal, 2);
        var item = new TestItem(12, 3);
        Assert.IsTrue(source.Add(item));
        source.ChangeTracker.GetSlots();

        using var transaction = ItemContainerTransaction.Begin(source, destination);
        Assert.IsTrue(source.TryTransferTo(destination, item, 3));
        Assert.AreSame(transaction, Boundary(source).Transaction);
        Assert.AreSame(transaction, Boundary(destination).Transaction);
        Assert.IsTrue(Monitor.IsEntered(Boundary(source).MutationLock));
        Assert.IsTrue(Monitor.IsEntered(Boundary(destination).MutationLock));
        Assert.IsFalse(source.ChangeTracker.HasChanges);
        Assert.IsFalse(destination.ChangeTracker.HasChanges);

        transaction.Commit();
        Assert.IsFalse(Monitor.IsEntered(Boundary(source).MutationLock));
        Assert.IsFalse(Monitor.IsEntered(Boundary(destination).MutationLock));
        Assert.AreSame(transaction, Boundary(source).Transaction);
        Assert.AreSame(transaction, Boundary(destination).Transaction);
        CollectionAssert.AreEquivalent(new[] { 0 }, source.ChangeTracker.GetSlots()!.ToArray());
        CollectionAssert.AreEquivalent(new[] { 0 }, destination.ChangeTracker.GetSlots()!.ToArray());
        transaction.Dispose();
        AssertUnboundAndUnlocked(source, destination);
    }

    [TestMethod]
    public void CurrentTransaction_UsesCurrentThreadBindingForSharedDiscovery()
    {
        var source = new ItemContainer(StorageType.Normal, 1);
        var destination = new ItemContainer(StorageType.Normal, 1);
        var sourceBoundary = Boundary(source);
        var destinationBoundary = Boundary(destination);

        Assert.IsNull(sourceBoundary.CurrentTransaction);
        Assert.IsNull(sourceBoundary.GetSharedCurrentThreadTransaction(destinationBoundary));

        using var transaction = ItemContainerTransaction.Begin(source, destination);
        Assert.AreSame(transaction, sourceBoundary.CurrentTransaction);
        Assert.AreSame(transaction, destinationBoundary.CurrentTransaction);
        Assert.AreSame(transaction, sourceBoundary.GetSharedCurrentThreadTransaction(destinationBoundary));

        transaction.Commit();

        Assert.AreSame(transaction, sourceBoundary.Transaction);
        Assert.AreSame(transaction, destinationBoundary.Transaction);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = sourceBoundary.CurrentTransaction);
        transaction.Dispose();
        Assert.IsNull(sourceBoundary.CurrentTransaction);
        Assert.IsNull(destinationBoundary.CurrentTransaction);
    }

    [TestMethod]
    public void CurrentTransaction_ForeignBindingIsNotJoinable()
    {
        using var transactionStarted = new ManualResetEventSlim();
        using var finishTransaction = new ManualResetEventSlim();
        var container = new ItemContainer(StorageType.Normal, 1);
        var boundary = Boundary(container);
        Exception? transactionFailure = null;
        var transactionThread = new Thread(() =>
        {
            try
            {
                using var transaction = ItemContainerTransaction.Begin(container);
                transactionStarted.Set();
                Assert.IsTrue(finishTransaction.Wait(TimeSpan.FromSeconds(5)));
                transaction.Commit();
            }
            catch (Exception exception) { transactionFailure = exception; }
        }) { IsBackground = true };

        transactionThread.Start();
        try
        {
            Assert.IsTrue(transactionStarted.Wait(TimeSpan.FromSeconds(5)));
            Assert.IsNull(boundary.CurrentTransaction);
        }
        finally { finishTransaction.Set(); }

        Assert.IsTrue(transactionThread.Join(TimeSpan.FromSeconds(5)));
        if (transactionFailure != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(transactionFailure).Throw();
        Assert.IsNull(boundary.CurrentTransaction);
    }

                        [TestMethod]
    public void TryTransferTo_OppositeStandaloneTransfersUseOneLockOrder()
    {
        using var ready = new CountdownEvent(2);
        using var start = new ManualResetEventSlim();
        var first = new ItemContainer(StorageType.Normal, 2);
        var second = new ItemContainer(StorageType.Normal, 2);
        var firstItem = new TestItem(151, 1);
        var secondItem = new TestItem(152, 1);
        Assert.IsTrue(first.Add(firstItem));
        Assert.IsTrue(second.Add(secondItem));
        var firstResult = false;
        var secondResult = false;
        Exception? firstFailure = null;
        Exception? secondFailure = null;
        var firstThread = new Thread(() =>
        {
            ready.Signal();
            start.Wait();
            try { firstResult = first.TryTransferTo(second, firstItem, 1); }
            catch (Exception exception) { firstFailure = exception; }
        }) { IsBackground = true };
        var secondThread = new Thread(() =>
        {
            ready.Signal();
            start.Wait();
            try { secondResult = second.TryTransferTo(first, secondItem, 1); }
            catch (Exception exception) { secondFailure = exception; }
        }) { IsBackground = true };

        firstThread.Start();
        secondThread.Start();
        Assert.IsTrue(ready.Wait(TimeSpan.FromSeconds(5)));
        start.Set();
        Assert.IsTrue(firstThread.Join(TimeSpan.FromSeconds(5)), "The first direction must finish.");
        Assert.IsTrue(secondThread.Join(TimeSpan.FromSeconds(5)), "The opposite direction must not deadlock.");
        if (firstFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(firstFailure).Throw();
        if (secondFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(secondFailure).Throw();

        Assert.IsTrue(firstResult);
        Assert.IsTrue(secondResult);
        Assert.AreSame(secondItem, first.GetById(secondItem.Id));
        Assert.AreSame(firstItem, second.GetById(firstItem.Id));
        AssertUnboundAndUnlocked(first, second);
    }

            [TestMethod]
    public void OrdinaryMutations_ParticipateAndTrackerPromotesOnlyOnCommit()
    {
        var container = new ItemContainer(StorageType.Normal, 3);
        var first = new TestItem(14, 1);
        var second = new TestItem(15, 1);

        using (ItemContainerTransaction.Begin(container))
        {
            Assert.IsTrue(container.Add(first));
            Assert.IsTrue(container.AddRange([second]));
            Assert.IsFalse(container.ChangeTracker.HasChanges);
        }
        Assert.AreEqual(0, container.TakenSlots);
        Assert.IsFalse(container.ChangeTracker.HasChanges);

        using (var transaction = ItemContainerTransaction.Begin(container))
        {
            Assert.IsTrue(container.AddRange([first, second]));
            container.Sort();
            transaction.Commit();
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, container.ChangeTracker.GetSlots()!.ToArray());
        }
        Assert.AreEqual(2, container.TakenSlots);

        using (var transaction = ItemContainerTransaction.Begin(container))
        {
            Assert.IsTrue(container.TryRemoveExact(first));
            container.Clear();
            transaction.Commit();
            Assert.IsNull(container.ChangeTracker.GetSlots());
        }
        Assert.AreEqual(0, container.TakenSlots);
    }

        [TestMethod]
    public void OrdinaryMutation_OnUnenlistedContainerRemainsCommittedDuringRollback()
    {
        var enlisted = new ItemContainer(StorageType.Normal, 1);
        var unowned = new ItemContainer(StorageType.Normal, 1);
        var enlistedItem = new TestItem(16, 1);
        var standaloneItem = new TestItem(17, 1);

        using (ItemContainerTransaction.Begin(enlisted))
        {
            Assert.IsTrue(enlisted.Add(enlistedItem));
            Assert.IsTrue(unowned.Add(standaloneItem));
        }

        Assert.IsNull(enlisted[0]);
        Assert.AreSame(standaloneItem, unowned[0]);
        Assert.IsFalse(enlisted.ChangeTracker.HasChanges);
        CollectionAssert.AreEquivalent(new[] { 0 }, unowned.ChangeTracker.GetSlots()!.ToArray());
    }

    [TestMethod]
    public void Dispose_WithoutCommitRestoresReferencesCountsAndRevisionsOfAllStorage()
    {
        var source = new ItemContainer(StorageType.Normal, 2);
        var destination = new ItemContainer(StorageType.Normal, 2);
        var item = new TestItem(20, 5);
        var existing = new TestItem(20, 4);
        Assert.IsTrue(source.Add(item));
        Assert.IsTrue(destination.Add(existing));
        var sourceEnumerator = source.GetEnumerator();
        var destinationEnumerator = destination.GetEnumerator();
        using (ItemContainerTransaction.Begin(source, destination))
        {
            Assert.IsTrue(source.TryTransferTo(destination, item, 3, 0));
            source.Clear(true);
            Assert.AreEqual(7, existing.Count);
        }
        Assert.AreSame(item, source[0]);
        Assert.AreEqual(5, item.Count);
        Assert.AreSame(existing, destination[0]);
        Assert.AreEqual(4, existing.Count);
        Assert.IsTrue(sourceEnumerator.MoveNext());
        Assert.AreSame(item, sourceEnumerator.Current);
        Assert.IsTrue(destinationEnumerator.MoveNext());
        Assert.AreSame(existing, destinationEnumerator.Current);
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
            using var transaction = ItemContainerTransaction.Begin(source, destination);
            Assert.IsTrue(source.TryTransferTo(destination, item, 5));
            throw original;
        });
        Assert.AreSame(original, thrown);
        Assert.AreSame(item, source[0]);
        Assert.IsNull(destination[0]);
        Assert.AreEqual(5, item.Count);
        Assert.IsNull(Boundary(source).Transaction);
    }

        [TestMethod]
    public void Commit_PromotesChangesAndRetainsBindingUntilDispose()
    {
        var first = new ItemContainer(StorageType.Normal, 2);
        var second = new ItemContainer(StorageType.Normal, 2);
        using var transaction = ItemContainerTransaction.Begin(second, first, second);
        Assert.IsTrue(first.Add(new TestItem(23, 1)));
        Assert.IsTrue(second.Add(new TestItem(24, 1)));
        Assert.IsFalse(first.ChangeTracker.HasChanges);
        Assert.IsFalse(second.ChangeTracker.HasChanges);

        transaction.Commit();

        AssertScopeBoundAndUnlocked(transaction, first, second);
        CollectionAssert.AreEquivalent(new[] { 0 }, first.ChangeTracker.GetSlots()!.ToArray());
        CollectionAssert.AreEquivalent(new[] { 0 }, second.ChangeTracker.GetSlots()!.ToArray());
        Assert.AreEqual(1, first.TakenSlots);
        Assert.AreEqual(1, second.TakenSlots);
        Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Commit());
        transaction.Dispose();
        transaction.Dispose();
        AssertUnboundAndUnlocked(first, second);
    }

    [TestMethod]
    public void Dispose_TwiceIsInertAndCommitAfterDisposalIsInvalid()
    {
        var container = new ItemContainer(StorageType.Normal, 1);
        using var transaction = ItemContainerTransaction.Begin(container);
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
        var container = new ItemContainer(StorageType.Normal, 3);
        using (var transaction = ItemContainerTransaction.Begin(container))
        {
            Assert.IsTrue(container.Add(new TestItem(26, 1)));
            Assert.IsTrue(container.Add(new TestItem(27, 1)));
            Assert.IsFalse(container.ChangeTracker.HasChanges);
            transaction.Commit();
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, container.ChangeTracker.GetSlots()!.ToArray());
        }

        using (var transaction = ItemContainerTransaction.Begin(container))
        {
            container.Clear();
            Assert.IsTrue(container.Add(new TestItem(28, 1)));
            transaction.Commit();
            Assert.IsNull(container.ChangeTracker.GetSlots());
        }
    }

    [TestMethod]
    public void ChangeTracker_ConsumedSlotSetCannotAffectFutureChanges()
    {
        var items = new ItemContainer(StorageType.Normal, 3);
        var tracker = items.ChangeTracker;

        Assert.IsTrue(items.Add(new TestItem(261, 1)));
        items.Replace(2, new TestItem(262, 1));
        Assert.AreSame(tracker, items.ChangeTracker);
        Assert.IsTrue(tracker.HasChanges);

        var slots = tracker.GetSlots();
        CollectionAssert.AreEquivalent(new[] { 0, 2 }, slots!.ToArray());
        slots.Clear();
        Assert.IsFalse(tracker.HasChanges);
        CollectionAssert.AreEqual(Array.Empty<int>(), tracker.GetSlots()!.ToArray());

        Assert.IsTrue(items.Add(new TestItem(283, 1)));
        CollectionAssert.AreEquivalent(new[] { 1 }, tracker.GetSlots()!.ToArray());
    }

    [TestMethod]
    public void ChangeTracker_NoChangesReturnEmptySet()
    {
        var tracker = new ItemContainer(StorageType.Normal, 1).ChangeTracker;

        Assert.IsFalse(tracker.HasChanges);
        Assert.AreEqual(0, tracker.GetSlots()!.Count);
        Assert.AreEqual(0, tracker.GetSlots()!.Count);
        Assert.IsFalse(tracker.HasChanges);
    }

    [TestMethod]
    public void ChangeTracker_SuppressedMutationsDoNotBecomeConsumableChanges()
    {
        var items = new ItemContainer(StorageType.Normal, 2);
        var item = new TestItem(276, 1);
        Assert.IsTrue(items.Add(item));
        CollectionAssert.AreEquivalent(new[] { 0 }, items.ChangeTracker.GetSlots()!.ToArray());

        Assert.AreEqual(1, items.Remove(item, publishChanges: false));
        Assert.IsFalse(items.ChangeTracker.HasChanges);
        Assert.AreEqual(0, items.ChangeTracker.GetSlots()!.Count);

        Assert.IsTrue(items.Add(new TestItem(277, 1)));
        CollectionAssert.AreEquivalent(new[] { 0 }, items.ChangeTracker.GetSlots()!.ToArray());

        items.Clear(publishChanges: false);
        Assert.IsFalse(items.ChangeTracker.HasChanges);
        Assert.IsTrue(items.Add(new TestItem(278, 1)));
        CollectionAssert.AreEquivalent(new[] { 0 }, items.ChangeTracker.GetSlots()!.ToArray());
    }

    [TestMethod]
    public void ChangeTracker_ConcurrentStandaloneMutationsPreserveBothUpdates()
    {
        using var ready = new CountdownEvent(2);
        using var start = new ManualResetEventSlim();
        using var completed = new CountdownEvent(2);
        var items = new ItemContainer(StorageType.Normal, 2);
        Exception? firstFailure = null;
        Exception? secondFailure = null;
        var first = new Thread(() =>
        {
            ready.Signal();
            start.Wait();
            try { Assert.IsTrue(items.Add(new TestItem(279, 1))); }
            catch (Exception exception) { firstFailure = exception; }
            finally { completed.Signal(); }
        }) { IsBackground = true };
        var second = new Thread(() =>
        {
            ready.Signal();
            start.Wait();
            try { Assert.IsTrue(items.Add(new TestItem(280, 1))); }
            catch (Exception exception) { secondFailure = exception; }
            finally { completed.Signal(); }
        }) { IsBackground = true };

        first.Start();
        second.Start();
        try
        {
            Assert.IsTrue(ready.Wait(TimeSpan.FromSeconds(5)));
        }
        finally { start.Set(); }

        Assert.IsTrue(completed.Wait(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(first.Join(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(second.Join(TimeSpan.FromSeconds(5)));
        if (firstFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(firstFailure).Throw();
        if (secondFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(secondFailure).Throw();

        CollectionAssert.AreEquivalent(new[] { 0, 1 }, items.ChangeTracker.GetSlots()!.ToArray());
    }

    [TestMethod]
    public void ChangeTracker_FullChangeDominatesPartialChanges()
    {
        var items = new ItemContainer(StorageType.Normal, 3);
        Assert.IsTrue(items.Add(new TestItem(263, 1)));

        items.Clear();
        Assert.IsTrue(items.Add(new TestItem(264, 1)));

        Assert.IsTrue(items.ChangeTracker.HasChanges);
        Assert.IsNull(items.ChangeTracker.GetSlots());
        Assert.IsFalse(items.ChangeTracker.HasChanges);
    }

    [TestMethod]
    public void ChangeTracker_FailedMutationsDoNotRecordChanges()
    {
        var source = new ItemContainer(StorageType.Normal, 1);
        var destination = new ItemContainer(StorageType.Normal, 1);
        var sourceItem = new TestItem(265, 1);
        Assert.IsTrue(source.Add(sourceItem));
        Assert.IsTrue(destination.Add(new TestItem(266, 1)));
        source.ChangeTracker.GetSlots();
        destination.ChangeTracker.GetSlots();

        Assert.IsFalse(source.Add(new TestItem(267, 1)));
        Assert.AreEqual(0, source.Remove(new TestItem(268, 1)));
        Assert.IsFalse(source.TryTransferTo(destination, sourceItem, 1));

        Assert.AreEqual(0, source.ChangeTracker.GetSlots()!.Count);
        Assert.AreEqual(0, destination.ChangeTracker.GetSlots()!.Count);
    }

    [TestMethod]
    public void ChangeTracker_TransactionCommitPromotesChangesAndRejectsActiveConsumption()
    {
        var source = new ItemContainer(StorageType.Normal, 2);
        var destination = new ItemContainer(StorageType.Normal, 2);
        var item = new TestItem(269, 1);
        Assert.IsTrue(source.Add(item));
        source.ChangeTracker.GetSlots();

        using var transaction = ItemContainerTransaction.Begin(source, destination);
        Assert.IsTrue(source.TryTransferTo(destination, item, 1));
        Assert.IsTrue(source.Add(new TestItem(281, 1)));
        Assert.IsTrue(source.Add(new TestItem(282, 1)));
        Assert.ThrowsExactly<InvalidOperationException>(() => source.ChangeTracker.GetSlots());
        Assert.IsFalse(destination.ChangeTracker.HasChanges);

        transaction.Commit();

        CollectionAssert.AreEquivalent(new[] { 0, 1 }, source.ChangeTracker.GetSlots()!.ToArray());
        CollectionAssert.AreEquivalent(new[] { 0 }, destination.ChangeTracker.GetSlots()!.ToArray());
    }

    [TestMethod]
    public void ChangeTracker_RollbackDiscardsPendingAndPreservesEarlierCommittedChanges()
    {
        var items = new ItemContainer(StorageType.Normal, 2);
        var first = new TestItem(270, 1);
        Assert.IsTrue(items.Add(first));

        using (ItemContainerTransaction.Begin(items))
        {
            Assert.IsTrue(items.Add(new TestItem(271, 1)));
        }

        CollectionAssert.AreEquivalent(new[] { 0 }, items.ChangeTracker.GetSlots()!.ToArray());
        Assert.AreSame(first, items[0]);
        Assert.IsNull(items[1]);
        Assert.IsFalse(items.ChangeTracker.HasChanges);
    }

    [TestMethod]
    public void ChangeTracker_StandaloneTransferRecordsBothContainers()
    {
        var source = new ItemContainer(StorageType.Normal, 2);
        var destination = new ItemContainer(StorageType.Normal, 2);
        var item = new TestItem(272, 4);
        Assert.IsTrue(source.Add(item));
        source.ChangeTracker.GetSlots();

        Assert.IsTrue(source.TryTransferTo(destination, item, 4));

        CollectionAssert.AreEquivalent(new[] { 0 }, source.ChangeTracker.GetSlots()!.ToArray());
        CollectionAssert.AreEquivalent(new[] { 0 }, destination.ChangeTracker.GetSlots()!.ToArray());
    }

                [TestMethod]
    public void ChangeTracker_ForeignReadWaitsUntilCommittedScopeIsDisposed()
    {
        var items = new ItemContainer(StorageType.Normal, 1);
        var boundary = Boundary(items);
        using var readerAttempted = new ManualResetEventSlim();
        using var readerCompleted = new ManualResetEventSlim();
        HashSet<int>? readSlots = null;
        Exception? readerFailure = null;
        var transaction = ItemContainerTransaction.Begin(items);
        Assert.IsTrue(items.Add(new TestItem(276, 1)));
        transaction.Commit();

        // The domain owner consumes the committed batch while retaining the transaction binding.
        CollectionAssert.AreEquivalent(new[] { 0 }, items.ChangeTracker.GetSlots()!.ToArray());
        var reader = new Thread(() =>
        {
            try
            {
                lock (boundary.MutationLock)
                {
                    readerAttempted.Set();
                    readSlots = items.ChangeTracker.GetSlots();
                }
            }
            catch (Exception exception) { readerFailure = exception; }
            finally { readerCompleted.Set(); }
        }) { IsBackground = true };
        reader.Start();
        Assert.IsTrue(readerAttempted.Wait(TimeSpan.FromSeconds(5)));
        Monitor.Enter(boundary.MutationLock);
        try { Assert.IsFalse(readerCompleted.IsSet, "The read must remain blocked by committed transaction ownership."); }
        finally { Monitor.Exit(boundary.MutationLock); }

        transaction.Dispose();
        Assert.IsTrue(reader.Join(TimeSpan.FromSeconds(5)));
        if (readerFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(readerFailure).Throw();
        Assert.AreEqual(0, readSlots!.Count);
        AssertUnboundAndUnlocked(items);
    }

    [TestMethod]
    public void ChangeTracker_ForeignReadWaitsUntilTransactionPublicationFinishes()
    {
        using var publicationStarted = new ManualResetEventSlim();
        using var finishPublication = new ManualResetEventSlim();
        using var readerAttempted = new ManualResetEventSlim();
        using var readerFinished = new ManualResetEventSlim();
        var container = new ItemContainer(StorageType.Normal, 1);
        HashSet<int>? readerSlots = null;
        Exception? publisherFailure = null;
        Exception? readerFailure = null;

        var publisher = new Thread(() =>
        {
            try
            {
                using var transaction = ItemContainerTransaction.Begin(container);
                Assert.IsTrue(container.Add(new TestItem(135, 1)));
                transaction.Commit();
                var slots = container.ChangeTracker.GetSlots();
                CollectionAssert.AreEquivalent(new[] { 0 }, slots!.ToArray());
                publicationStarted.Set();
                Assert.IsTrue(finishPublication.Wait(TimeSpan.FromSeconds(5)));
            }
            catch (Exception exception)
            {
                publisherFailure = exception;
                publicationStarted.Set();
            }
        }) { IsBackground = true };
        publisher.Start();
        Assert.IsTrue(publicationStarted.Wait(TimeSpan.FromSeconds(5)));

        var reader = new Thread(() =>
        {
            try
            {
                readerAttempted.Set();
                readerSlots = container.ChangeTracker.GetSlots();
            }
            catch (Exception exception)
            {
                readerFailure = exception;
            }
            finally { readerFinished.Set(); }
        }) { IsBackground = true };
        reader.Start();

        try
        {
            Assert.IsTrue(readerAttempted.Wait(TimeSpan.FromSeconds(5)));
            Assert.IsTrue(SpinWait.SpinUntil(
                () => (reader.ThreadState & ThreadState.WaitSleepJoin) != 0,
                TimeSpan.FromSeconds(5)), "The foreign tracker reader should wait for transaction publication.");
            Assert.IsFalse(readerFinished.IsSet);
        }
        finally { finishPublication.Set(); }

        Assert.IsTrue(publisher.Join(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(reader.Join(TimeSpan.FromSeconds(5)));
        if (publisherFailure != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(publisherFailure).Throw();
        if (readerFailure != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(readerFailure).Throw();
        Assert.IsTrue(readerFinished.IsSet);
        Assert.AreEqual(0, readerSlots!.Count);
        AssertUnboundAndUnlocked(container);
    }

        [TestMethod]
    public void Commit_EmptyRangeDoesNotTrackChangesOrInvalidateEnumerator()
    {
        var container = new ItemContainer(StorageType.Normal, 2);
        var existing = new TestItem(31, 2);
        Assert.IsTrue(container.Add(existing));
        container.ChangeTracker.GetSlots();
        var enumerator = container.GetEnumerator();
        using var transaction = ItemContainerTransaction.Begin(container);
        Assert.IsTrue(container.AddRange(Array.Empty<IItem?>()));
        transaction.Commit();
        Assert.IsFalse(container.ChangeTracker.HasChanges);
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
        using var transaction = ItemContainerTransaction.Begin(source, destination);
        Assert.IsTrue(source.TryTransferTo(destination, item, 1));
        transaction.Commit();
        Assert.ThrowsExactly<InvalidOperationException>(() => sourceEnumerator.MoveNext());
        Assert.ThrowsExactly<InvalidOperationException>(() => destinationEnumerator.MoveNext());
    }

                            [TestMethod]
    public void RecordChanges_RejectsStorageNotOwnedByTransaction()
    {
        var enlisted = new ItemContainer(StorageType.Normal, 1);
        var unowned = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(611, 1);
        using var transaction = ItemContainerTransaction.Begin(enlisted);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            Boundary(unowned).RecordTransactionChanges(transaction, new HashSet<int> { 0 }));

        Assert.AreSame(transaction, Boundary(enlisted).Transaction);
        Assert.IsNull(Boundary(unowned).Transaction);
        Assert.IsFalse(Monitor.IsEntered(Boundary(unowned).MutationLock));
        Assert.AreEqual(0, unowned.TakenSlots);
        Assert.IsTrue(enlisted.Add(item));
        transaction.Commit();
    }

    [TestMethod]
    public void RecordChanges_RejectsEnlistedBoundaryWithoutMutationLock()
    {
        var container = new ItemContainer(StorageType.Normal, 1);
        var storage = Boundary(container);
        using var transaction = ItemContainerTransaction.Begin(container);

        Monitor.Exit(storage.MutationLock);
        try
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                storage.RecordTransactionChanges(transaction, new HashSet<int> { 0 }));
        }
        finally
        {
            Monitor.Enter(storage.MutationLock);
        }

        Assert.AreSame(transaction, storage.Transaction);
        transaction.Commit();
        transaction.Dispose();
        AssertUnboundAndUnlocked(container);
    }

    [TestMethod]
    public void Begin_RejectsInvalidArgumentsAndContributionsBeforeLocking()
    {
        var container = new ItemContainer(StorageType.Normal, 1);
        Assert.ThrowsExactly<ArgumentNullException>(() => ItemContainerTransaction.Begin(null!));
        Assert.ThrowsExactly<ArgumentException>(() => ItemContainerTransaction.Begin());
        Assert.ThrowsExactly<ArgumentNullException>(() => ItemContainerTransaction.Begin(container, null!));
        Assert.ThrowsExactly<ArgumentException>(() => ItemContainerTransaction.Begin(container, new ExternalParticipant()));
        Assert.ThrowsExactly<ArgumentException>(() => ItemContainerTransaction.Begin(container, new CompositeParticipant([])));
        Assert.ThrowsExactly<ArgumentException>(() => ItemContainerTransaction.Begin(container, new CompositeParticipant([null!])));
        AssertUnboundAndUnlocked(container);
        using var valid = ItemContainerTransaction.Begin(container);
    }

    [TestMethod]
    public void Begin_ResolvesEveryContributionBeforeAcquiringFirstLock()
    {
        var container = new ItemContainer(StorageType.Normal, 1);
        var storage = Boundary(container);
        var invalid = new CompositeParticipant([])
        {
            OnResolve = () =>
            {
                Assert.IsFalse(Monitor.IsEntered(storage.MutationLock));
                Assert.IsNull(storage.Transaction);
            }
        };
        Assert.ThrowsExactly<ArgumentException>(() => ItemContainerTransaction.Begin(container, invalid));
        AssertUnboundAndUnlocked(container);
    }

        [TestMethod]
    public void Begin_SnapshotFailureReleasesAllLocksAndNeverExposesBindings()
    {
        var first = new ItemContainer(StorageType.Normal, 1);
        var second = new ItemContainer(StorageType.Normal, 1);
        var firstItem = new TestItem(40, 3);
        var failingItem = new TestItem(41, 4);
        Assert.IsTrue(first.Add(firstItem));
        Assert.IsTrue(second.Add(failingItem));
        first.ChangeTracker.GetSlots();
        second.ChangeTracker.GetSlots();
        var original = new InvalidOperationException("Count capture failed.");
        failingItem.OnCountRead = () =>
        {
            Assert.IsTrue(Monitor.IsEntered(Boundary(first).MutationLock));
            Assert.IsTrue(Monitor.IsEntered(Boundary(second).MutationLock));
            Assert.IsNull(Boundary(first).Transaction);
            Assert.IsNull(Boundary(second).Transaction);
            throw original;
        };

        Assert.AreSame(original, Assert.ThrowsExactly<InvalidOperationException>(() => ItemContainerTransaction.Begin(first, second)));

        failingItem.OnCountRead = null;
        AssertUnboundAndUnlocked(first, second);
        Assert.AreSame(firstItem, first[0]);
        Assert.AreSame(failingItem, second[0]);
        Assert.AreEqual(3, firstItem.Count);
        Assert.AreEqual(4, failingItem.Count);
        Assert.IsFalse(first.ChangeTracker.HasChanges);
        Assert.IsFalse(second.ChangeTracker.HasChanges);
        using var fresh = ItemContainerTransaction.Begin(first, second);
    }

    [TestMethod]
    public void Begin_LocksAllParticipantsWhenInputOrderDiffersFromBoundaryOrder()
    {
        var first = new ItemContainer(StorageType.Normal, 1);
        var second = new ItemContainer(StorageType.Normal, 1);
        using var transaction = ItemContainerTransaction.Begin(second, first);
        foreach (var container in new[] { first, second })
        {
            var boundary = Boundary(container);
            Assert.IsTrue(Monitor.IsEntered(boundary.MutationLock));
            Assert.AreSame(transaction, boundary.Transaction);
        }
    }

    [TestMethod]
    public void Begin_NestedDisjointScopeSucceeds()
    {
        var first = new ItemContainer(StorageType.Normal, 1);
        var second = new ItemContainer(StorageType.Normal, 1);
        var firstItem = new TestItem(43, 1);
        var secondItem = new TestItem(44, 1);

        using (var outer = ItemContainerTransaction.Begin(first))
        {
            Assert.IsTrue(first.Add(firstItem));
            using (var inner = ItemContainerTransaction.Begin(second))
            {
                Assert.IsTrue(second.Add(secondItem));
                inner.Commit();
            }

            Assert.IsNotNull(first[0]);
            Assert.IsNotNull(second[0]);
            outer.Commit();
        }

        Assert.AreSame(firstItem, first[0]);
        Assert.AreSame(secondItem, second[0]);
        AssertUnboundAndUnlocked(first, second);
    }

    [TestMethod]
    public void Begin_NestedScopeForEnlistedBoundaryRejectsWithoutChangingOuterScope()
    {
        var container = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(45, 1);
        using var outer = ItemContainerTransaction.Begin(container);

        Assert.ThrowsExactly<InvalidOperationException>(() => ItemContainerTransaction.Begin(container));

        Assert.AreSame(outer, Boundary(container).Transaction);
        Assert.IsTrue(Monitor.IsEntered(Boundary(container).MutationLock));
        Assert.IsTrue(container.Add(item));
        Assert.AreSame(item, container[0]);
        outer.Commit();
        Assert.AreSame(outer, Boundary(container).Transaction);
        outer.Dispose();
        AssertUnboundAndUnlocked(container);
    }

        [TestMethod]
    public void Begin_RepeatedCompositeParticipantSnapshotsEachBoundaryOnce()
    {
        var first = new ItemContainer(StorageType.Normal, 1);
        var second = new ItemContainer(StorageType.Normal, 1);
        var firstItem = new TestItem(42, 1);
        var secondItem = new TestItem(43, 1);
        Assert.IsTrue(first.Add(firstItem));
        Assert.IsTrue(second.Add(secondItem));
        first.ChangeTracker.GetSlots();
        second.ChangeTracker.GetSlots();
        var firstCountReads = 0;
        var secondCountReads = 0;
        firstItem.OnCountRead = () => firstCountReads++;
        secondItem.OnCountRead = () => secondCountReads++;
        var composite = new CompositeParticipant([Boundary(first), Boundary(second)]);

        using var transaction = ItemContainerTransaction.Begin(first, composite, composite, second);

        Assert.AreEqual(1, firstCountReads);
        Assert.AreEqual(1, secondCountReads);
        firstItem.OnCountRead = null;
        secondItem.OnCountRead = null;
        first.Clear();
        Assert.IsTrue(second.TryRemoveExact(secondItem));
        Assert.IsTrue(second.Add(firstItem));
        transaction.Commit();
        Assert.IsNull(first[0]);
        Assert.AreSame(firstItem, second[0]);
        Assert.IsNull(first.ChangeTracker.GetSlots());
        CollectionAssert.AreEquivalent(new[] { 0 }, second.ChangeTracker.GetSlots()!.ToArray());
        transaction.Dispose();
        AssertUnboundAndUnlocked(first, second);
    }

        [TestMethod]
    public void Begin_DifferentBoundariesForSameStorageRejectBeforeLocking()
    {
        var container = new ItemContainer(StorageType.Normal, 1);
        var storage = Boundary(container);
        var first = new CompositeParticipant([Boundary(container)]);
        var secondBoundary = new ItemContainerMutationBoundary(storage.Storage);
        var second = new CompositeParticipant([secondBoundary]);

        Assert.ThrowsExactly<ArgumentException>(() => ItemContainerTransaction.Begin(first, second));

        Assert.IsNull(storage.Transaction);
        Assert.IsFalse(Monitor.IsEntered(storage.MutationLock));
        var acquired = false;
        OnOtherThread(() =>
        {
            acquired = Monitor.TryEnter(storage.MutationLock);
            if (acquired) Monitor.Exit(storage.MutationLock);
        });
        Assert.IsTrue(acquired);
    }

                    [TestMethod]
    public void ItemContainerTransaction_BalancesCurrentThreadOwnershipOnCommitAndRollback()
    {
        var container = new ItemContainer(StorageType.Normal, 2);
        Assert.IsFalse(ItemContainerMutationBoundary.CurrentThreadOwnsStorage);

        using (ItemContainerTransaction.Begin(container))
        {
            Assert.IsTrue(ItemContainerMutationBoundary.CurrentThreadOwnsStorage);
            Assert.IsTrue(container.Add(new TestItem(6051, 1)));
        }

        Assert.IsFalse(ItemContainerMutationBoundary.CurrentThreadOwnsStorage);
        Assert.AreEqual(0, container.TakenSlots);

        using (var transaction = ItemContainerTransaction.Begin(container))
        {
            Assert.IsTrue(ItemContainerMutationBoundary.CurrentThreadOwnsStorage);
            transaction.Commit();
        }

        Assert.IsFalse(ItemContainerMutationBoundary.CurrentThreadOwnsStorage);
    }

        [TestMethod]
    public void TransactionBegin_WhenOwningStorageRejectsContendedAdditionalLocksAndReleasesPrefix()
    {
        var owned = new ItemContainer(StorageType.Normal, 1);
        var prefix = new ItemContainer(StorageType.Normal, 1);
        var contended = new ItemContainer(StorageType.Normal, 1);
        var prefixBoundary = Boundary(prefix);
        var contendedBoundary = Boundary(contended);
        Assert.IsTrue(prefixBoundary.LockOrder < contendedBoundary.LockOrder);

        AssertContendedAdditionalAcquisitionRejected(owned, prefixBoundary, contendedBoundary,
            () => ItemContainerTransaction.Begin(prefix, contended),
            "A thread that owns storage must reject a contended additional transaction without waiting.");
        AssertUnboundAndUnlocked(owned, prefix, contended);
    }

    [TestMethod]
    public void StandaloneTransfer_WhenOwningStorageRejectsContendedParticipantAndReleasesPrefix()
    {
        var owned = new ItemContainer(StorageType.Normal, 1);
        var source = new ItemContainer(StorageType.Normal, 1);
        var destination = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(6121, 1);
        Assert.IsTrue(source.Add(item));
        var boundaries = new[] { Boundary(source), Boundary(destination) }.OrderBy(boundary => boundary.LockOrder).ToArray();
        var prefixBoundary = boundaries[0];
        var contendedBoundary = boundaries[1];

        AssertContendedAdditionalAcquisitionRejected(owned, prefixBoundary, contendedBoundary,
            () => Assert.IsFalse(source.TryTransferTo(destination, item, 1)),
            "A standalone transfer must reject a contended participant instead of waiting while its caller owns storage.",
            expectsException: false);
        Assert.AreSame(item, source[0]);
        Assert.IsNull(destination[0]);
        AssertUnboundAndUnlocked(owned, source, destination);
    }

        [TestMethod]
    public void ContainerMutation_RejectsMutationWhileCommittedScopeRemainsBound()
    {
        var container = new ItemContainer(StorageType.Normal, 2);
        using var transaction = ItemContainerTransaction.Begin(container);
        Assert.IsTrue(container.Add(new TestItem(605, 1)));

        transaction.Commit();

        Assert.AreSame(transaction, Boundary(container).Transaction);
        Assert.ThrowsExactly<InvalidOperationException>(() => container.Add(new TestItem(606, 1)));
        Assert.AreSame(transaction, Boundary(container).Transaction);
        Assert.AreEqual(1, container.TakenSlots);
        Assert.IsTrue(container.ChangeTracker.HasChanges);
    }

    [TestMethod]
    public void Transfer_PartialEnlistmentRejectsWithoutLockingMissingStorageOrStartingNestedScope()
    {
        var first = new ItemContainer(StorageType.Normal, 1);
        var second = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(43, 1);
        Assert.IsTrue(first.Add(item));
        using var transaction = ItemContainerTransaction.Begin(first);
        Assert.ThrowsExactly<InvalidOperationException>(() => first.TryTransferTo(second, item, 1));
        Assert.AreSame(transaction, Boundary(first).Transaction);
        Assert.AreSame(item, first[0]);
        Assert.IsNull(second[0]);
        AssertUnboundAndUnlocked(second);
        Assert.ThrowsExactly<InvalidOperationException>(() => ItemContainerTransaction.Begin(first, second));
        AssertUnboundAndUnlocked(second);
    }

    [TestMethod]
    public void Transfer_DestinationOnlyEnlistmentRejectsAndLeavesCallerTransactionUsable()
    {
        var source = new ItemContainer(StorageType.Normal, 1);
        var destination = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(45, 1);
        Assert.IsTrue(source.Add(item));
        using var transaction = ItemContainerTransaction.Begin(destination);

        Assert.ThrowsExactly<InvalidOperationException>(() => source.TryTransferTo(destination, item, 1));

        Assert.IsNull(Boundary(source).Transaction);
        Assert.AreSame(transaction, Boundary(destination).Transaction);
        Assert.AreSame(item, source[0]);
        Assert.IsNull(destination[0]);
        Assert.IsTrue(Monitor.IsEntered(Boundary(destination).MutationLock));
        transaction.Commit();
        transaction.Dispose();
        AssertUnboundAndUnlocked(source, destination);
    }

    [TestMethod]
    public void Transfer_DifferentActiveScopesRejectWithoutMutation()
    {
        var first = new ItemContainer(StorageType.Normal, 1);
        var second = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(44, 1);
        Assert.IsTrue(first.Add(item));
        using var firstScope = ItemContainerTransaction.Begin(first);
        using var secondScope = ItemContainerTransaction.Begin(second);
        Assert.ThrowsExactly<InvalidOperationException>(() => first.TryTransferTo(second, item, 1));
        Assert.AreSame(firstScope, Boundary(first).Transaction);
        Assert.AreSame(secondScope, Boundary(second).Transaction);
        Assert.AreSame(item, first[0]);
        Assert.IsNull(second[0]);
    }

    [TestMethod]
    public void Transfer_CurrentTransactionDiscoveryDoesNotAuthorizeMutationWithoutItsLock()
    {
        var source = new ItemContainer(StorageType.Normal, 1);
        var destination = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(451, 1);
        Assert.IsTrue(source.Add(item));
        var sourceBoundary = Boundary(source);
        var sourceLock = sourceBoundary.MutationLock;
        using var transaction = ItemContainerTransaction.Begin(source, destination);

        Monitor.Exit(sourceLock);
        try
        {
            Assert.AreSame(transaction, sourceBoundary.CurrentTransaction);
            Assert.ThrowsExactly<InvalidOperationException>(() => source.TryTransferTo(destination, item, 1));
        }
        finally { Monitor.Enter(sourceLock); }

        Assert.AreSame(transaction, sourceBoundary.Transaction);
        Assert.AreSame(item, source[0]);
        Assert.IsNull(destination[0]);
        transaction.Commit();
        transaction.Dispose();
        AssertUnboundAndUnlocked(source, destination);
    }

    [TestMethod]
    public void Transfer_WithoutTransactionUsesStandaloneBoundaryOwnership()
    {
        var source = new ItemContainer(StorageType.Normal, 1);
        var destination = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(42, 1);
        Assert.IsTrue(source.Add(item));

        Assert.IsTrue(source.TryTransferTo(destination, item, 1));

        Assert.IsNull(source[0]);
        Assert.AreSame(item, destination[0]);
        AssertUnboundAndUnlocked(source, destination);
    }

        [TestMethod]
    public void Transfer_ToSameContainerKeepsExistingNoMutationBehavior()
    {
        var source = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(46, 1);
        Assert.IsTrue(source.Add(item));
        source.ChangeTracker.GetSlots();

        Assert.IsFalse(source.TryTransferTo(source, item, 1));

        Assert.AreSame(item, source[0]);
        Assert.AreEqual(1, item.Count);
        Assert.IsFalse(source.ChangeTracker.HasChanges);
        AssertUnboundAndUnlocked(source);
    }

    [TestMethod]
    public void Transfer_BothLocksHeldWithoutTransactionStillRejects()
    {
        var source = new ItemContainer(StorageType.Normal, 1);
        var destination = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(47, 1);
        Assert.IsTrue(source.Add(item));
        var sourceLock = Boundary(source).MutationLock;
        var destinationLock = Boundary(destination).MutationLock;

        lock (sourceLock)
        lock (destinationLock)
            Assert.ThrowsExactly<InvalidOperationException>(() => source.TryTransferTo(destination, item, 1));

        Assert.AreSame(item, source[0]);
        Assert.IsNull(destination[0]);
        AssertUnboundAndUnlocked(source, destination);
    }

    [TestMethod]
    public void StorageTransfer_WithoutTransactionRejectsBeforeMutation()
    {
        var source = new ItemContainer(StorageType.Normal, 1);
        var destination = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(48, 1);
        Assert.IsTrue(source.Add(item));
        var sourceStorage = Boundary(source).Storage;
        var destinationStorage = Boundary(destination).Storage;
        var sourceRevision = sourceStorage.MutationRevision;
        var destinationRevision = destinationStorage.MutationRevision;

        Assert.ThrowsExactly<InvalidOperationException>(() => Boundary(source).TryTransferTo(
            Boundary(destination), item, 1, 0, -1, null));

        Assert.AreSame(item, source[0]);
        Assert.IsNull(destination[0]);
        Assert.AreEqual(sourceRevision, sourceStorage.MutationRevision);
        Assert.AreEqual(destinationRevision, destinationStorage.MutationRevision);
        AssertUnboundAndUnlocked(source, destination);
    }

    [TestMethod]
    public void StorageTransfer_DifferentActiveTransactionsRejectBeforeMutation()
    {
        var source = new ItemContainer(StorageType.Normal, 1);
        var destination = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(49, 1);
        Assert.IsTrue(source.Add(item));
        var sourceStorage = Boundary(source).Storage;
        var destinationStorage = Boundary(destination).Storage;
        var sourceRevision = sourceStorage.MutationRevision;
        var destinationRevision = destinationStorage.MutationRevision;
        using var sourceTransaction = ItemContainerTransaction.Begin(source);
        using var destinationTransaction = ItemContainerTransaction.Begin(destination);

        Assert.ThrowsExactly<InvalidOperationException>(() => Boundary(source).TryTransferTo(
            Boundary(destination), item, 1, 0, -1, null));

        Assert.AreSame(sourceTransaction, Boundary(source).Transaction);
        Assert.AreSame(destinationTransaction, Boundary(destination).Transaction);
        Assert.AreSame(item, source[0]);
        Assert.IsNull(destination[0]);
        Assert.AreEqual(sourceRevision, sourceStorage.MutationRevision);
        Assert.AreEqual(destinationRevision, destinationStorage.MutationRevision);
    }

    [TestMethod]
    public void StorageTransfer_SameTransactionReportsSlotsAndDisposeRestoresState()
    {
        var source = new ItemContainer(StorageType.Normal, 1);
        var destination = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(50, 4);
        Assert.IsTrue(source.Add(item));
        var sourceStorage = Boundary(source).Storage;
        var destinationStorage = Boundary(destination).Storage;
        var sourceRevision = sourceStorage.MutationRevision;
        var destinationRevision = destinationStorage.MutationRevision;

        using (ItemContainerTransaction.Begin(source, destination))
        {
            // Storage owns the exact item algorithm; the transaction owns synchronization and rollback.
            Assert.IsTrue(sourceStorage.TryTransferTo(destinationStorage, item, 4, 0, -1, null,
                out var sourceSlots, out var destinationSlots));

            Assert.IsTrue(sourceSlots.SetEquals([0]));
            Assert.IsTrue(destinationSlots.SetEquals([0]));
            Assert.IsNull(source[0]);
            Assert.AreSame(item, destination[0]);
            Assert.AreEqual(4, destination[0]!.Count);
            Assert.AreEqual(sourceRevision + 1, sourceStorage.MutationRevision);
            Assert.AreEqual(destinationRevision + 1, destinationStorage.MutationRevision);
        }

        Assert.AreSame(item, source[0]);
        Assert.AreEqual(4, source[0]!.Count);
        Assert.IsNull(destination[0]);
        Assert.AreEqual(sourceRevision, sourceStorage.MutationRevision);
        Assert.AreEqual(destinationRevision, destinationStorage.MutationRevision);
    }

    [TestMethod]
    public void StorageTransfer_WrongThreadRejectsWithoutEndingTransaction()
    {
        var source = new ItemContainer(StorageType.Normal, 1);
        var destination = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(51, 1);
        Assert.IsTrue(source.Add(item));
        var sourceStorage = Boundary(source).Storage;
        var destinationStorage = Boundary(destination).Storage;
        using var transaction = ItemContainerTransaction.Begin(source, destination);

        OnOtherThread(() => Assert.ThrowsExactly<InvalidOperationException>(() => Boundary(source).TryTransferTo(
            Boundary(destination), item, 1, 0, -1, null)));

        Assert.AreSame(transaction, Boundary(source).Transaction);
        Assert.AreSame(transaction, Boundary(destination).Transaction);
        Assert.AreSame(item, source[0]);
        Assert.IsNull(destination[0]);
        transaction.Dispose();
        AssertUnboundAndUnlocked(source, destination);
    }

    [TestMethod]
    public void Scope_WrongThreadCommitAndDisposeRejectWithoutEndingScope()
    {
        var first = new ItemContainer(StorageType.Normal, 2);
        var second = new ItemContainer(StorageType.Normal, 2);
        var item = new TestItem(45, 1);
        Assert.IsTrue(first.Add(item));
        using var transaction = ItemContainerTransaction.Begin(first, second);
        OnOtherThread(() =>
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Commit());
            Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Dispose());
        });
        Assert.AreSame(transaction, Boundary(first).Transaction);
        Assert.AreSame(item, first[0]);
        Assert.AreEqual(1, first.TakenSlots);
        Assert.AreEqual(0, second.TakenSlots);
        transaction.Commit();
        OnOtherThread(() => Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Dispose()));
    }

        [TestMethod]
    public void Commit_ForeignBeginWaitsUntilOwnerPublicationCompletes()
    {
        using var publicationStarted = new ManualResetEventSlim();
        using var finishPublication = new ManualResetEventSlim();
        using var publicationFinished = new ManualResetEventSlim();
        using var contenderAttempted = new ManualResetEventSlim();
        using var contenderEntered = new ManualResetEventSlim();
        var container = new ItemContainer(StorageType.Normal, 2);
        var itemA = new TestItem(140, 1);
        var itemB = new TestItem(141, 1);
        var transaction = ItemContainerTransaction.Begin(container);
        Assert.IsTrue(container.Add(itemA));
        transaction.Commit();
        CollectionAssert.AreEquivalent(new[] { 0 }, container.ChangeTracker.GetSlots()!.ToArray());

        var publisher = new Thread(() =>
        {
            publicationStarted.Set();
            try { Assert.IsTrue(finishPublication.Wait(TimeSpan.FromSeconds(5))); }
            finally { publicationFinished.Set(); }
        }) { IsBackground = true };
        publisher.Start();
        Assert.IsTrue(publicationStarted.Wait(TimeSpan.FromSeconds(5)));

        Exception? contenderFailure = null;
        var contender = new Thread(() =>
        {
            try
            {
                contenderAttempted.Set();
                using var scope = ItemContainerTransaction.Begin(container);
                contenderEntered.Set();
                Assert.IsTrue(container.Add(itemB));
                scope.Commit();
            }
            catch (Exception exception) { contenderFailure = exception; }
        }) { IsBackground = true };
        contender.Start();
        try
        {
            Assert.IsTrue(contenderAttempted.Wait(TimeSpan.FromSeconds(5)));
            Assert.IsFalse(contenderEntered.IsSet);
            Assert.AreSame(itemA, container[0]);
            Assert.IsNull(container[1]);
        }
        finally { finishPublication.Set(); }

        Assert.IsTrue(publisher.Join(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(publicationFinished.IsSet);
        transaction.Dispose();
        Assert.IsTrue(contender.Join(TimeSpan.FromSeconds(5)));
        if (contenderFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(contenderFailure).Throw();
        Assert.IsTrue(contenderEntered.IsSet);
        Assert.AreSame(itemA, container[0]);
        Assert.AreSame(itemB, container[1]);
        AssertUnboundAndUnlocked(container);
    }

        [TestMethod]
    public void Commit_RejectsMutationUntilCommittedScopeIsDisposedButAllowsDisjointScope()
    {
        var container = new ItemContainer(StorageType.Normal, 2);
        var transaction = ItemContainerTransaction.Begin(container);
        Assert.IsTrue(container.Add(new TestItem(144, 1)));
        transaction.Commit();

        Assert.ThrowsExactly<InvalidOperationException>(() => ItemContainerTransaction.Begin(container));
        Assert.ThrowsExactly<InvalidOperationException>(() => container.Add(new TestItem(145, 1)));
        var unrelated = new ItemContainer(StorageType.Normal, 1);
        using (var disjoint = ItemContainerTransaction.Begin(unrelated))
        {
            Assert.IsTrue(unrelated.Add(new TestItem(146, 1)));
            disjoint.Commit();
        }
        Assert.AreEqual(1, container.TakenSlots);

        transaction.Dispose();
        using var later = ItemContainerTransaction.Begin(container);
        Assert.IsTrue(container.Add(new TestItem(147, 1)));
        later.Commit();
        Assert.AreEqual(2, container.TakenSlots);
        later.Dispose();
        AssertUnboundAndUnlocked(container);
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
        using var owner = ItemContainerTransaction.Begin(first, second);
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
                foreach (var boundary in required)
                {
                    Assert.IsTrue(Monitor.IsEntered(boundary.MutationLock));
                    Assert.AreSame(contender, boundary.Transaction);
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
            Assert.AreSame(owner, Boundary(second).Transaction);
        }
        finally
        {
            if (commit) owner.Commit();
            owner.Dispose();
        }
        Assert.IsTrue(worker.Join(TimeSpan.FromSeconds(5)), "Ordered overlapping scopes must not deadlock.");
        if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        AssertUnboundAndUnlocked(first, second, third);
    }

        [TestMethod]
    public void TransactionMarkerExposesNoMechanicsAndMutationBoundaryIsInternal()
    {
        Assert.AreEqual(0, typeof(IItemTransactional).GetMembers().Length);
        Assert.IsFalse(typeof(ItemContainerMutationBoundary).IsPublic);
    }

    [TestMethod]
    public void StandaloneMutation_PublishesOnceWhenOutermostScopeDisposes()
    {
        var (container, owner) = CreateOwnedContainer("container", 3);
        using (container.BeginMutation())
        {
            Assert.IsTrue(container.Add(new TestItem(201, 1)));
            Assert.IsTrue(container.Add(new TestItem(202, 1)));
            Assert.AreEqual(0, owner.EffectsCalls);
            Assert.AreEqual(0, owner.PublicationCalls);
        }

        CollectionAssert.AreEqual(new[] { "effects:container", "publish:container" }, owner.Timeline);
        CollectionAssert.AreEquivalent(new[] { 0, 1 }, owner.PublishedSlots!.ToArray());
        Assert.IsFalse(container.ChangeTracker.HasChanges);
        AssertUnboundAndUnlocked(container);
    }

    [TestMethod]
    public void StandaloneMutation_NoRecordedChangesDoesNotPublish()
    {
        var (container, owner) = CreateOwnedContainer("container", 1, [new TestItem(203, 1)]);

        Assert.IsFalse(container.Add(new TestItem(204, 1)));
        container.Clear(publishChanges: false);

        Assert.AreEqual(0, owner.EffectsCalls);
        Assert.AreEqual(0, owner.PublicationCalls);
        Assert.IsNull(container[0]);
        AssertUnboundAndUnlocked(container);
    }

    [TestMethod]
    public void StandaloneMutation_RetainsLogicalOwnershipUntilPublicationFinishes()
    {
        using var publicationEntered = new ManualResetEventSlim();
        using var finishPublication = new ManualResetEventSlim();
        using var contenderStarted = new ManualResetEventSlim();
        using var contenderFinished = new ManualResetEventSlim();
        var (container, owner) = CreateOwnedContainer("container", 2);
        owner.OnPublish = () =>
        {
            Assert.IsFalse(Monitor.IsEntered(Boundary(container).MutationLock));
            lock (Boundary(container).MutationLock)
            {
                Assert.IsTrue(Boundary(container).HasStandalonePublicationOwner);
                Assert.IsTrue(Boundary(container).IsStandalonePublicationOwnedByCurrentThread);
            }
            publicationEntered.Set();
            Assert.IsTrue(finishPublication.Wait(TimeSpan.FromSeconds(5)));
        };

        Exception? firstFailure = null;
        var first = new Thread(() =>
        {
            try { Assert.IsTrue(container.Add(new TestItem(215, 1))); }
            catch (Exception exception) { firstFailure = exception; }
        }) { IsBackground = true };
        first.Start();
        Assert.IsTrue(publicationEntered.Wait(TimeSpan.FromSeconds(5)));

        Exception? contenderFailure = null;
        var contender = new Thread(() =>
        {
            try
            {
                contenderStarted.Set();
                Assert.IsTrue(container.Add(new TestItem(216, 1)));
            }
            catch (Exception exception) { contenderFailure = exception; }
            finally { contenderFinished.Set(); }
        }) { IsBackground = true };
        contender.Start();
        try
        {
            Assert.IsTrue(contenderStarted.Wait(TimeSpan.FromSeconds(5)));
            Assert.IsFalse(contenderFinished.IsSet);
            Assert.IsFalse(Monitor.IsEntered(Boundary(container).MutationLock));
        }
        finally { finishPublication.Set(); }

        Assert.IsTrue(first.Join(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(contender.Join(TimeSpan.FromSeconds(5)));
        if (firstFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(firstFailure).Throw();
        if (contenderFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(contenderFailure).Throw();
        Assert.AreEqual(2, owner.PublicationCalls);
        AssertUnboundAndUnlocked(container);
    }

    [TestMethod]
    public void Commit_CompletesEveryOwnerBeforePublishingInParticipantOrder()
    {
        var timeline = new List<string>();
        var (first, firstOwner) = CreateOwnedContainer("first", 2, [new TestItem(205, 1)], timeline);
        var (second, secondOwner) = CreateOwnedContainer("second", 2, [new TestItem(206, 1)], timeline);
        var transaction = ItemContainerTransaction.Begin(first, second);
        Assert.IsTrue(first.Add(new TestItem(207, 1)));
        Assert.IsTrue(second.Add(new TestItem(208, 1)));

        transaction.Commit();

        CollectionAssert.AreEqual(new[]
        {
            "effects:first", "effects:second", "publish:first", "publish:second"
        }, timeline);
        Assert.AreEqual(1, firstOwner.PublicationCalls);
        Assert.AreEqual(1, secondOwner.PublicationCalls);
        Assert.IsFalse(Monitor.IsEntered(Boundary(first).MutationLock));
        Assert.IsFalse(Monitor.IsEntered(Boundary(second).MutationLock));
        Assert.AreSame(transaction, Boundary(first).Transaction);
        Assert.AreSame(transaction, Boundary(second).Transaction);
        transaction.Dispose();
        AssertUnboundAndUnlocked(first, second);
    }

    [TestMethod]
    public void Commit_PublicationFailureLeavesStorageCommittedAndDoesNotPublishLaterOwners()
    {
        var (first, firstOwner) = CreateOwnedContainer("first", 2, [new TestItem(209, 1)]);
        var (second, secondOwner) = CreateOwnedContainer("second", 2, [new TestItem(210, 1)]);
        firstOwner.OnPublish = () => throw new InvalidOperationException("publisher failed");
        var transaction = ItemContainerTransaction.Begin(first, second);
        var added = new TestItem(211, 1);
        Assert.IsTrue(first.Add(added));
        Assert.IsTrue(second.Add(new TestItem(212, 1)));

        Assert.ThrowsExactly<InvalidOperationException>(transaction.Commit);
        transaction.Dispose();

        Assert.AreSame(added, first[1]);
        Assert.AreEqual(0, secondOwner.PublicationCalls);
        Assert.AreEqual(1, firstOwner.DiscardCalls);
        Assert.AreEqual(1, secondOwner.DiscardCalls);
        Assert.IsFalse(second.ChangeTracker.HasChanges);
        AssertUnboundAndUnlocked(first, second);
    }

    [TestMethod]
    public void StandaloneTransfer_CompletesBothOwnersAfterReleasingLocks()
    {
        var timeline = new List<string>();
        var item = new TestItem(213, 2);
        var (source, sourceOwner) = CreateOwnedContainer("source", 2, [item], timeline);
        var (destination, destinationOwner) = CreateOwnedContainer("destination", 2, [], timeline);
        Action<ItemContainer> verifyOwnership = container =>
        {
            Assert.IsFalse(Monitor.IsEntered(Boundary(container).MutationLock));
            lock (Boundary(container).MutationLock)
            {
                Assert.IsTrue(Boundary(container).HasStandalonePublicationOwner);
                Assert.IsTrue(Boundary(container).IsStandalonePublicationOwnedByCurrentThread);
            }
        };
        sourceOwner.OnEffects = () => { verifyOwnership(source); verifyOwnership(destination); };
        destinationOwner.OnEffects = () => { verifyOwnership(source); verifyOwnership(destination); };

        Assert.IsTrue(source.TryTransferTo(destination, item, 2));

        CollectionAssert.AreEqual(new[]
        {
            "effects:source", "effects:destination", "publish:source", "publish:destination"
        }, timeline);
        Assert.IsNull(source[0]);
        Assert.AreSame(item, destination[0]);
        AssertUnboundAndUnlocked(source, destination);
    }

    [TestMethod]
    public void StandaloneTransfer_PublicationFailureReleasesBothBoundaries()
    {
        var item = new TestItem(214, 1);
        var (source, sourceOwner) = CreateOwnedContainer("source", 1, [item]);
        var (destination, destinationOwner) = CreateOwnedContainer("destination", 1, []);
        sourceOwner.OnPublish = () => throw new InvalidOperationException("publisher failed");

        Assert.ThrowsExactly<InvalidOperationException>(() => source.TryTransferTo(destination, item, 1));

        Assert.IsNull(source[0]);
        Assert.AreSame(item, destination[0]);
        Assert.AreEqual(0, destinationOwner.PublicationCalls);
        AssertUnboundAndUnlocked(source, destination);
    }

    private static (ItemContainer Container, TestCompletionOwner Owner) CreateOwnedContainer(
        string name, int capacity, IEnumerable<IItem>? items = null, List<string>? timeline = null)
    {
        var owner = new TestCompletionOwner(name, timeline ?? []);
        var container = items == null
            ? new ItemContainer(StorageType.Normal, capacity, -1, owner)
            : new ItemContainer(StorageType.Normal, items, capacity, -1, owner);
        owner.Container = container;
        return (container, owner);
    }

    private sealed class TestCompletionOwner(string name, List<string> timeline) : IItemContainerCompletionOwner
    {
        internal ItemContainer Container { get; set; } = null!;
        internal int EffectsCalls { get; private set; }
        internal int PublicationCalls { get; private set; }
        internal int DiscardCalls { get; private set; }
        internal HashSet<int>? PublishedSlots { get; private set; }
        internal Action? OnEffects { get; set; }
        internal Action? OnPublish { get; set; }
        internal List<string> Timeline => timeline;
        public void DiscardPendingCompletion() => DiscardCalls++;
        public void CompleteCommittedEffects()
        {
            EffectsCalls++;
            timeline.Add($"effects:{name}");
            Assert.IsFalse(Monitor.IsEntered(Boundary(Container).MutationLock));
            OnEffects?.Invoke();
        }
        public void PublishCommittedChanges()
        {
            PublicationCalls++;
            timeline.Add($"publish:{name}");
            PublishedSlots = Container.ChangeTracker.GetSlots();
            OnPublish?.Invoke();
        }
    }

    [TestMethod]
    public void Transaction_StoresNoExecutableCallbacksOrCallbackCollections()
    {
        static bool ContainsDelegate(Type type) => typeof(Delegate).IsAssignableFrom(type) ||
            type.HasElementType && ContainsDelegate(type.GetElementType()!) ||
            type.IsGenericType && type.GetGenericArguments().Any(ContainsDelegate);
        foreach (var field in typeof(ItemContainerTransaction).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            Assert.IsFalse(ContainsDelegate(field.FieldType), $"Executable callback field: {field.Name}");
    }

    private static ItemContainerMutationBoundary Boundary(ItemContainer container) =>
        ((IItemTransactionSource)container).Boundaries.Single();

    private static void AssertContendedAdditionalAcquisitionRejected(
        ItemContainer owned,
        ItemContainerMutationBoundary prefixBoundary,
        ItemContainerMutationBoundary contendedBoundary,
        Action operation,
        string rejectionMessage,
        bool expectsException = true)
    {
        using var contendedLockHeld = new ManualResetEventSlim();
        using var releaseContendedLock = new ManualResetEventSlim();
        using var rejectedAttemptCompleted = new ManualResetEventSlim();
        Exception? ownerFailure = null;
        var lockOwner = new Thread(() =>
        {
            lock (contendedBoundary.MutationLock)
            {
                contendedLockHeld.Set();
                Assert.IsTrue(releaseContendedLock.Wait(TimeSpan.FromSeconds(5)));
            }
        }) { IsBackground = true };
        lockOwner.Start();
        Assert.IsTrue(contendedLockHeld.Wait(TimeSpan.FromSeconds(5)));

        var transactionOwner = new Thread(() =>
        {
            try
            {
                using var transaction = ItemContainerTransaction.Begin(owned);
                Assert.IsTrue(ItemContainerMutationBoundary.CurrentThreadOwnsStorage);
                if (expectsException) Assert.ThrowsExactly<InvalidOperationException>(operation);
                else operation();
                Assert.IsTrue(ItemContainerMutationBoundary.CurrentThreadOwnsStorage);
                rejectedAttemptCompleted.Set();
                transaction.Commit();
            }
            catch (Exception exception) { ownerFailure = exception; }
        }) { IsBackground = true };
        transactionOwner.Start();
        try
        {
            Assert.IsTrue(rejectedAttemptCompleted.Wait(TimeSpan.FromSeconds(5)), rejectionMessage);
            Assert.IsTrue(Monitor.TryEnter(prefixBoundary.MutationLock),
                "The acquired lock prefix must be released after contention is rejected.");
            Monitor.Exit(prefixBoundary.MutationLock);
        }
        finally { releaseContendedLock.Set(); }

        Assert.IsTrue(lockOwner.Join(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(transactionOwner.Join(TimeSpan.FromSeconds(5)));
        if (ownerFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ownerFailure).Throw();
    }

    private static void AssertUnboundAndUnlocked(params ItemContainer[] containers)
    {
        foreach (var container in containers)
        {
            var storage = Boundary(container);
            lock (storage.MutationLock)
            {
                Assert.IsNull(storage.Transaction);
                Assert.IsFalse(storage.HasStandalonePublicationOwner);
            }
            AssertMutationLockReleased(storage);
        }
    }

    private static void AssertStandaloneOwnedAndUnlocked(params ItemContainer[] containers)
    {
        foreach (var container in containers)
        {
            var storage = Boundary(container);
            lock (storage.MutationLock)
            {
                Assert.IsNull(storage.Transaction);
                Assert.IsTrue(storage.HasStandalonePublicationOwner);
                Assert.IsTrue(storage.IsStandalonePublicationOwnedByCurrentThread);
            }
            AssertMutationLockReleased(storage);
        }
    }

    private static void AssertScopeBoundAndUnlocked(ItemContainerTransaction transaction, params ItemContainer[] containers)
    {
        foreach (var container in containers)
        {
            var storage = Boundary(container);
            lock (storage.MutationLock)
            {
                Assert.AreSame(transaction, storage.Transaction);
                Assert.IsFalse(storage.HasStandalonePublicationOwner);
            }
            AssertMutationLockReleased(storage);
        }
    }

    private static void AssertMutationLockReleased(ItemContainerMutationBoundary storage)
    {
        Assert.IsFalse(Monitor.IsEntered(storage.MutationLock));
        var acquired = false;
        OnOtherThread(() =>
        {
            acquired = Monitor.TryEnter(storage.MutationLock);
            if (acquired) Monitor.Exit(storage.MutationLock);
        });
        Assert.IsTrue(acquired);
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

    private sealed class ExternalParticipant : IItemTransactional { }
    private sealed class CompositeParticipant(IReadOnlyList<ItemContainerMutationBoundary> boundaries) : IItemTransactionSource
    {
        public Action? OnResolve { get; init; }
        public IReadOnlyList<ItemContainerMutationBoundary> Boundaries { get { OnResolve?.Invoke(); return boundaries; } }
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
            Assert.AreEqual(typeof(IReadOnlyItemContainer), contract.GetProperty("Items")!.PropertyType, contract.Name);
        }

        Assert.IsTrue(typeof(IItemTransactional).IsAssignableFrom(typeof(IItemContainer)));
        Assert.IsNull(typeof(IItemContainer).GetProperty("Mutations"));
        Assert.IsTrue(typeof(IItemTransactional).IsAssignableFrom(typeof(IMoneyPouchContainer)));
        Assert.IsNull(typeof(IMoneyPouchContainer).GetProperty("Mutations"));
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

        Assert.AreEqual(typeof(IReadOnlyItemContainer), typeof(IEquipmentContainer).GetProperty("Items")!.PropertyType);
        Assert.IsNull(typeof(IEquipmentContainer).GetProperty("Mutations"));
        Assert.IsFalse(typeof(IMoneyPouchContainer).GetProperty("Items") is not null);
        Assert.IsNull(typeof(IMoneyPouchContainer).GetProperty("Mutations"));
    }

    [TestMethod]
    public void ContainerInterfacesSeparateReadOnlyAndMutationCapabilities()
    {
        Assert.IsTrue(typeof(IReadOnlyItemContainer).GetInterfaces().Contains(typeof(IContainer<IItem?>)));
        Assert.IsTrue(typeof(IItemContainer).GetInterfaces().Contains(typeof(IReadOnlyItemContainer)));

        var readOnlyMembers = typeof(IReadOnlyItemContainer).GetMethods().Select(method => method.Name).ToHashSet();
        foreach (var mutationMember in new[] { "Add", "Remove", "TryRemoveExact", "Replace", "Swap", "Move", "AddRange", "Sort", "Clear" })
        {
            Assert.IsFalse(readOnlyMembers.Contains(mutationMember), mutationMember);
        }

        Assert.IsTrue(typeof(IItemTransactional).IsAssignableFrom(typeof(IItemContainer)));
        Assert.IsFalse(typeof(IEquipmentContainer).GetInterfaces().Contains(typeof(IContainer<IItem?>)));
        Assert.AreEqual(typeof(IReadOnlyItemContainer), typeof(IEquipmentContainer).GetProperty("Items")!.PropertyType);
        Assert.IsNotNull(typeof(IEquipmentContainer).GetProperty("Item", [typeof(EquipmentSlot)]));
        Assert.IsNull(typeof(IEquipmentContainer).GetProperty("Item", [typeof(int)]));
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

    private static ItemContainerMutationBoundary Boundary(IItemTransactional participant) =>
        ((IItemTransactionSource)participant).Boundaries.Single();
}
