using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Characters;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Characters;

[TestClass]
public sealed class TradeExchangeTests
{
    [TestMethod]
    public void TryExchange_TransfersStackableUnstackableAndMoneyOffers()
    {
        var firstInventory = new ComposedTestContainer(14);
        var secondInventory = new ComposedTestContainer(14);
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var firstOffer = new ComposedTestContainer(StorageType.Normal, 14);
        var secondOffer = new ComposedTestContainer(StorageType.Normal, 14);
        firstOffer.Items.Add(new TestItem(100, 20, stackable: true)).Should().BeTrue();
        firstOffer.Items.Add(new TestItem(101, 1)).Should().BeTrue();
        secondOffer.Items.Add(new TestItem(995, 250, stackable: true)).Should().BeTrue();

        var result = CreateTradeExchange().TryCompleteTrade(first, firstOffer.Items, second, secondOffer.Items);

        result.Should().BeTrue();
        first.MoneyPouch.Count.Should().Be(250);
        second.Inventory.Items.GetCountById(100).Should().Be(20);
        second.Inventory.Items.GetCountById(101).Should().Be(1);
        firstOffer.Items.GetCountById(100).Should().Be(0);
        secondOffer.Items.GetCountById(995).Should().Be(0);
    }

    [TestMethod]
    public void TryExchange_WhenDestinationCapacityChanges_FailsWithoutMutation()
    {
        var firstInventory = new ComposedTestContainer(0);
        var secondInventory = new ComposedTestContainer(0);
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var firstOffer = new ComposedTestContainer(StorageType.Normal, 14);
        var secondOffer = new ComposedTestContainer(StorageType.Normal, 14);
        firstOffer.Items.Add(new TestItem(100, 1)).Should().BeTrue();

        var result = CreateTradeExchange().TryCompleteTrade(first, firstOffer.Items, second, secondOffer.Items);

        result.Should().BeFalse();
        firstInventory.Items.TakenSlots.Should().Be(0);
        secondInventory.Items.TakenSlots.Should().Be(0);
        firstOffer.Items.GetCountById(100).Should().Be(1);
    }

    [TestMethod]
    public void TryCompleteTrade_PublishesAfterBothRecipientsAndEscrowReachFinalState()
    {
        var firstInventory = new ComposedTestContainer(4);
        var secondInventory = new ComposedTestContainer(4);
        var firstMoneyPouch = new TestMoneyPouch(firstInventory);
        var secondMoneyPouch = new TestMoneyPouch(secondInventory);
        var first = CreateCharacter(firstInventory, firstMoneyPouch);
        var second = CreateCharacter(secondInventory, secondMoneyPouch);
        var firstOffer = new ComposedTestContainer(StorageType.Normal, 4);
        var secondOffer = new ComposedTestContainer(StorageType.Normal, 4);
        firstOffer.Items.Add(new TestItem(100, 1)).Should().BeTrue();
        secondOffer.Items.Add(new TestItem(101, 1)).Should().BeTrue();
        var firstPublicationSawFinalState = false;
        firstInventory.OnUpdateAction = () =>
        {
            Monitor.IsEntered(firstInventory.MutationLock).Should().BeFalse();
            Monitor.IsEntered(firstMoneyPouch.MutationLock).Should().BeFalse();
            Monitor.IsEntered(secondMoneyPouch.MutationLock).Should().BeFalse();
            firstInventory.Items.GetCountById(101).Should().Be(1);
            secondInventory.Items.GetCountById(100).Should().Be(1);
            firstOffer.Items.TakenSlots.Should().Be(0);
            secondOffer.Items.TakenSlots.Should().Be(0);
            firstPublicationSawFinalState = true;
        };

        CreateTradeExchange().TryCompleteTrade(first, firstOffer.Items, second, secondOffer.Items).Should().BeTrue();

        firstPublicationSawFinalState.Should().BeTrue();
    }

    [TestMethod]
    public void TryCompleteTrade_WhenLaterPouchMutationFails_RestoresStateWithoutPublication()
    {
        var firstInventory = new ComposedTestContainer(4);
        var secondInventory = new ComposedTestContainer(4);
        var firstMoneyPouch = new TestMoneyPouch(firstInventory);
        var secondMoneyPouch = new TestMoneyPouch(secondInventory) { FailNextStorageAdd = true };
        var first = CreateCharacter(firstInventory, firstMoneyPouch);
        var second = CreateCharacter(secondInventory, secondMoneyPouch);
        var firstOffer = new ComposedTestContainer(StorageType.Normal, 4);
        var secondOffer = new ComposedTestContainer(StorageType.Normal, 4);
        firstOffer.Items.Add(new TestItem(995, 5, stackable: true)).Should().BeTrue();
        secondOffer.Items.Add(new TestItem(995, 7, stackable: true)).Should().BeTrue();
        var publicationOccurred = false;
        firstInventory.OnUpdateAction = () =>
        {
            publicationOccurred = true;
        };

        CreateTradeExchange().TryCompleteTrade(first, firstOffer.Items, second, secondOffer.Items).Should().BeFalse();

        publicationOccurred.Should().BeFalse();
        secondInventory.Items.GetCountById(995).Should().Be(0);
        firstMoneyPouch.Count.Should().Be(0);
        secondMoneyPouch.Count.Should().Be(0);
        firstOffer.Items.GetCountById(995).Should().Be(5);
        secondOffer.Items.GetCountById(995).Should().Be(7);
    }

    [TestMethod]
    public void TryOfferMoneyFromPouch_PublishesAfterPouchInventoryAndOfferStorageCommit()
    {
        var inventory = new ComposedTestContainer(4);
        var moneyPouch = new TestMoneyPouch(inventory);
        moneyPouch.Add(25).Should().BeTrue();
        var character = CreateCharacter(inventory, moneyPouch);
        var offer = new ComposedTestContainer(StorageType.Normal, 4);
        var publicationSawFinalState = false;
        offer.OnUpdateAction = () =>
        {
            Monitor.IsEntered(offer.MutationLock).Should().BeFalse();
            Monitor.IsEntered(moneyPouch.MutationLock).Should().BeFalse();
            Monitor.IsEntered(inventory.MutationLock).Should().BeFalse();
            offer.Items.GetCountById(995).Should().Be(25);
            moneyPouch.Count.Should().Be(0);
            inventory.Items.GetCountById(995).Should().Be(0);
            publicationSawFinalState = true;
        };

        CreateTradeExchange().TryOfferMoneyFromPouch(character, offer.Items, new TestItem(995, 25, stackable: true)).Should().BeTrue();

        publicationSawFinalState.Should().BeTrue();
    }

    [TestMethod]
    public void TryOfferMoneyFromPouch_WhenCoinsAreSplitBetweenPouchAndInventory_TransfersCompleteAmount()
    {
        var inventory = new ComposedTestContainer(4);
        inventory.Items.Add(new TestItem(995, 75, stackable: true)).Should().BeTrue();
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        var moneyPouch = new MoneyPouchContainer(character, CreateItemBuilder());
        character.MoneyPouch.Returns(moneyPouch);
        moneyPouch.Add(25).Should().BeTrue();
        var offer = new ComposedTestContainer(StorageType.Normal, 4);

        var result = CreateTradeExchange().TryOfferMoneyFromPouch(character, offer.Items,
            new TestItem(995, 100, stackable: true));

        result.Should().BeTrue();
        offer.Items.GetCountById(995).Should().Be(100);
        moneyPouch.Count.Should().Be(0);
        inventory.Items.GetCountById(995).Should().Be(0);
    }

    [TestMethod]
    public void ItemContainer_AddRange_UsesGenericStorageOperation()
    {
        var container = new ItemContainer(StorageType.Normal, 4);

        var added = container.AddRange([new TestItem(995, 10, stackable: true)]);

        added.Should().BeTrue();
        container.GetCountById(995).Should().Be(10);
    }

    [TestMethod]
    public void TryReturnMoneyToPouch_PublishesAfterPouchInventoryAndOfferStorageCommit()
    {
        var inventory = new ComposedTestContainer(4);
        var moneyPouch = new TestMoneyPouch(inventory);
        moneyPouch.Add(int.MaxValue - 5).Should().BeTrue();
        var character = CreateCharacter(inventory, moneyPouch);
        var offer = new ComposedTestContainer(StorageType.Normal, 4);
        offer.Items.Add(new TestItem(995, 10, stackable: true)).Should().BeTrue();
        var publicationSawFinalState = false;
        offer.OnUpdateAction = () =>
        {
            Monitor.IsEntered(offer.MutationLock).Should().BeFalse();
            Monitor.IsEntered(moneyPouch.MutationLock).Should().BeFalse();
            Monitor.IsEntered(inventory.MutationLock).Should().BeFalse();
            offer.Items.GetCountById(995).Should().Be(0);
            moneyPouch.Count.Should().Be(int.MaxValue);
            inventory.Items.GetCountById(995).Should().Be(5);
            publicationSawFinalState = true;
        };

        CreateTradeExchange().TryReturnMoneyToPouch(character, offer.Items, new TestItem(995, 10, stackable: true), -1).Should().BeTrue();

        publicationSawFinalState.Should().BeTrue();
    }

    [TestMethod]
    public void TryExchange_WhenPouchIsFull_UsesInventoryForCoinOverflow()
    {
        var firstInventory = new ComposedTestContainer(1);
        var secondInventory = new ComposedTestContainer(1);
        var firstMoneyPouch = new TestMoneyPouch(firstInventory);
        firstMoneyPouch.Add(int.MaxValue).Should().BeTrue();
        var first = CreateCharacter(firstInventory, firstMoneyPouch);
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var firstOffer = new ComposedTestContainer(StorageType.Normal, 14);
        var secondOffer = new ComposedTestContainer(StorageType.Normal, 14);
        secondOffer.Items.Add(new TestItem(995, 2, stackable: true)).Should().BeTrue();

        var result = CreateTradeExchange().TryCompleteTrade(first, firstOffer.Items, second, secondOffer.Items);

        result.Should().BeTrue();
        firstMoneyPouch.Count.Should().Be(int.MaxValue);
        firstInventory.Items.GetCountById(995).Should().Be(2);
    }

    [TestMethod]
    public void TryExchange_WhenItemAndFullPouchOverflowShareOneSlot_FailsWithoutMutation()
    {
        var firstInventory = new ComposedTestContainer(2);
        firstInventory.Items.Add(new TestItem(200, 1)).Should().BeTrue();
        var firstMoneyPouch = new TestMoneyPouch(firstInventory);
        firstMoneyPouch.Add(int.MaxValue).Should().BeTrue();
        var first = CreateCharacter(firstInventory, firstMoneyPouch);
        var secondInventory = new ComposedTestContainer(1);
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var firstOffer = new ComposedTestContainer(StorageType.Normal, 14);
        var secondOffer = new ComposedTestContainer(StorageType.Normal, 14);
        secondOffer.Items.Add(new TestItem(101, 1)).Should().BeTrue();
        secondOffer.Items.Add(new TestItem(995, 1, stackable: true)).Should().BeTrue();

        var result = CreateTradeExchange().TryCompleteTrade(first, firstOffer.Items, second, secondOffer.Items);

        result.Should().BeFalse();
        firstInventory.Items.GetCountById(101).Should().Be(0);
        firstInventory.Items.GetCountById(995).Should().Be(0);
        firstMoneyPouch.Count.Should().Be(int.MaxValue);
        secondOffer.Items.GetCountById(101).Should().Be(1);
        secondOffer.Items.GetCountById(995).Should().Be(1);
    }

    [TestMethod]
    public void TryExchange_WhenDestinationBatchReturnsFalseAfterEarlierItem_DoesNotLeavePartialCredit()
    {
        var firstInventory = new ComposedTestContainer(2);
        var secondInventory = new ComposedTestContainer(3);
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var firstOffer = new ComposedTestContainer(StorageType.Normal, 14);
        var secondOffer = new ComposedTestContainer(StorageType.Normal, 14);
        firstOffer.Items.Add(new TestItem(200, 1)).Should().BeTrue();
        secondOffer.Items.Add(new TestItem(101, 1)).Should().BeTrue();
        secondOffer.Items.Add(new TestItem(102, 2)).Should().BeTrue();

        CreateTradeExchange().TryCompleteTrade(first, firstOffer.Items, second, secondOffer.Items).Should().BeFalse();
        CreateTradeExchange().TryRefundTrade(first, firstOffer.Items, second, secondOffer.Items).Should().BeTrue();

        firstInventory.Items.GetCountById(101).Should().Be(0);
        secondInventory.Items.GetCountById(101).Should().Be(1);
        secondInventory.Items.GetCountById(102).Should().Be(2);
        firstInventory.Items.GetCountById(200).Should().Be(1);
    }

    [TestMethod]
    public void TryExchange_WhenSecondRecipientPreflightFails_DoesNotMutateFirstRecipientBeforeRefund()
    {
        var firstInventory = new ComposedTestContainer(3);
        var secondInventory = new ComposedTestContainer(2);
        var secondExisting = new TestItem(100, 1, stackable: true);
        secondInventory.Items.Add(secondExisting).Should().BeTrue();
        var capacityItem = new TestItem(300, 1);
        secondInventory.Items.Add(capacityItem).Should().BeTrue();
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var firstOffer = new ComposedTestContainer(StorageType.Normal, 14);
        var secondOffer = new ComposedTestContainer(StorageType.Normal, 14);
        firstOffer.Items.Add(new TestItem(201, 1)).Should().BeTrue();
        firstOffer.Items.Add(new TestItem(100, 1, stackable: true)).Should().BeTrue();
        secondOffer.Items.Add(new TestItem(101, 1)).Should().BeTrue();
        var firstInventoryUpdatesBeforeExchange = firstInventory.UpdateCount;

        CreateTradeExchange().TryCompleteTrade(first, firstOffer.Items, second, secondOffer.Items).Should().BeFalse();
        firstInventory.UpdateCount.Should().Be(firstInventoryUpdatesBeforeExchange);
        firstInventory.Items.TakenSlots.Should().Be(0);
        secondInventory.Items.GetCountById(100).Should().Be(1);
        secondInventory.Items.GetCountById(101).Should().Be(0);
        firstOffer.Items.GetCountById(201).Should().Be(1);
        firstOffer.Items.GetCountById(100).Should().Be(1);
        secondOffer.Items.GetCountById(101).Should().Be(1);

        secondInventory.Items.Remove(capacityItem).Should().Be(1);
        CreateTradeExchange().TryRefundTrade(first, firstOffer.Items, second, secondOffer.Items).Should().BeTrue();

        firstInventory.Items.GetCountById(101).Should().Be(0);
        secondInventory.Items.GetCountById(101).Should().Be(1);
        firstInventory.Items.GetCountById(201).Should().Be(1);
        firstInventory.Items.GetCountById(100).Should().Be(1);
    }

    [TestMethod]
    public void TryRefund_ReturnsEscrowToTheOfferingCharacters()
    {
        var firstInventory = new ComposedTestContainer(1);
        var secondInventory = new ComposedTestContainer(1);
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var firstOffer = new ComposedTestContainer(StorageType.Normal, 14);
        var secondOffer = new ComposedTestContainer(StorageType.Normal, 14);
        firstOffer.Items.Add(new TestItem(100, 1)).Should().BeTrue();
        secondOffer.Items.Add(new TestItem(995, 125, stackable: true)).Should().BeTrue();

        var result = CreateTradeExchange().TryRefundTrade(first, firstOffer.Items, second, secondOffer.Items);

        result.Should().BeTrue();
        firstInventory.Items.GetCountById(100).Should().Be(1);
        second.MoneyPouch.Count.Should().Be(125);
    }

    [TestMethod]
    public void OfferInventoryItem_UsesSharedExactTransferBoundary()
    {
        var firstInventory = new ComposedTestContainer(14);
        var secondInventory = new ComposedTestContainer(14);
        var firstMoneyPouch = new TestMoneyPouch(firstInventory);
        var secondMoneyPouch = new TestMoneyPouch(secondInventory);
        var first = CreateCharacter(firstInventory, firstMoneyPouch);
        var second = CreateCharacter(secondInventory, secondMoneyPouch);
        var item = new TestItem(102, 5, stackable: true);
        firstInventory.Items.Add(item).Should().BeTrue();
        var script = CreatePreparedScript(first, second, firstMoneyPouch, secondMoneyPouch);
        var method = typeof(TradingCharacterScript).GetMethod("TryOfferInventoryItem", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var result = (bool)method.Invoke(script, [true, item, 3, 0])!;

        result.Should().BeTrue();
        firstInventory.Items.GetCountById(102).Should().Be(2);
        script.SelfContainer.Items.GetCountById(102).Should().Be(3);
    }

    [TestMethod]
    public async Task FinishTradeSession_ConcurrentCallsTransferOnlyOnce()
    {
        var firstInventory = new ComposedTestContainer(14);
        var secondInventory = new ComposedTestContainer(14);
        var firstMoneyPouch = new TestMoneyPouch(firstInventory);
        var secondMoneyPouch = new TestMoneyPouch(secondInventory);
        var first = CreateCharacter(firstInventory, firstMoneyPouch);
        var second = CreateCharacter(secondInventory, secondMoneyPouch);

        var script = CreatePreparedScript(first, second, firstMoneyPouch, secondMoneyPouch);
        using var start = new Barrier(3);
        var firstCall = Task.Run(() =>
        {
            start.SignalAndWait();
            script.FinishTradeSession();
        });
        var secondCall = Task.Run(() =>
        {
            start.SignalAndWait();
            script.FinishTradeSession();
        });
        start.SignalAndWait();
        await Task.WhenAll(firstCall, secondCall);
        script.FinishTradeSession();

        firstInventory.Items.GetCountById(101).Should().Be(1);
        secondInventory.Items.GetCountById(100).Should().Be(1);
        script.TradeSession.Should().BeFalse();
    }

    [TestMethod]
    public void CancelTradeSession_IsIdempotentAndConservesEscrow()
    {
        var (firstInventory, secondInventory, _, _, script) = CreatePreparedTradeScenario();
        var firstOffer = script.SelfContainer;
        var secondOffer = script.TargetContainer;

        script.CancelTradeSession();
        script.CancelTradeSession();

        firstInventory.Items.GetCountById(100).Should().Be(1);
        secondInventory.Items.GetCountById(101).Should().Be(1);
        TotalCount(100, firstInventory.Items, secondInventory.Items, firstOffer.Items, secondOffer.Items).Should().Be(1);
        TotalCount(101, firstInventory.Items, secondInventory.Items, firstOffer.Items, secondOffer.Items).Should().Be(1);
        script.TradeSession.Should().BeFalse();
    }

    [TestMethod]
    public void FinishTradeSession_UsesComposedExchangeToCompleteTrade()
    {
        var (firstInventory, secondInventory, _, _, script) = CreatePreparedTradeScenario();
        var firstOffer = script.SelfContainer;
        var secondOffer = script.TargetContainer;

        script.FinishTradeSession();

        firstInventory.Items.GetCountById(101).Should().Be(1);
        secondInventory.Items.GetCountById(100).Should().Be(1);
        firstOffer.Items.TakenSlots.Should().Be(0);
        secondOffer.Items.TakenSlots.Should().Be(0);
        script.TradeSession.Should().BeFalse();
    }

    [TestMethod]
    public void TargetDestroy_ForwardsCancellationToOwner()
    {
        var (firstInventory, secondInventory, _, second, script) = CreatePreparedTradeScenario();
        var targetScript = CreateScript(second);
        var session = GetField(script, "_tradeSession");
        SetProperty(session!, "TargetScript", targetScript);
        SetField(targetScript, "_linkedTradeSession", session!);

        targetScript.OnDestroy();

        firstInventory.Items.GetCountById(100).Should().Be(1);
        secondInventory.Items.GetCountById(101).Should().Be(1);
        script.TradeSession.Should().BeFalse();
        GetField(targetScript, "_linkedTradeSession").Should().BeNull();
    }

    [TestMethod]
    public void Destroy_WhenRefundCannotFit_StoresEscrowInPersistentRecoveryContainers()
    {
        var firstInventory = new ComposedTestContainer(0);
        var secondInventory = new ComposedTestContainer(0);
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var firstRewards = CreateRewardContainer(first);
        var secondRewards = CreateRewardContainer(second);
        first.Rewards.Returns(firstRewards);
        second.Rewards.Returns(secondRewards);
        var script = CreatePreparedScript(first, second, first.MoneyPouch, second.MoneyPouch);
        var firstOffer = script.SelfContainer;
        var secondOffer = script.TargetContainer;

        script.OnDestroy();

        script.TradeSession.Should().BeFalse();
        firstRewards.Items.GetCountById(100).Should().Be(1);
        secondRewards.Items.GetCountById(101).Should().Be(1);
        firstOffer.Items.GetCountById(100).Should().Be(0);
        secondOffer.Items.GetCountById(101).Should().Be(0);

        var firstReloadedRewards = CreateRewardContainer(first);
        firstReloadedRewards.Hydrate(firstRewards.Dehydrate());
        firstReloadedRewards.Items.GetCountById(100).Should().Be(1);

        var secondReloadedRewards = CreateRewardContainer(second);
        secondReloadedRewards.Hydrate(secondRewards.Dehydrate());
        secondReloadedRewards.Items.GetCountById(101).Should().Be(1);
    }

    [TestMethod]
    public void TryConserveEscrow_WhenRecoveryCapacityIsInsufficient_PreservesEscrow()
    {
        var firstInventory = new ComposedTestContainer(14);
        var secondInventory = new ComposedTestContainer(14);
        var firstRewards = new ComposedTestContainer(14);
        var secondRewards = new ComposedTestContainer(0);
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory), firstRewards);
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory), secondRewards);
        var firstOffer = new ComposedTestContainer(StorageType.Normal, 14);
        var secondOffer = new ComposedTestContainer(StorageType.Normal, 14);
        firstOffer.Items.Add(new TestItem(100, 1)).Should().BeTrue();
        secondOffer.Items.Add(new TestItem(101, 1)).Should().BeTrue();
        secondOffer.Items.Add(new TestItem(102, 2)).Should().BeTrue();
        CreateTradeExchange().TryConserveEscrow(first, firstOffer.Items, second, secondOffer.Items).Should().BeFalse();

        firstRewards.Items.GetCountById(100).Should().Be(0);
        firstOffer.Items.GetCountById(100).Should().Be(1);
        secondRewards.Items.GetCountById(101).Should().Be(0);
        secondRewards.Items.GetCountById(102).Should().Be(0);
        secondOffer.Items.GetCountById(101).Should().Be(1);
        secondOffer.Items.GetCountById(102).Should().Be(2);
    }

    [TestMethod]
    public async Task FinishAndCancelRace_ConservesEscrow()
    {
        var (firstInventory, secondInventory, _, _, script) = CreatePreparedTradeScenario();
        var firstOffer = script.SelfContainer;
        var secondOffer = script.TargetContainer;
        using var start = new Barrier(3);
        var finish = Task.Run(() =>
        {
            start.SignalAndWait();
            script.FinishTradeSession();
        });
        var cancel = Task.Run(() =>
        {
            start.SignalAndWait();
            script.CancelTradeSession();
        });

        start.SignalAndWait();
        await Task.WhenAll(finish, cancel);

        TotalCount(100, firstInventory.Items, secondInventory.Items, firstOffer.Items, secondOffer.Items).Should().Be(1);
        TotalCount(101, firstInventory.Items, secondInventory.Items, firstOffer.Items, secondOffer.Items).Should().Be(1);
        script.TradeSession.Should().BeFalse();
    }

    [TestMethod]
    public async Task IndependentTrades_CompleteWithoutCrossSessionInterference()
    {
        var firstAInventory = new ComposedTestContainer(14);
        var secondAInventory = new ComposedTestContainer(14);
        var firstBInventory = new ComposedTestContainer(14);
        var secondBInventory = new ComposedTestContainer(14);
        var firstA = CreateCharacter(firstAInventory, new TestMoneyPouch(firstAInventory));
        var secondA = CreateCharacter(secondAInventory, new TestMoneyPouch(secondAInventory));
        var firstB = CreateCharacter(firstBInventory, new TestMoneyPouch(firstBInventory));
        var secondB = CreateCharacter(secondBInventory, new TestMoneyPouch(secondBInventory));
        var scriptA = CreatePreparedScript(firstA, secondA, firstA.MoneyPouch, secondA.MoneyPouch);
        var scriptB = CreatePreparedScript(firstB, secondB, firstB.MoneyPouch, secondB.MoneyPouch);
        using var start = new Barrier(3);
        var firstTrade = Task.Run(() =>
        {
            start.SignalAndWait();
            scriptA.FinishTradeSession();
        });
        var secondTrade = Task.Run(() =>
        {
            start.SignalAndWait();
            scriptB.FinishTradeSession();
        });

        start.SignalAndWait();
        await Task.WhenAll(firstTrade, secondTrade);

        firstAInventory.Items.GetCountById(101).Should().Be(1);
        secondAInventory.Items.GetCountById(100).Should().Be(1);
        firstBInventory.Items.GetCountById(101).Should().Be(1);
        secondBInventory.Items.GetCountById(100).Should().Be(1);
        scriptA.TradeSession.Should().BeFalse();
        scriptB.TradeSession.Should().BeFalse();
    }

    [TestMethod]
    public void FinishTradeSession_OfferRevisionChangeInvalidatesAcceptance()
    {
        var firstInventory = new ComposedTestContainer(14);
        var secondInventory = new ComposedTestContainer(14);
        var firstMoneyPouch = new TestMoneyPouch(firstInventory);
        var secondMoneyPouch = new TestMoneyPouch(secondInventory);
        var first = CreateCharacter(firstInventory, firstMoneyPouch);
        var second = CreateCharacter(secondInventory, secondMoneyPouch);
        var script = CreatePreparedScript(first, second, firstMoneyPouch, secondMoneyPouch);

        script.SelfContainer.Items.Add(new TestItem(100, 1)).Should().BeTrue();
        script.FinishTradeSession();

        script.TradeSession.Should().BeTrue();
        script.SelfAccepted.Should().BeFalse();
        firstInventory.Items.TakenSlots.Should().Be(0);
        secondInventory.Items.TakenSlots.Should().Be(0);
    }

    private static (ComposedTestContainer FirstInventory, ComposedTestContainer SecondInventory,
        ICharacter First, ICharacter Second, TradingCharacterScript Script) CreatePreparedTradeScenario()
    {
        var firstInventory = new ComposedTestContainer(14);
        var secondInventory = new ComposedTestContainer(14);
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        return (firstInventory, secondInventory, first, second,
            CreatePreparedScript(first, second, first.MoneyPouch, second.MoneyPouch));
    }

    private static TradingCharacterScript CreatePreparedScript(
        ICharacter first,
        ICharacter second,
        IMoneyPouchContainer firstMoneyPouch,
        IMoneyPouchContainer secondMoneyPouch)
    {
        var script = CreateScript(first);
        var firstOffer = new TradingCharacterScript.TradeContainer();
        var secondOffer = new TradingCharacterScript.TradeContainer();
        firstOffer.Items.Add(new TestItem(100, 1)).Should().BeTrue();
        secondOffer.Items.Add(new TestItem(101, 1)).Should().BeTrue();

        var stateType = typeof(TradingCharacterScript).GetNestedType("TradeSessionState", BindingFlags.NonPublic)!;
        var state = Activator.CreateInstance(
            stateType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [script, second],
            culture: null)!;

        SetField(script, "_tradeSession", state);
        SetProperty(script, "TradeSession", true);
        SetProperty(script, "Target", second);
        SetProperty(script, "SelfContainer", firstOffer);
        SetProperty(script, "TargetContainer", secondOffer);
        SetProperty(script, "SelfAccepted", true);
        SetProperty(script, "TargetAccepted", true);
        SetProperty(script, "SelfAcceptedContainerRevision", firstOffer.Revision);
        SetProperty(script, "TargetAcceptedContainerRevision", secondOffer.Revision);
        return script;
    }

    private static int TotalCount(int itemId, params IItemContainer[] containers) =>
        containers.Sum(container => container.GetCountById(itemId));

    private static TradeExchange CreateTradeExchange() => new(CreateItemBuilder());

    private static TradingCharacterScript CreateScript(ICharacter character)
    {
        var characterContext = Substitute.For<ICharacterContext>();
        characterContext.Character.Returns(character);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(characterContext);
        return new TradingCharacterScript(contextAccessor, CreateItemBuilder());
    }

    private static ICharacter CreateCharacter(
        IInventoryContainer inventory,
        IMoneyPouchContainer moneyPouch,
        IRewardContainer? rewards = null,
        IBankContainer? bank = null)
    {
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        character.MoneyPouch.Returns(moneyPouch);
        character.Rewards.Returns(rewards);
        character.Bank.Returns(bank);
        character.Widgets.Returns(Substitute.For<IWidgetContainer>());
        character.DisplayName.Returns("Test character");
        return character;
    }

    private static RewardContainer CreateRewardContainer(ICharacter owner)
    {
        owner.EventManager.Returns(Substitute.For<IEventManager>());
        return new RewardContainer(owner, CreateItemBuilder());
    }

    private static IItemBuilder CreateItemBuilder()
    {
        return new TestItemBuilder();
    }

    private static void SetField(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

    private static object? GetField(object target, string name) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);

    private static void SetProperty(object target, string name, object? value) =>
        target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(target, value);

    private static object? GetProperty(object target, string name) =>
        target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target);

    private sealed class TestMoneyPouch : ComposedTestContainer, IMoneyPouchContainer
    {
        private readonly IInventoryContainer _overflowInventory;
        public bool FailNextStorageAdd { get; set; }
        public TestMoneyPouch(IInventoryContainer overflowInventory) : base(StorageType.AlwaysStack, 1, 0, publishItemChanges: false)
        {
            _overflowInventory = overflowInventory;
            Container.Storage.ReplaceState([new TestItem(995, 0, stackable: true)]);
        }
        public string Examine => Count.ToString();
        public int Count => Items[0]?.Count ?? 0;
        public bool HasSpaceForCoins(int count) => count > 0 && (long)Count + count <= int.MaxValue;
        public bool Contains(int id) => Items.Contains(id);
        public bool Contains(int id, int count) => Items.Contains(id, count);

        public bool Add(int count)
        {
            if (count <= 0) return false;
            var pouchCount = Math.Min(int.MaxValue - Count, count);
            if (pouchCount > 0 && !Items.Add(new TestItem(995, pouchCount, stackable: true))) return false;
            var overflow = count - pouchCount;
            return overflow == 0 || _overflowInventory.Items.Add(new TestItem(995, overflow, stackable: true));
        }
        public bool TryAddExact(int count)
        {
            var transaction = new ItemContainerTransaction();
            EnlistIn(transaction);
            return transaction.TryExecute(tx => TryStageAddExact(tx, count));
        }
        public bool TryStageAddExact(IItemContainerTransaction transaction, int count)
        {
            if (FailNextStorageAdd) { FailNextStorageAdd = false; return false; }
            if (count <= 0) return false;
            var pouchCount = Math.Min(int.MaxValue - Count, count);
            var overflow = count - pouchCount;
            if (overflow > 0 && !_overflowInventory.Items.HasSpaceFor(new TestItem(995, overflow, stackable: true))) return false;
            if (pouchCount > 0 && !transaction.TryAddRange(Items.Mutations, [new TestItem(995, pouchCount, stackable: true)])) return false;
            if (overflow > 0 && !transaction.TryAddRange(_overflowInventory.Items.Mutations,
                    [new TestItem(995, overflow, stackable: true)])) return false;
            if (pouchCount > 0) transaction.OnCommitted(() => OnUpdate());
            return true;
        }
        public void EnlistIn(IItemContainerTransaction transaction)
        {
            transaction.Include(Items.Mutations);
            transaction.Include(_overflowInventory.Items.Mutations);
        }
        public bool AddFromInventory(int count) => false;
        public bool MoveToInventory(int count) => false;
        public int Remove(int count)
        {
            var removed = Items.Remove(new TestItem(995, count, stackable: true));
            var remaining = count - removed;
            return remaining <= 0 ? removed : removed + _overflowInventory.Items.Remove(new TestItem(995, remaining, stackable: true));
        }
        public bool TryRemoveExact(int count)
        {
            var transaction = new ItemContainerTransaction();
            EnlistIn(transaction);
            return transaction.TryExecute(tx => TryStageRemoveExact(tx, count));
        }
        public bool TryStageRemoveExact(IItemContainerTransaction transaction, int count)
        {
            if (count <= 0) return false;
            var pouchCount = Math.Min(Count, count); var overflow = count - pouchCount;
            if (overflow > _overflowInventory.Items.GetCountById(995)) return false;
            if (pouchCount > 0 && !transaction.TryRemoveExact(Items.Mutations, new TestItem(995, pouchCount, stackable: true), 0)) return false;
            if (overflow > 0 && !transaction.TryRemoveExact(_overflowInventory.Items.Mutations,
                    new TestItem(995, overflow, stackable: true), -1)) return false;
            transaction.OnCommitted(() => OnUpdate());
            return true;
        }
    }
    private sealed class TestItem : IItem
    {
        public int Id { get; }
        public long[] ExtraData => [];
        public int Count { get; set; }
        public string Name => $"Test item {Id}";
        public IItemDefinition ItemDefinition { get; }
        public IEquipmentDefinition EquipmentDefinition { get; } = Substitute.For<IEquipmentDefinition>();
        public IItemScript ItemScript { get; }
        public IEquipmentScript EquipmentScript { get; } = Substitute.For<IEquipmentScript>();
        public TestItem(int id, int count, bool stackable = false, bool noted = false)
        {
            Id = id;
            Count = count;
            var definition = Substitute.For<IItemDefinition>();
            definition.Stackable.Returns(stackable);
            definition.Noted.Returns(noted);
            ItemDefinition = definition;
            var script = Substitute.For<IItemScript>();
            script.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(info =>
            {
                var left = info.ArgAt<IItem>(0);
                var right = info.ArgAt<IItem>(1);
                return left.Id == right.Id && left.ExtraData.SequenceEqual(right.ExtraData) &&
                       (info.ArgAt<bool>(2) || left.ItemDefinition.Stackable || left.ItemDefinition.Noted);
            });
            ItemScript = script;
        }

        public IItem Clone() => new TestItem(
            Id,
            Count,
            ItemDefinition.Stackable,
            ItemDefinition.Noted);

        public IItem Clone(int newCount) => new TestItem(
            Id,
            newCount,
            ItemDefinition.Stackable,
            ItemDefinition.Noted);

        public bool Equals(IItem otherItem, bool ignoreCount = true)
        {
            return otherItem != null && Id == otherItem.Id && ExtraData.SequenceEqual(otherItem.ExtraData) &&
                   (ignoreCount || Count == otherItem.Count);
        }

        public string? SerializeExtraData() => null;
    }

    private sealed class TestItemBuilder : IItemBuilder, IItemId, IItemOptional
    {
        private int _id;
        private int _count = 1;

        public IItemId Create() => this;

        public IItemOptional WithId(int id)
        {
            _id = id;
            return this;
        }

        public IItemOptional WithCount(int count)
        {
            _count = count;
            return this;
        }

        public IItemOptional WithExtraData(string data) => this;

        public IItem Build() => new TestItem(_id, _count, stackable: _id == 995);
    }
}
