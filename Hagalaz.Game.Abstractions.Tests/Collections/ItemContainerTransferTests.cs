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
    public void TryTransferTo_InterfaceTypedContainersMoveExactQuantityAndPublishAfterCommit()
    {
        var sourceUpdates = 0;
        var destinationUpdates = 0;
        var source = new ItemContainer(StorageType.Normal, 2, _ => sourceUpdates++);
        var destination = new ItemContainer(StorageType.Normal, 2, _ => destinationUpdates++);
        var item = new TestItem(10, 7);
        Assert.IsTrue(source.Add(item));

        using var transaction = ItemContainerTransaction.Begin(source, destination);
        Assert.IsTrue(source.TryTransferTo(destination, item, 7, 0));
        transaction.Commit();

        Assert.AreEqual(0, source.TakenSlots);
        Assert.AreSame(item, destination[0]);
        Assert.AreEqual(7, destination[0]!.Count);
        Assert.AreEqual(2, sourceUpdates);
        Assert.AreEqual(1, destinationUpdates);
    }

    [TestMethod]
    public void TryTransferTo_InterfaceTypedContainersLeaveBothStoragesUnchangedOnFailure()
    {
        var sourceUpdates = 0;
        var destinationUpdates = 0;
        var source = new ItemContainer(StorageType.Normal, 2, _ => sourceUpdates++);
        var destination = new ItemContainer(StorageType.Normal, 1, _ => destinationUpdates++);
        var item = new TestItem(10, 2);
        var blockingItem = new TestItem(11, 1);
        Assert.IsTrue(source.Add(item));
        Assert.IsTrue(destination.Add(blockingItem));

        using var transaction = ItemContainerTransaction.Begin(source, destination);
        Assert.IsFalse(source.TryTransferTo(destination, item, 2, 0));

        Assert.AreSame(item, source[0]);
        Assert.AreEqual(2, source[0]!.Count);
        Assert.AreSame(blockingItem, destination[0]);
        Assert.AreEqual(1, sourceUpdates);
        Assert.AreEqual(1, destinationUpdates);
    }

    [TestMethod]
    public void TryTransferTo_ResolvesBoundaryFromContainerDecorator()
    {
        var source = new ItemContainer(StorageType.Normal, 2);
        var destination = new ItemContainer(StorageType.Normal, 2);
        var decoratedDestination = new ItemContainerDecorator(destination);
        var item = new TestItem(9, 2);
        Assert.IsTrue(source.Add(item));

        using var transaction = ItemContainerTransaction.Begin(source, decoratedDestination);
        Assert.IsTrue(source.TryTransferTo(decoratedDestination, item, 2, 0));
        transaction.Commit();

        Assert.IsNull(source[0]);
        Assert.AreSame(item, destination[0]);
    }

    [TestMethod]
    public void OrdinaryMutations_ParticipateAutomaticallyAndRollbackOrPublishOnCommit()
    {
        var publications = 0;
        var container = new ItemContainer(StorageType.Normal, 3, _ => publications++);
        var first = new TestItem(14, 1);
        var second = new TestItem(15, 1);

        using (ItemContainerTransaction.Begin(container))
        {
            Assert.IsTrue(container.Add(first));
            Assert.IsTrue(container.AddRange([second]));
            Assert.AreSame(first, container[0]);
            Assert.AreEqual(0, publications);
        }
        Assert.AreEqual(0, container.TakenSlots);
        Assert.AreEqual(0, publications);

        using (var transaction = ItemContainerTransaction.Begin(container))
        {
            Assert.IsTrue(container.AddRange([first, second]));
            container.Sort();
            transaction.Commit();
            transaction.Dispose();
        }
        Assert.AreEqual(2, container.TakenSlots);
        Assert.AreEqual(1, publications);

        using (var transaction = ItemContainerTransaction.Begin(container))
        {
            Assert.IsTrue(container.TryRemoveExact(first));
            container.Clear(true);
            transaction.Commit();
        }
        Assert.AreEqual(0, container.TakenSlots);
        Assert.AreEqual(2, publications);
    }

    [TestMethod]
    public void OrdinaryMutation_OnUnenlistedContainerRemainsStandaloneDuringRollback()
    {
        var firstPublications = 0;
        var secondPublications = 0;
        var first = new ItemContainer(StorageType.Normal, 2, _ => firstPublications++);
        var second = new ItemContainer(StorageType.Normal, 2, _ => secondPublications++);
        var firstItem = new TestItem(16, 1);
        var secondItem = new TestItem(17, 1);
        using (ItemContainerTransaction.Begin(first))
        {
            Assert.IsTrue(first.Add(firstItem));
            Assert.IsTrue(second.Add(secondItem));
            Assert.AreEqual(0, firstPublications);
            Assert.AreEqual(1, secondPublications);
        }

        Assert.IsNull(first[0]);
        Assert.AreEqual(0, firstPublications);
        Assert.AreSame(secondItem, second[0]);
        Assert.AreEqual(1, secondPublications);
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
        using (ItemContainerTransaction.Begin(source, destination))
        {
            Assert.IsTrue(source.TryTransferTo(destination, item, 3, 0));
            source.Clear(true);
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
            using var transaction = ItemContainerTransaction.Begin(source, destination);
            Assert.IsTrue(source.TryTransferTo(destination, item, 5));
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
        using var transaction = ItemContainerTransaction.Begin(second, first, second);
        Assert.IsTrue(first.Add(new TestItem(23, 1)));
        Assert.IsTrue(second.Add(new TestItem(24, 1)));
        Completion(first).Before(() =>
        {
            AssertScopeBoundAndUnlocked(transaction, first, second);
            order.Add("hook");
        });
        Completion(first).After(() =>
        {
            AssertScopeBoundAndUnlocked(transaction, first, second);
            Assert.AreEqual(1, first.TakenSlots);
            Assert.AreEqual(1, second.TakenSlots);
            order.Add("after1");
        });
        Completion(second).After(() => order.Add("after2"));
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
        var updates = new List<HashSet<int>?>();
        var container = new ItemContainer(StorageType.Normal, 3, slots => updates.Add(slots));
        using (var transaction = ItemContainerTransaction.Begin(container))
        {
            Assert.IsTrue(container.Add(new TestItem(26, 1)));
            Assert.IsTrue(container.Add(new TestItem(27, 1)));
            Assert.AreEqual(0, updates.Count);
            transaction.Commit();
        }
        CollectionAssert.AreEquivalent(new[] { 0, 1 }, updates.Single()!.ToArray());
        updates.Clear();
        using (var transaction = ItemContainerTransaction.Begin(container))
        {
            container.Clear(true);
            Assert.IsTrue(container.Add(new TestItem(28, 1)));
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
        using var transaction = ItemContainerTransaction.Begin(container);
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
        using var transaction = ItemContainerTransaction.Begin(source, destination);
        Assert.IsTrue(source.TryTransferTo(destination, item, 1));
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
        ItemContainerTransaction? committing = null;
        first = new ItemContainer(StorageType.Normal, 2, _ =>
        {
            AssertScopeBoundAndUnlocked(committing!, first!, second!);
            calls.Add("first");
            throw original;
        });
        second = new ItemContainer(StorageType.Normal, 2, _ => calls.Add("second"));
        using var transaction = ItemContainerTransaction.Begin(first, second);
        committing = transaction;
        Assert.IsTrue(first.Add(new TestItem(33, 1)));
        Assert.IsTrue(second.Add(new TestItem(34, 1)));
        Completion(first).After(() => calls.Add("after"));
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
        using var transaction = ItemContainerTransaction.Begin(first, second, third);
        Assert.IsTrue(first.Add(new TestItem(35, 1)));
        Assert.IsTrue(second.Add(new TestItem(36, 1)));
        Assert.IsTrue(third.Add(new TestItem(37, 1)));
        Completion(first).After(() => calls.Add("after1"));
        Completion(second).After(() => calls.Add("after2"));
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
        using var transaction = ItemContainerTransaction.Begin(container);
        Assert.IsTrue(container.Add(new TestItem(37, 1)));
        Completion(container).After(() => calls.Add("after1"));
        Completion(container).After(() => { AssertScopeBoundAndUnlocked(transaction, container); calls.Add("after2"); throw original; });
        Completion(container).After(() => calls.Add("after3"));
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
        using var transaction = ItemContainerTransaction.Begin(container);
        Assert.IsTrue(container.Add(new TestItem(38, 1)));
        Completion(container).Before(() => { AssertScopeBoundAndUnlocked(transaction, container); calls.Add("hook"); throw original; });
        Completion(container).After(() => calls.Add("after"));
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
        using var transaction = ItemContainerTransaction.Begin(container);
        Assert.IsTrue(container.Add(new TestItem(39, 1)));
        Completion(container).Before(() => throw hookFailure);
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
        var storage = Boundary(container).Storage;
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
        Assert.AreSame(original, Assert.ThrowsExactly<InvalidOperationException>(() => ItemContainerTransaction.Begin(first, second)));
        failingItem.OnCountRead = null;
        AssertUnboundAndUnlocked(first, second);
        Assert.AreSame(firstItem, first[0]);
        Assert.AreSame(failingItem, second[0]);
        Assert.AreEqual(3, firstItem.Count);
        Assert.AreEqual(4, failingItem.Count);
        Assert.AreEqual(0, calls);
        using var fresh = ItemContainerTransaction.Begin(first, second);
    }

    [TestMethod]
    public void Begin_LockOrderIsDeterministicAndIndependentFromPublicationOrder()
    {
        var first = new ItemContainer(StorageType.Normal, 1);
        var second = new ItemContainer(StorageType.Normal, 1);
        using var transaction = ItemContainerTransaction.Begin(second, first);
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
        using var transaction = ItemContainerTransaction.Begin(first, composite, composite, second);
        Assert.AreEqual(1, reads);
        item.OnCountRead = null;
        first.Clear(true);
        Assert.IsTrue(second.Add(item));
        transaction.Commit();
        CollectionAssert.AreEqual(new[] { "first", "second" }, calls);
        AssertUnboundAndUnlocked(first, second);
    }

    [TestMethod]
    public void Begin_DifferentBoundariesForSameStorageRejectBeforeLocking()
    {
        var container = new ItemContainer(StorageType.Normal, 1);
        var storage = Boundary(container).Storage;
        var first = new CompositeParticipant([Boundary(container)]);
        var secondBoundary = new ItemContainerMutationBoundary(storage, _ => Assert.Fail("Rejected participants must not publish."));
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
    public void Mutation_AuthorizationIsValidatedAfterLockAcquisitionWhenTransactionBinds()
    {
        var ownerThreadId = Environment.CurrentManagedThreadId;
        using var publicationEntered = new ManualResetEventSlim();
        using var finishPublication = new ManualResetEventSlim();
        var publicationCount = 0;
        var container = new ItemContainer(StorageType.Normal, 2, _ =>
        {
            Interlocked.Increment(ref publicationCount);
            if (Environment.CurrentManagedThreadId == ownerThreadId)
            {
                publicationEntered.Set();
                Assert.IsTrue(finishPublication.Wait(TimeSpan.FromSeconds(5)));
            }
        });
        var storage = Boundary(container).Storage;
        var workerItem = new TestItem(601, 1);
        Exception? workerFailure = null;
        var worker = new Thread(() =>
        {
            try { container.Add(workerItem); }
            catch (Exception exception) { workerFailure = exception; }
        }) { IsBackground = true };

        Monitor.Enter(storage.MutationLock);
        ItemContainerTransaction? transaction = null;
        try
        {
            worker.Start();
            Assert.IsTrue(SpinWait.SpinUntil(
                () => (worker.ThreadState & ThreadState.WaitSleepJoin) != 0,
                TimeSpan.FromSeconds(5)), "The worker must be blocked on MutationLock before Begin binds it.");
            transaction = ItemContainerTransaction.Begin(container);
        }
        finally
        {
            Monitor.Exit(storage.MutationLock);
        }

        var releaser = new Thread(() =>
        {
            if (publicationEntered.Wait(TimeSpan.FromSeconds(5)))
            {
                worker.Join(TimeSpan.FromSeconds(5));
            }
            finishPublication.Set();
        }) { IsBackground = true };
        releaser.Start();

        Assert.IsTrue(container.Add(new TestItem(600, 1)));
        transaction!.Commit();

        Assert.IsTrue(worker.Join(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(releaser.Join(TimeSpan.FromSeconds(5)));
        Assert.IsInstanceOfType<InvalidOperationException>(workerFailure);
        Assert.AreEqual(0, container.GetCountById(workerItem.Id));
        Assert.AreEqual(1, publicationCount);
        AssertUnboundAndUnlocked(container);
    }

    [TestMethod]
    public void StandaloneMutation_RemainsStandaloneWhenTransactionBindsBeforePublication()
    {
        var publications = 0;
        using var publicationStarted = new ManualResetEventSlim();
        using var allowPublication = new ManualResetEventSlim();
        var container = new ItemContainer(StorageType.Normal, 2, _ =>
        {
            Interlocked.Increment(ref publications);
            publicationStarted.Set();
            Assert.IsTrue(allowPublication.Wait(TimeSpan.FromSeconds(5)));
        });
        var boundary = Boundary(container);
        var item = new TestItem(602, 1);
        Exception? mutationFailure = null;
        var mutationThread = new Thread(() =>
        {
            try { Assert.IsTrue(container.Add(item)); }
            catch (Exception exception) { mutationFailure = exception; }
        }) { IsBackground = true };
        mutationThread.Start();
        Assert.IsTrue(publicationStarted.Wait(TimeSpan.FromSeconds(5)));

        using var transactionBound = new ManualResetEventSlim();
        using var allowRollback = new ManualResetEventSlim();
        Exception? transactionFailure = null;
        var transactionThread = new Thread(() =>
        {
            try
            {
                using var transaction = ItemContainerTransaction.Begin(container);
                transactionBound.Set();
                Assert.IsTrue(allowRollback.Wait(TimeSpan.FromSeconds(5)));
            }
            catch (Exception exception) { transactionFailure = exception; }
        }) { IsBackground = true };
        transactionThread.Start();

        Assert.IsTrue(transactionBound.Wait(TimeSpan.FromSeconds(5)));
        Assert.IsNotNull(boundary.Storage.Transaction);
        allowRollback.Set();

        Assert.IsTrue(transactionThread.Join(TimeSpan.FromSeconds(5)));
        allowPublication.Set();
        Assert.IsTrue(mutationThread.Join(TimeSpan.FromSeconds(5)));
        if (mutationFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(mutationFailure).Throw();
        if (transactionFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(transactionFailure).Throw();
        Assert.AreSame(item, container[0]);
        Assert.AreEqual(1, publications);
        AssertUnboundAndUnlocked(container);
    }

    [TestMethod]
    public void ContainerMutation_InsideTransactionDefersPublicationUntilCommit()
    {
        var publications = 0;
        var container = new ItemContainer(StorageType.Normal, 2, _ => publications++);
        using var transaction = ItemContainerTransaction.Begin(container);

        Assert.IsTrue(container.Add(new TestItem(604, 1)));

        Assert.AreEqual(0, publications);
        transaction.Commit();
        Assert.AreEqual(1, publications);
    }

    [TestMethod]
    public void ContainerMutation_RejectsCommittedTransactionDuringPublication()
    {
        var publications = 0;
        ItemContainer? container = null;
        container = new ItemContainer(StorageType.Normal, 2, _ =>
        {
            publications++;
            Assert.ThrowsExactly<InvalidOperationException>(() => container!.Add(new TestItem(605, 1)));
        });
        using var transaction = ItemContainerTransaction.Begin(container!);

        Assert.IsTrue(container!.Add(new TestItem(605, 1)));
        transaction.Commit();

        Assert.AreEqual(1, publications);
    }

    [TestMethod]
    public void Commit_InterruptedDuringScopeTeardownRetriesLockAndReleasesBinding()
    {
        using var publicationStarted = new ManualResetEventSlim();
        using var finishPublication = new ManualResetEventSlim();
        using var publicationReturned = new ManualResetEventSlim();
        using var teardownLockHeld = new ManualResetEventSlim();
        using var releaseTeardownLock = new ManualResetEventSlim();
        var publicationCount = 0;
        var container = new ItemContainer(StorageType.Normal, 1, _ =>
        {
            Interlocked.Increment(ref publicationCount);
            publicationStarted.Set();
            try { Assert.IsTrue(finishPublication.Wait(TimeSpan.FromSeconds(5))); }
            finally { publicationReturned.Set(); }
        });
        var storage = Boundary(container).Storage;
        var item = new TestItem(603, 1);
        Exception? commitFailure = null;
        var commitThread = new Thread(() =>
        {
            try
            {
                using var transaction = ItemContainerTransaction.Begin(container);
                container.Add(item);
                transaction.Commit();
            }
            catch (Exception exception) { commitFailure = exception; }
        }) { IsBackground = true };
        commitThread.Start();
        Assert.IsTrue(publicationStarted.Wait(TimeSpan.FromSeconds(5)));

        var lockHolder = new Thread(() =>
        {
            Monitor.Enter(storage.MutationLock);
            try
            {
                teardownLockHeld.Set();
                Assert.IsTrue(releaseTeardownLock.Wait(TimeSpan.FromSeconds(5)));
            }
            finally { Monitor.Exit(storage.MutationLock); }
        }) { IsBackground = true };
        lockHolder.Start();
        Assert.IsTrue(teardownLockHeld.Wait(TimeSpan.FromSeconds(5)));
        finishPublication.Set();
        Assert.IsTrue(publicationReturned.Wait(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(SpinWait.SpinUntil(
            () => (commitThread.ThreadState & ThreadState.WaitSleepJoin) != 0,
            TimeSpan.FromSeconds(5)), "Commit must be waiting to reacquire MutationLock for teardown.");
        commitThread.Interrupt();
        releaseTeardownLock.Set();

        Assert.IsTrue(commitThread.Join(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(lockHolder.Join(TimeSpan.FromSeconds(5)));
        Assert.IsInstanceOfType<ThreadInterruptedException>(commitFailure);
        Assert.AreSame(item, container[0]);
        Assert.AreEqual(1, publicationCount);
        AssertUnboundAndUnlocked(container);
        using var fresh = ItemContainerTransaction.Begin(container);
        fresh.Commit();
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
        Assert.AreSame(transaction, Boundary(first).Storage.Transaction);
        Assert.AreSame(item, first[0]);
        Assert.IsNull(second[0]);
        AssertUnboundAndUnlocked(second);
        Assert.ThrowsExactly<InvalidOperationException>(() => ItemContainerTransaction.Begin(first, second));
        AssertUnboundAndUnlocked(second);
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
        Assert.AreSame(firstScope, Boundary(first).Storage.Transaction);
        Assert.AreSame(secondScope, Boundary(second).Storage.Transaction);
        Assert.AreSame(item, first[0]);
        Assert.IsNull(second[0]);
    }

    [TestMethod]
    public void Transfer_WithoutTransactionRejectsBeforeMutation()
    {
        var source = new ItemContainer(StorageType.Normal, 1);
        var destination = new ItemContainer(StorageType.Normal, 1);
        var item = new TestItem(42, 1);
        Assert.IsTrue(source.Add(item));

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

        Assert.ThrowsExactly<InvalidOperationException>(() => sourceStorage.TryTransferTo(
            destinationStorage, item, 1, 0, -1, null, out _, out _));

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

        Assert.ThrowsExactly<InvalidOperationException>(() => sourceStorage.TryTransferTo(
            destinationStorage, item, 1, 0, -1, null, out _, out _));

        Assert.AreSame(sourceTransaction, sourceStorage.Transaction);
        Assert.AreSame(destinationTransaction, destinationStorage.Transaction);
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

        OnOtherThread(() => Assert.ThrowsExactly<InvalidOperationException>(() => sourceStorage.TryTransferTo(
            destinationStorage, item, 1, 0, -1, null, out _, out _)));

        Assert.AreSame(transaction, sourceStorage.Transaction);
        Assert.AreSame(transaction, destinationStorage.Transaction);
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
        Assert.AreSame(transaction, Boundary(first).Storage.Transaction);
        Assert.AreSame(item, first[0]);
        Assert.AreEqual(1, first.TakenSlots);
        Assert.AreEqual(0, second.TakenSlots);
        transaction.Commit();
        OnOtherThread(() => Assert.ThrowsExactly<InvalidOperationException>(() => transaction.Dispose()));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Commit_ForeignBeginWaitsUntilPublicationFinishesAndCannotSeeLaterMutation(bool publicationFails)
    {
        using var publicationStarted = new ManualResetEventSlim();
        using var finishPublication = new ManualResetEventSlim();
        using var contenderAttempted = new ManualResetEventSlim();
        using var contenderEntered = new ManualResetEventSlim();
        var itemA = new TestItem(140, 1);
        var itemB = new TestItem(141, 1);
        var original = new InvalidOperationException("Publisher failure.");
        ItemContainer? container = null;
        ItemContainerTransaction? committing = null;
        var publications = 0;
        container = new ItemContainer(StorageType.Normal, 2, _ =>
        {
            AssertScopeBoundAndUnlocked(committing!, container!);
            if (Interlocked.Increment(ref publications) != 1) return;
            Assert.AreSame(itemA, container![0]);
            Assert.IsNull(container[1]);
            publicationStarted.Set();
            Assert.IsTrue(finishPublication.Wait(TimeSpan.FromSeconds(5)), "Publication must be released by the test.");
            Assert.IsFalse(contenderEntered.IsSet, "The later transaction must not enter before publication returns.");
            Assert.AreSame(itemA, container![0]);
            Assert.IsNull(container[1]);
            if (publicationFails) throw original;
        });

        Exception? firstFailure = null;
        var first = new Thread(() =>
        {
            try
            {
                using var transaction = ItemContainerTransaction.Begin(container);
                committing = transaction;
                Assert.IsTrue(container.Add(itemA));
                transaction.Commit();
            }
            catch (Exception exception) { firstFailure = exception; }
        }) { IsBackground = true };
        first.Start();
        Assert.IsTrue(publicationStarted.Wait(TimeSpan.FromSeconds(5)), "The first scope must reach publication.");

        Exception? secondFailure = null;
        var second = new Thread(() =>
        {
            try
            {
                contenderAttempted.Set();
                using var transaction = ItemContainerTransaction.Begin(container);
                committing = transaction;
                contenderEntered.Set();
                Assert.IsTrue(container.Add(itemB));
                transaction.Commit();
            }
            catch (Exception exception) { secondFailure = exception; }
        }) { IsBackground = true };
        second.Start();
        try
        {
            Assert.IsTrue(contenderAttempted.Wait(TimeSpan.FromSeconds(5)));
            Assert.IsFalse(contenderEntered.Wait(TimeSpan.FromMilliseconds(100)), "The foreign scope must wait through publication.");
            Assert.AreSame(itemA, container[0]);
            Assert.IsNull(container[1]);
        }
        finally { finishPublication.Set(); }

        Assert.IsTrue(first.Join(TimeSpan.FromSeconds(5)), "The committing scope must finish publication.");
        Assert.IsTrue(second.Join(TimeSpan.FromSeconds(5)), "The waiting scope must proceed after release.");
        if (publicationFails) Assert.AreSame(original, firstFailure);
        else if (firstFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(firstFailure).Throw();
        if (secondFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(secondFailure).Throw();
        Assert.IsTrue(contenderEntered.IsSet);
        Assert.AreSame(itemA, container[0]);
        Assert.AreSame(itemB, container[1]);
        Assert.AreEqual(2, publications, "The first publication must run once, even when it throws.");
        AssertUnboundAndUnlocked(container);
    }

    [TestMethod]
    public void Commit_PublicationRejectsSameThreadReentrancyButAllowsDisjointScope()
    {
        ItemContainer? container = null;
        ItemContainerTransaction? committing = null;
        var publicationCount = 0;
        container = new ItemContainer(StorageType.Normal, 2, _ =>
        {
            AssertScopeBoundAndUnlocked(committing!, container!);
            if (Interlocked.Increment(ref publicationCount) != 1) return;
            Assert.ThrowsExactly<InvalidOperationException>(() => ItemContainerTransaction.Begin(container!));
            Assert.ThrowsExactly<InvalidOperationException>(() => container!.Add(new TestItem(145, 1)));
            var unrelated = new ItemContainer(StorageType.Normal, 1);
            using var disjoint = ItemContainerTransaction.Begin(unrelated);
            Assert.IsTrue(unrelated.Add(new TestItem(146, 1)));
            disjoint.Commit();
            Assert.AreEqual(1, container!.TakenSlots);
        });
        using (var transaction = ItemContainerTransaction.Begin(container!))
        {
            committing = transaction;
            Assert.IsTrue(container!.Add(new TestItem(144, 1)));
            transaction.Commit();
        }
        Assert.AreEqual(1, container!.TakenSlots);
        using var later = ItemContainerTransaction.Begin(container!);
        committing = later;
        Assert.IsTrue(container!.Add(new TestItem(147, 1)));
        later.Commit();
        Assert.AreEqual(2, container!.TakenSlots);
        AssertUnboundAndUnlocked(container);
    }

    [TestMethod]
    public void Commit_MultiStorageForeignScopeWaitsAndUsesDeterministicLockOrder()
    {
        using var publicationStarted = new ManualResetEventSlim();
        using var finishPublication = new ManualResetEventSlim();
        using var contenderAttempted = new ManualResetEventSlim();
        using var contenderEntered = new ManualResetEventSlim();
        var prefix = new ItemContainer(StorageType.Normal, 1);
        var ownerOnly = new ItemContainer(StorageType.Normal, 1);
        ItemContainerTransaction? committing = null;
        ItemContainer? shared = null;
        shared = new ItemContainer(StorageType.Normal, 1, _ =>
        {
            AssertScopeBoundAndUnlocked(committing!, shared!, ownerOnly);
            publicationStarted.Set();
            Assert.IsTrue(finishPublication.Wait(TimeSpan.FromSeconds(5)));
            Assert.IsFalse(contenderEntered.IsSet);
        });
        var owner = new CompositeParticipant([Boundary(shared), Boundary(ownerOnly)]);
        var contender = new CompositeParticipant([Boundary(prefix), Boundary(shared)]);
        Exception? ownerFailure = null;
        var first = new Thread(() =>
        {
            try
            {
                using var transaction = ItemContainerTransaction.Begin(owner);
                committing = transaction;
                Assert.IsTrue(shared.Add(new TestItem(148, 1)));
                Assert.IsTrue(ownerOnly.Add(new TestItem(149, 1)));
                transaction.Commit();
            }
            catch (Exception exception) { ownerFailure = exception; }
        }) { IsBackground = true };
        first.Start();
        Assert.IsTrue(publicationStarted.Wait(TimeSpan.FromSeconds(5)));

        Exception? contenderFailure = null;
        var second = new Thread(() =>
        {
            try
            {
                contenderAttempted.Set();
                using var transaction = ItemContainerTransaction.Begin(contender);
                var order = (ItemContainerStorage[])typeof(ItemContainerTransaction)
                    .GetField("_lockOrder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(transaction)!;
                CollectionAssert.AreEqual(new[] { Boundary(prefix).Storage, Boundary(shared).Storage }, order);
                foreach (var storage in order) Assert.IsTrue(Monitor.IsEntered(storage.MutationLock));
                contenderEntered.Set();
                transaction.Commit();
            }
            catch (Exception exception) { contenderFailure = exception; }
        }) { IsBackground = true };
        second.Start();
        try
        {
            Assert.IsTrue(contenderAttempted.Wait(TimeSpan.FromSeconds(5)));
            Assert.IsFalse(contenderEntered.Wait(TimeSpan.FromMilliseconds(100)));
        }
        finally { finishPublication.Set(); }
        Assert.IsTrue(first.Join(TimeSpan.FromSeconds(5)), "The first scope must release the complete set.");
        Assert.IsTrue(second.Join(TimeSpan.FromSeconds(5)), "The overlapping scope must not deadlock during teardown.");
        if (ownerFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ownerFailure).Throw();
        if (contenderFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(contenderFailure).Throw();
        Assert.IsTrue(contenderEntered.IsSet);
        AssertUnboundAndUnlocked(prefix, shared, ownerOnly);
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
        using var transaction = ItemContainerTransaction.Begin(container);
        Assert.IsTrue(container.Add(new TestItem(47, 1)));
        foreach (var failure in hookFailures)
            Completion(container).Before(() => { AssertScopeBoundAndUnlocked(transaction, container); throw failure; });
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
        Assert.AreEqual(0, typeof(IItemTransactional).GetMembers().Length);
        Assert.IsNull(typeof(IItemContainer).Assembly.GetType("Hagalaz.Game.Abstractions.Collections.IItemContainerMutationBoundary"));
    }

    [TestMethod]
    public void ItemContainer_KeepsRawStorageAndStateHelpersInternal()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Assert.IsNull(typeof(ItemContainer).GetProperty("Storage", flags));
        foreach (var name in new[] { "RestoreItems", "SnapshotItems", "ReplaceState", "BeginMutation" })
        {
            var method = typeof(ItemContainer).GetMethod(name, flags);
            Assert.IsNotNull(method, name);
            Assert.IsTrue(method.IsAssembly, name);
        }
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ItemContainer, TestCompletionOwner> CompletionOwners = new();
    private static TestCompletionOwner Completion(ItemContainer container) => CompletionOwners.GetValue(container, key =>
    {
        var owner = new TestCompletionOwner(Boundary(key));
        typeof(ItemContainerMutationBoundary).GetField("_completion", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(Boundary(key), owner);
        return owner;
    });

    private sealed class TestCompletionOwner(ItemContainerMutationBoundary boundary) : IItemContainerCompletionOwner
    {
        private readonly Dictionary<ItemContainerTransaction, Dictionary<int, Action>> _before = [];
        private readonly Dictionary<ItemContainerTransaction, Dictionary<int, Action>> _after = [];
        internal void Before(Action effect) => Add(_before, effect);
        internal void After(Action effect) => Add(_after, effect);
        private void Add(Dictionary<ItemContainerTransaction, Dictionary<int, Action>> pending, Action effect)
        {
            var transaction = boundary.Storage.Transaction!;
            if (!pending.TryGetValue(transaction, out var effects)) pending.Add(transaction, effects = []);
            effects.Add(transaction.NextCompletionOrder(), effect);
        }
        public void DiscardPendingCompletion(ItemContainerTransaction transaction) { _before.Remove(transaction); _after.Remove(transaction); }
        public void CompleteBeforePublication(ItemContainerTransaction transaction, int order)
        {
            if (_before.TryGetValue(transaction, out var effects) && effects.Remove(order, out var effect)) effect();
        }
        public void CompleteAfterPublication(ItemContainerTransaction transaction, int order)
        {
            if (_after.TryGetValue(transaction, out var effects) && effects.Remove(order, out var effect)) effect();
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

    private static void AssertScopeBoundAndUnlocked(ItemContainerTransaction transaction, params ItemContainer[] containers)
    {
        foreach (var container in containers)
        {
            var storage = Boundary(container).Storage;
            Assert.AreSame(transaction, storage.Transaction);
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

    private sealed class ExternalParticipant : IItemTransactional { }
    private sealed class CompositeParticipant(IReadOnlyList<ItemContainerMutationBoundary> boundaries) : IItemTransactionSource
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

        using var transaction = ItemContainerTransaction.Begin(inventory.Items, bank.Items);
        Assert.IsTrue(inventory.Items.TryTransferTo(bank.Items, item, 4, 0));
        transaction.Commit();

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
        Assert.IsNull(typeof(IItemContainer).GetProperty("Mutations"));
        Assert.IsFalse(typeof(IItemContainer).GetProperties().Any(property => property.Name == "Mutations"));
        Assert.IsNull(typeof(IItemContainer).Assembly.GetType("Hagalaz.Game.Abstractions.Collections.IItemContainerTransactionParticipant"));
        Assert.IsNull(typeof(IItemContainer).Assembly.GetType("Hagalaz.Game.Abstractions.Collections.IItemContainerTransactionParticipantInternal"));
        Assert.IsNotNull(typeof(IItemContainer).Assembly.GetType("Hagalaz.Game.Abstractions.Collections.IItemTransactional"));
        Assert.AreEqual(typeof(IItemTransactional[]), typeof(ItemContainerTransaction)
            .GetMethod(nameof(ItemContainerTransaction.Begin))!.GetParameters()[0].ParameterType);
        Assert.IsNotNull(typeof(IItemContainer).GetMethod(nameof(IItemContainer.TryTransferTo)));
        Assert.IsNull(typeof(IItemContainer).Assembly.GetType("Hagalaz.Game.Abstractions.Collections.IItemContainerMutationBoundary"));
        foreach (var method in new[] { "TryAdd", "TryAddRange", "TryRemoveExact", "Sort", "Clear", "EnsureOutsideTransaction" })
        {
            Assert.IsNull(typeof(IItemTransactional).GetMethod(method), method);
        }
        Assert.IsNull(typeof(IItemTransactional).GetMethod("EnsureUnbound"));
        Assert.IsFalse(typeof(IEquipmentContainer).GetInterfaces().Contains(typeof(IContainer<IItem?>)));
        Assert.AreEqual(typeof(IReadOnlyItemContainer), typeof(IEquipmentContainer).GetProperty("Items")!.PropertyType);
        Assert.IsNotNull(typeof(IEquipmentContainer).GetProperty("Item", [typeof(EquipmentSlot)]));
        Assert.IsNull(typeof(IEquipmentContainer).GetProperty("Item", [typeof(int)]));
        Assert.IsNull(typeof(IEquipmentContainer).GetProperty("Mutations"));
        Assert.IsFalse(typeof(IEquipmentContainer).GetProperties().Any(property => property.PropertyType == typeof(IItemContainer)));
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

    private sealed class ItemContainerDecorator : IItemContainer, IItemTransactionSource
    {
        private readonly IItemContainer _inner;

        public ItemContainerDecorator(IItemContainer inner) => _inner = inner;

        IReadOnlyList<ItemContainerMutationBoundary> IItemTransactionSource.Boundaries =>
            ((IItemTransactionSource)_inner).Boundaries;

        public StorageType Type => _inner.Type;
        public int Capacity => _inner.Capacity;
        public int FreeSlots => _inner.FreeSlots;
        public int TakenSlots => _inner.TakenSlots;
        public IItem? this[int index] => _inner[index];
        public IEnumerator<IItem?> GetEnumerator() => _inner.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Add(IItem item) => _inner.Add(item);
        public bool Add(int slot, IItem item) => _inner.Add(slot, item);
        public int Remove(IItem item, int preferredSlot = -1, bool update = true) => _inner.Remove(item, preferredSlot, update);
        public bool TryRemoveExact(IItem item, int preferredSlot = -1) => _inner.TryRemoveExact(item, preferredSlot);
        public bool TryTransferTo(IItemContainer destination, IItem item, int count, int preferredSourceSlot = -1,
            int destinationSlot = -1, IItem? destinationItem = null) =>
            _inner.TryTransferTo(destination, item, count, preferredSourceSlot, destinationSlot, destinationItem);
        public void Replace(int slot, IItem item) => _inner.Replace(slot, item);
        public void Swap(int fromSlot, int toSlot) => _inner.Swap(fromSlot, toSlot);
        public void Move(int fromSlot, int toSlot) => _inner.Move(fromSlot, toSlot);
        public bool AddRange(IEnumerable<IItem?> items) => _inner.AddRange(items);
        public void Sort() => _inner.Sort();
        public void Clear(bool update) => _inner.Clear(update);
        public IItem? GetById(int id) => _inner.GetById(id);
        public int GetCount(IItem item) => _inner.GetCount(item);
        public int GetCountById(int id) => _inner.GetCountById(id);
        public int GetInstanceSlot(IItem instance) => _inner.GetInstanceSlot(instance);
        public int GetSlotByItem(IItem item, bool ignoreCount = true) => _inner.GetSlotByItem(item, ignoreCount);
        public bool Contains(int id) => _inner.Contains(id);
        public bool Contains(int id, int count) => _inner.Contains(id, count);
        public bool HasSpaceFor(IItem item) => _inner.HasSpaceFor(item);
        public bool HasSpaceForRange(IEnumerable<IItem?> items) => _inner.HasSpaceForRange(items);
    }

    private static ItemContainerMutationBoundary Boundary(IItemTransactional participant) =>
        ((IItemTransactionSource)participant).Boundaries.Single();
}
