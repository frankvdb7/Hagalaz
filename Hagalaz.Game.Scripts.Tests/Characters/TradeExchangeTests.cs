using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Characters;
using DefaultWidgetScript = Hagalaz.Game.Scripts.Model.Widgets.DefaultWidgetScript;
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
        var first = CreateCharacter(firstInventory);
        var second = CreateCharacter(secondInventory);
        var firstOffer = new TradingCharacterScript.TradeContainer();
        var secondOffer = new TradingCharacterScript.TradeContainer();
        AddOfferItem(firstOffer, new TestItem(100, 20, stackable: true)).Should().BeTrue();
        AddOfferItem(firstOffer, new TestItem(101, 1)).Should().BeTrue();
        AddOfferItem(secondOffer, new TestItem(995, 250, stackable: true)).Should().BeTrue();

        var result = TryCompleteTrade(first, firstOffer, second, secondOffer);

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
        var first = CreateCharacter(firstInventory);
        var second = CreateCharacter(secondInventory);
        var firstOffer = new TradingCharacterScript.TradeContainer();
        var secondOffer = new TradingCharacterScript.TradeContainer();
        AddOfferItem(firstOffer, new TestItem(100, 1)).Should().BeTrue();

        var result = TryCompleteTrade(first, firstOffer, second, secondOffer);

        result.Should().BeFalse();
        firstInventory.Items.TakenSlots.Should().Be(0);
        secondInventory.Items.TakenSlots.Should().Be(0);
        firstOffer.Items.GetCountById(100).Should().Be(1);
    }

    [TestMethod]
    public void TryStageCompletion_MissingOfferParticipantRejectsAndRollsBack()
    {
        CreateTradeParticipants(out var firstInventory, out var secondInventory, out var first, out var second,
            out var firstOffer, out var secondOffer);
        AddOfferItem(firstOffer, new TestItem(100, 1)).Should().BeTrue();

        using (ItemContainerTransaction.Begin(secondOffer.MutableItems, first.MoneyPouch, second.MoneyPouch))
        {
            var action = () => CreateTradeExchange().TryStageCompletion(first, firstOffer, second, secondOffer);
            action.Should().Throw<InvalidOperationException>();
        }

        firstOffer.Items.GetCountById(100).Should().Be(1);
        secondInventory.Items.GetCountById(100).Should().Be(0);
    }

    [TestMethod]
    public void TryStageCompletion_MissingRecipientInventoryRejectsAndRollsBack()
    {
        CreateTradeParticipants(out var firstInventory, out var secondInventory, out var first, out var second,
            out var firstOffer, out var secondOffer);
        AddOfferItem(secondOffer, new TestItem(100, 1)).Should().BeTrue();

        using (ItemContainerTransaction.Begin(firstOffer.MutableItems, secondOffer.MutableItems, second.MoneyPouch))
        {
            var action = () => CreateTradeExchange().TryStageCompletion(first, firstOffer, second, secondOffer);
            action.Should().Throw<InvalidOperationException>();
        }

        secondOffer.Items.GetCountById(100).Should().Be(1);
        firstInventory.Items.GetCountById(100).Should().Be(0);
    }

    [TestMethod]
    public void TryStageCompletion_CoinOnlyTradeRequiresMoneyPouchParticipant()
    {
        var firstInventory = new ComposedTestContainer(4);
        var secondInventory = new ComposedTestContainer(4);
        var first = CreateCharacter(firstInventory);
        var second = CreateCharacter(secondInventory);
        var firstOffer = new TradingCharacterScript.TradeContainer();
        var secondOffer = new TradingCharacterScript.TradeContainer();
        AddOfferItem(firstOffer, new TestItem(995, 5, stackable: true)).Should().BeTrue();

        using (ItemContainerTransaction.Begin(firstOffer.MutableItems, secondOffer.MutableItems, firstInventory, secondInventory))
        {
            var action = () => CreateTradeExchange().TryStageCompletion(first, firstOffer, second, secondOffer);
            action.Should().Throw<InvalidOperationException>();
        }

        firstOffer.Items.GetCountById(995).Should().Be(5);
        second.MoneyPouch.Count.Should().Be(0);
        secondInventory.Items.GetCountById(995).Should().Be(0);
    }

    [TestMethod]
    public void TryStageRefund_MissingParticipantRejectsAndRollsBack()
    {
        CreateTradeParticipants(out var firstInventory, out var secondInventory, out var first, out var second,
            out var firstOffer, out var secondOffer);
        AddOfferItem(firstOffer, new TestItem(100, 1)).Should().BeTrue();

        using (ItemContainerTransaction.Begin(firstOffer.MutableItems, secondOffer.MutableItems, second.MoneyPouch))
        {
            var action = () => CreateTradeExchange().TryStageRefund(first, firstOffer, second, secondOffer);
            action.Should().Throw<InvalidOperationException>();
        }

        firstOffer.Items.GetCountById(100).Should().Be(1);
        firstInventory.Items.GetCountById(100).Should().Be(0);
    }

    [TestMethod]
    public void TryStageEscrowRecovery_MissingSelectedDestinationRejectsAndRollsBack()
    {
        var firstInventory = new ComposedTestContainer(4);
        var secondInventory = new ComposedTestContainer(4);
        var first = CreateCharacter(firstInventory);
        var second = CreateCharacter(secondInventory);
        var firstOffer = new TradingCharacterScript.TradeContainer();
        var secondOffer = new TradingCharacterScript.TradeContainer();
        var rewardItems = new ComposedTestContainer(StorageType.Normal, 4);
        var rewards = Substitute.For<IRewardContainer>();
        rewards.Items.Returns(rewardItems.Items);
        first.Rewards.Returns(rewards);
        AddOfferItem(firstOffer, new TestItem(100, 1)).Should().BeTrue();

        using (ItemContainerTransaction.Begin(firstOffer.MutableItems, secondOffer.MutableItems))
        {
            CreateTradeExchange().TryStageEscrowRecovery(first, firstOffer, second, secondOffer).Should().BeFalse();
        }

        firstOffer.Items.GetCountById(100).Should().Be(1);
        rewardItems.Items.GetCountById(100).Should().Be(0);
    }

    [TestMethod]
    public void CommitTrade_PublishesAfterBothRecipientsAndEscrowReachFinalState()
    {
        CreateTradeParticipants(out var firstInventory, out var secondInventory, out var first, out var second,
            out var firstOffer, out var secondOffer);
        AddOfferItem(firstOffer, new TestItem(100, 1)).Should().BeTrue();
        AddOfferItem(secondOffer, new TestItem(101, 1)).Should().BeTrue();
        var firstPublicationSawFinalState = false;
        firstInventory.OnUpdateAction = () =>
        {
            firstInventory.CanAcquireSynchronizationLockFromOtherThread().Should().BeTrue();
            firstInventory.Items.GetCountById(101).Should().Be(1);
            secondInventory.Items.GetCountById(100).Should().Be(1);
            firstOffer.Items.TakenSlots.Should().Be(0);
            secondOffer.Items.TakenSlots.Should().Be(0);
            firstPublicationSawFinalState = true;
        };

        var result = TryCompleteTrade(first, firstOffer, second, secondOffer);
        result.Should().BeTrue();

        firstPublicationSawFinalState.Should().BeTrue();
    }

    [TestMethod]
    public void CommitTrade_WhenLaterPouchStorageAddFails_RestoresStateWithoutPublication()
    {
        var firstInventory = new ComposedTestContainer(4);
        var secondInventory = new ComposedTestContainer(4);
        var failSecondPouchStorageAdd = false;
        var first = CreateCharacter(firstInventory);
        var second = CreateCharacter(secondInventory, moneyPouchItemBuilder: new TestItemBuilder((id, count) =>
            new TestItem(failSecondPouchStorageAdd ? id + 1 : id, count, stackable: id == 995)));
        var firstMoneyPouch = first.MoneyPouch;
        var secondMoneyPouch = second.MoneyPouch;
        var firstOffer = new TradingCharacterScript.TradeContainer();
        var secondOffer = new TradingCharacterScript.TradeContainer();
        AddOfferItem(firstOffer, new TestItem(995, 5, stackable: true)).Should().BeTrue();
        AddOfferItem(secondOffer, new TestItem(995, 7, stackable: true)).Should().BeTrue();
        var publicationOccurred = false;
        firstInventory.OnUpdateAction = () =>
        {
            publicationOccurred = true;
        };
        failSecondPouchStorageAdd = true;

        var result = TryCompleteTrade(first, firstOffer, second, secondOffer);
        result.Should().BeFalse();

        publicationOccurred.Should().BeFalse();
        first.DidNotReceive().SendChatMessage(Arg.Any<string>());
        second.DidNotReceive().SendChatMessage(Arg.Any<string>());
        first.EventManager.DidNotReceive().SendEvent(Arg.Any<MoneyPouchChangedEvent>());
        second.EventManager.DidNotReceive().SendEvent(Arg.Any<MoneyPouchChangedEvent>());
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
        var character = CreateCharacter(inventory);
        var moneyPouch = character.MoneyPouch;
        moneyPouch.Add(25).Should().BeTrue();
        var offer = new TradingCharacterScript.TradeContainer();
        var publicationSawFinalState = false;
        character.When(owner => owner.SendChatMessage(Arg.Any<string>())).Do(_ =>
        {
            inventory.CanAcquireSynchronizationLockFromOtherThread().Should().BeTrue();
            offer.Items.GetCountById(995).Should().Be(25);
            moneyPouch.Count.Should().Be(0);
            inventory.Items.GetCountById(995).Should().Be(0);
            publicationSawFinalState = true;
        });

        CreateTradeExchange().TryOfferMoneyFromPouch(character, offer, new TestItem(995, 25, stackable: true)).Should().BeTrue();

        publicationSawFinalState.Should().BeTrue();
        offer.Revision.Should().Be(2);
    }

    [TestMethod]
    public void TryOfferMoneyFromPouch_WhenCoinsAreSplitBetweenPouchAndInventory_TransfersCompleteAmount()
    {
        var inventory = new ComposedTestContainer(4);
        inventory.Items.Add(new TestItem(995, 75, stackable: true)).Should().BeTrue();
        var character = CreateCharacter(inventory);
        var moneyPouch = character.MoneyPouch;
        moneyPouch.Add(25).Should().BeTrue();
        var offer = new TradingCharacterScript.TradeContainer();

        var result = CreateTradeExchange().TryOfferMoneyFromPouch(character, offer,
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
        var character = CreateCharacter(inventory);
        var moneyPouch = character.MoneyPouch;
        moneyPouch.Add(int.MaxValue - 5).Should().BeTrue();
        var offer = new TradingCharacterScript.TradeContainer();
        AddOfferItem(offer, new TestItem(995, 10, stackable: true)).Should().BeTrue();
        var publicationSawFinalState = false;
        inventory.OnUpdateAction = () =>
        {
            inventory.CanAcquireSynchronizationLockFromOtherThread().Should().BeTrue();
            offer.Items.GetCountById(995).Should().Be(0);
            moneyPouch.Count.Should().Be(int.MaxValue);
            inventory.Items.GetCountById(995).Should().Be(5);
            publicationSawFinalState = true;
        };

        CreateTradeExchange().TryReturnMoneyToPouch(character, offer, new TestItem(995, 10, stackable: true), -1).Should().BeTrue();

        publicationSawFinalState.Should().BeTrue();
        offer.Revision.Should().Be(3);
    }

    [TestMethod]
    public void TryExchange_WhenPouchIsFull_UsesInventoryForCoinOverflow()
    {
        var firstInventory = new ComposedTestContainer(1);
        var secondInventory = new ComposedTestContainer(1);
        var first = CreateCharacter(firstInventory);
        var firstMoneyPouch = first.MoneyPouch;
        firstMoneyPouch.Add(int.MaxValue).Should().BeTrue();
        var second = CreateCharacter(secondInventory);
        var firstOffer = new TradingCharacterScript.TradeContainer();
        var secondOffer = new TradingCharacterScript.TradeContainer();
        AddOfferItem(secondOffer, new TestItem(995, 2, stackable: true)).Should().BeTrue();

        var result = TryCompleteTrade(first, firstOffer, second, secondOffer);

        result.Should().BeTrue();
        firstMoneyPouch.Count.Should().Be(int.MaxValue);
        firstInventory.Items.GetCountById(995).Should().Be(2);
    }

    [TestMethod]
    public void TryExchange_WhenItemAndFullPouchOverflowShareOneSlot_FailsWithoutMutation()
    {
        var firstInventory = new ComposedTestContainer(2);
        firstInventory.Items.Add(new TestItem(200, 1)).Should().BeTrue();
        var first = CreateCharacter(firstInventory);
        var firstMoneyPouch = first.MoneyPouch;
        firstMoneyPouch.Add(int.MaxValue).Should().BeTrue();
        var secondInventory = new ComposedTestContainer(1);
        var second = CreateCharacter(secondInventory);
        var firstOffer = new TradingCharacterScript.TradeContainer();
        var secondOffer = new TradingCharacterScript.TradeContainer();
        AddOfferItem(secondOffer, new TestItem(101, 1)).Should().BeTrue();
        AddOfferItem(secondOffer, new TestItem(995, 1, stackable: true)).Should().BeTrue();

        var result = TryCompleteTrade(first, firstOffer, second, secondOffer);

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
        var first = CreateCharacter(firstInventory);
        var second = CreateCharacter(secondInventory);
        var firstOffer = new TradingCharacterScript.TradeContainer();
        var secondOffer = new TradingCharacterScript.TradeContainer();
        AddOfferItem(firstOffer, new TestItem(200, 1)).Should().BeTrue();
        AddOfferItem(secondOffer, new TestItem(101, 1)).Should().BeTrue();
        AddOfferItem(secondOffer, new TestItem(102, 2)).Should().BeTrue();

        TryCompleteTrade(first, firstOffer, second, secondOffer).Should().BeFalse();
        TryRefundTrade(first, firstOffer, second, secondOffer).Should().BeTrue();

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
        var first = CreateCharacter(firstInventory);
        var second = CreateCharacter(secondInventory);
        var firstOffer = new TradingCharacterScript.TradeContainer();
        var secondOffer = new TradingCharacterScript.TradeContainer();
        AddOfferItem(firstOffer, new TestItem(201, 1)).Should().BeTrue();
        AddOfferItem(firstOffer, new TestItem(100, 1, stackable: true)).Should().BeTrue();
        AddOfferItem(secondOffer, new TestItem(101, 1)).Should().BeTrue();
        var firstInventoryUpdatesBeforeExchange = firstInventory.UpdateCount;

        TryCompleteTrade(first, firstOffer, second, secondOffer).Should().BeFalse();
        firstInventory.UpdateCount.Should().Be(firstInventoryUpdatesBeforeExchange);
        firstInventory.Items.TakenSlots.Should().Be(0);
        secondInventory.Items.GetCountById(100).Should().Be(1);
        secondInventory.Items.GetCountById(101).Should().Be(0);
        firstOffer.Items.GetCountById(201).Should().Be(1);
        firstOffer.Items.GetCountById(100).Should().Be(1);
        secondOffer.Items.GetCountById(101).Should().Be(1);

        secondInventory.Items.Remove(capacityItem).Should().Be(1);
        TryRefundTrade(first, firstOffer, second, secondOffer).Should().BeTrue();

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
        var first = CreateCharacter(firstInventory);
        var second = CreateCharacter(secondInventory);
        var firstOffer = new TradingCharacterScript.TradeContainer();
        var secondOffer = new TradingCharacterScript.TradeContainer();
        AddOfferItem(firstOffer, new TestItem(100, 1)).Should().BeTrue();
        AddOfferItem(secondOffer, new TestItem(995, 125, stackable: true)).Should().BeTrue();

        var result = TryRefundTrade(first, firstOffer, second, secondOffer);

        result.Should().BeTrue();
        firstInventory.Items.GetCountById(100).Should().Be(1);
        second.MoneyPouch.Count.Should().Be(125);
    }

    [TestMethod]
    public void OfferInventoryItem_UsesSharedExactTransferBoundary()
    {
        var firstInventory = new ComposedTestContainer(14);
        var secondInventory = new ComposedTestContainer(14);
        var first = CreateCharacter(firstInventory);
        var second = CreateCharacter(secondInventory);
        var item = new TestItem(102, 5, stackable: true);
        item.ItemScript.CanTradeItem(item, first).Returns(true);
        firstInventory.Items.Add(item).Should().BeTrue();
        var trade = CreatePreparedScript(first, second);

        trade.Handlers.SelfInventoryOffer!(0, ComponentClickType.Option5Click, item.Id, 0).Should().BeTrue();
        trade.Script.SelfIntInputHandler!(3);

        firstInventory.Items.GetCountById(102).Should().Be(2);
        trade.Script.SelfContainer.Items.GetCountById(102).Should().Be(3);
    }

    [TestMethod]
    public async Task FinishTradeSession_ConcurrentCallsTransferOnlyOnce()
    {
        var firstInventory = new ComposedTestContainer(14);
        var secondInventory = new ComposedTestContainer(14);
        var first = CreateCharacter(firstInventory);
        var second = CreateCharacter(secondInventory);

        var trade = CreatePreparedScript(first, second);
        trade.AcceptOffers();
        using var start = new Barrier(3);
        var firstCall = Task.Run(() =>
        {
            start.SignalAndWait();
            trade.AcceptSelfFinal();
        });
        var secondCall = Task.Run(() =>
        {
            start.SignalAndWait();
            trade.AcceptTargetFinal();
        });
        start.SignalAndWait();
        await Task.WhenAll(firstCall, secondCall);
        trade.AcceptSelfFinal();

        firstInventory.Items.GetCountById(101).Should().Be(1);
        secondInventory.Items.GetCountById(100).Should().Be(1);
        trade.Script.TradeSession.Should().BeFalse();
    }

    [TestMethod]
    public void CancelTradeSession_IsIdempotentAndConservesEscrow()
    {
        var (firstInventory, secondInventory, _, _, trade) = CreatePreparedTradeScenario();
        var script = trade.Script;
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
        var (firstInventory, secondInventory, _, _, trade) = CreatePreparedTradeScenario();
        var script = trade.Script;
        var firstOffer = script.SelfContainer;
        var secondOffer = script.TargetContainer;

        trade.CompleteTrade();

        firstInventory.Items.GetCountById(101).Should().Be(1);
        secondInventory.Items.GetCountById(100).Should().Be(1);
        AssertTradeClosed(script, firstOffer.Items, secondOffer.Items);
    }

    [TestMethod]
    public void FinishTradeSession_WhenPublicationFailsAfterCommit_StillCompletesAndCleansUp()
    {
        var (firstInventory, secondInventory, _, _, trade) = CreatePreparedTradeScenario();
        var script = trade.Script;
        var firstOffer = script.SelfContainer;
        var secondOffer = script.TargetContainer;
        var publicationException = FailPublicationAfterCommit(firstInventory);

        Action finish = trade.CompleteTrade;
        finish.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(publicationException);

        firstInventory.Items.GetCountById(101).Should().Be(1);
        secondInventory.Items.GetCountById(100).Should().Be(1);
        firstOffer.Items.TakenSlots.Should().Be(0);
        secondOffer.Items.TakenSlots.Should().Be(0);
        script.TradeSession.Should().BeFalse();

        script.CancelTradeSession();
        trade.AcceptBothFinal();
        TotalCount(100, firstInventory.Items, secondInventory.Items, firstOffer.Items, secondOffer.Items).Should().Be(1);
        TotalCount(101, firstInventory.Items, secondInventory.Items, firstOffer.Items, secondOffer.Items).Should().Be(1);
    }

    [TestMethod]
    public void FinishTradeSession_WhenPouchPublicationFailsAfterCommit_StillCompletesAndCleansUp()
    {
        var firstInventory = new ComposedTestContainer(14);
        var secondInventory = new ComposedTestContainer(14);
        var first = CreateCharacter(firstInventory);
        var second = CreateCharacter(secondInventory);
        var firstMoneyPouch = first.MoneyPouch;
        var publicationException = new InvalidOperationException("pouch publication failed");
        first.When(character => character.SendChatMessage(Arg.Any<string>()))
            .Do(_ => throw publicationException);
        var trade = CreatePreparedScript(first, second);
        var script = trade.Script;
        var firstOffer = script.SelfContainer;
        var secondOffer = script.TargetContainer;
        AddOfferItem(secondOffer, new TestItem(995, 3, stackable: true)).Should().BeTrue();

        Action finish = trade.CompleteTrade;
        finish.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(publicationException);

        firstMoneyPouch.Count.Should().Be(3);
        firstInventory.Items.GetCountById(101).Should().Be(1);
        secondInventory.Items.GetCountById(100).Should().Be(1);
        AssertTradeClosed(script, firstOffer.Items, secondOffer.Items);
    }

    [TestMethod]
    public void CancelTradeSession_WhenRefundPublicationFailsAfterCommit_StillCancelsAndCleansUp()
    {
        var (firstInventory, secondInventory, _, _, trade) = CreatePreparedTradeScenario();
        var script = trade.Script;
        var firstOffer = script.SelfContainer;
        var secondOffer = script.TargetContainer;
        var publicationException = FailPublicationAfterCommit(firstInventory);

        Action cancel = script.CancelTradeSession;
        cancel.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(publicationException);

        firstInventory.Items.GetCountById(100).Should().Be(1);
        secondInventory.Items.GetCountById(101).Should().Be(1);
        firstOffer.Items.TakenSlots.Should().Be(0);
        secondOffer.Items.TakenSlots.Should().Be(0);
        script.TradeSession.Should().BeFalse();

        script.CancelTradeSession();
        TotalCount(100, firstInventory.Items, secondInventory.Items, firstOffer.Items, secondOffer.Items).Should().Be(1);
        TotalCount(101, firstInventory.Items, secondInventory.Items, firstOffer.Items, secondOffer.Items).Should().Be(1);
    }

    [TestMethod]
    public void Destroy_WhenRecoveryPublicationFailsAfterCommit_StillCancelsAndCleansUp()
    {
        var firstInventory = new ComposedTestContainer(0);
        var secondInventory = new ComposedTestContainer(0);
        var firstRewards = new ComposedTestContainer(4);
        var secondRewards = new ComposedTestContainer(4);
        var first = CreateCharacter(firstInventory, firstRewards);
        var second = CreateCharacter(secondInventory, secondRewards);
        var trade = CreatePreparedScript(first, second);
        var script = trade.Script;
        var firstOffer = script.SelfContainer;
        var secondOffer = script.TargetContainer;

        var publicationException = FailPublicationAfterCommit(firstRewards);

        Action destroy = script.OnDestroy;
        destroy.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(publicationException);

        script.TradeSession.Should().BeFalse();
        firstRewards.Items.GetCountById(100).Should().Be(1);
        secondRewards.Items.GetCountById(101).Should().Be(1);
        firstOffer.Items.TakenSlots.Should().Be(0);
        secondOffer.Items.TakenSlots.Should().Be(0);
        TotalCount(100, firstInventory.Items, secondInventory.Items, firstOffer.Items, secondOffer.Items,
            firstRewards.Items, secondRewards.Items).Should().Be(1);
        TotalCount(101, firstInventory.Items, secondInventory.Items, firstOffer.Items, secondOffer.Items,
            firstRewards.Items, secondRewards.Items).Should().Be(1);
    }

    [TestMethod]
    public void TargetDestroy_ForwardsCancellationToOwner()
    {
        var (firstInventory, secondInventory, _, _, trade) = CreatePreparedTradeScenario();
        var script = trade.Script;

        trade.TargetScript.OnDestroy();

        firstInventory.Items.GetCountById(100).Should().Be(1);
        secondInventory.Items.GetCountById(101).Should().Be(1);
        script.TradeSession.Should().BeFalse();
    }

    [TestMethod]
    public void Destroy_WhenRefundCannotFit_StoresEscrowInPersistentRecoveryContainers()
    {
        var firstInventory = new ComposedTestContainer(0);
        var secondInventory = new ComposedTestContainer(0);
        var first = CreateCharacter(firstInventory);
        var second = CreateCharacter(secondInventory);
        var firstRewards = CreateRewardContainer(first);
        var secondRewards = CreateRewardContainer(second);
        first.Rewards.Returns(firstRewards);
        second.Rewards.Returns(secondRewards);
        var trade = CreatePreparedScript(first, second);
        var script = trade.Script;
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
    public void CommitEscrowRecovery_WhenRecoveryCapacityIsInsufficient_PreservesEscrow()
    {
        var firstInventory = new ComposedTestContainer(14);
        var secondInventory = new ComposedTestContainer(14);
        var firstRewards = new ComposedTestContainer(14);
        var secondRewards = new ComposedTestContainer(0);
        var first = CreateCharacter(firstInventory, firstRewards);
        var second = CreateCharacter(secondInventory, secondRewards);
        var firstOffer = new TradingCharacterScript.TradeContainer();
        var secondOffer = new TradingCharacterScript.TradeContainer();
        AddOfferItem(firstOffer, new TestItem(100, 1)).Should().BeTrue();
        AddOfferItem(secondOffer, new TestItem(101, 1)).Should().BeTrue();
        AddOfferItem(secondOffer, new TestItem(102, 2)).Should().BeTrue();
        TryRecoverTrade(first, firstOffer, second, secondOffer).Should().BeFalse();

        firstRewards.Items.GetCountById(100).Should().Be(0);
        firstOffer.Items.GetCountById(100).Should().Be(1);
        secondRewards.Items.GetCountById(101).Should().Be(0);
        secondRewards.Items.GetCountById(102).Should().Be(0);
        secondOffer.Items.GetCountById(101).Should().Be(1);
        secondOffer.Items.GetCountById(102).Should().Be(2);
    }

    [TestMethod]
    public Task FinishAndCancelRace_ConservesEscrow()
    {
        var (_, _, _, _, trade) = CreatePreparedTradeScenario();
        return AssertFinishRaceConservesEscrow(trade, trade.Script.CancelTradeSession);
    }

    [TestMethod]
    public Task FinishAndOwnerDestroyRace_ConservesEscrow()
    {
        var (_, _, _, _, trade) = CreatePreparedTradeScenario();
        return AssertFinishRaceConservesEscrow(trade, trade.Script.OnDestroy);
    }

    [TestMethod]
    public async Task FinishAndTargetDestroyRace_ConservesEscrow()
    {
        var (_, _, _, _, trade) = CreatePreparedTradeScenario();
        await AssertFinishRaceConservesEscrow(trade, trade.TargetScript.OnDestroy);
    }

    [TestMethod]
    public Task FinishAndInterruptRace_ConservesEscrow()
    {
        var (_, _, _, _, trade) = CreatePreparedTradeScenario();
        return AssertFinishRaceConservesEscrow(trade, () => trade.Script.OnInterrupt(new object()));
    }

    private static async Task AssertFinishRaceConservesEscrow(PreparedTrade trade, Action lifecycleOperation)
    {
        trade.AcceptOffers();
        trade.AcceptSelfFinal();
        var script = trade.Script;
        var firstInventory = trade.FirstInventory;
        var secondInventory = trade.SecondInventory;
        var firstOffer = script.SelfContainer;
        var secondOffer = script.TargetContainer;
        using var start = new Barrier(3);
        var finish = Task.Run(() =>
        {
            start.SignalAndWait();
            trade.AcceptTargetFinal();
        });
        var lifecycle = Task.Run(() =>
        {
            start.SignalAndWait();
            lifecycleOperation();
        });

        start.SignalAndWait();
        await Task.WhenAll(finish, lifecycle);

        TotalCount(100, firstInventory, secondInventory, firstOffer.Items, secondOffer.Items).Should().Be(1);
        TotalCount(101, firstInventory, secondInventory, firstOffer.Items, secondOffer.Items).Should().Be(1);
        script.TradeSession.Should().BeFalse();
    }

    [TestMethod]
    public async Task IndependentTrades_CompleteWithoutCrossSessionInterference()
    {
        var firstAInventory = new ComposedTestContainer(14);
        var secondAInventory = new ComposedTestContainer(14);
        var firstBInventory = new ComposedTestContainer(14);
        var secondBInventory = new ComposedTestContainer(14);
        var firstA = CreateCharacter(firstAInventory);
        var secondA = CreateCharacter(secondAInventory);
        var firstB = CreateCharacter(firstBInventory);
        var secondB = CreateCharacter(secondBInventory);
        var tradeA = CreatePreparedScript(firstA, secondA);
        var tradeB = CreatePreparedScript(firstB, secondB);
        using var start = new Barrier(3);
        var firstTrade = Task.Run(() =>
        {
            start.SignalAndWait();
            tradeA.CompleteTrade();
        });
        var secondTrade = Task.Run(() =>
        {
            start.SignalAndWait();
            tradeB.CompleteTrade();
        });

        start.SignalAndWait();
        await Task.WhenAll(firstTrade, secondTrade);

        firstAInventory.Items.GetCountById(101).Should().Be(1);
        secondAInventory.Items.GetCountById(100).Should().Be(1);
        firstBInventory.Items.GetCountById(101).Should().Be(1);
        secondBInventory.Items.GetCountById(100).Should().Be(1);
        tradeA.Script.TradeSession.Should().BeFalse();
        tradeB.Script.TradeSession.Should().BeFalse();
    }

    [TestMethod]
    public void FinishTradeSession_OfferRevisionChangeInvalidatesAcceptance()
    {
        var firstInventory = new ComposedTestContainer(14);
        var secondInventory = new ComposedTestContainer(14);
        var first = CreateCharacter(firstInventory);
        var second = CreateCharacter(secondInventory);
        var trade = CreatePreparedScript(first, second);
        var script = trade.Script;

        trade.AcceptOffers();
        trade.AcceptSelfFinal();
        AddOfferItem(script.SelfContainer, new TestItem(102, 1)).Should().BeTrue();
        trade.AcceptTargetFinal();

        script.TradeSession.Should().BeTrue();
        script.SelfAccepted.Should().BeFalse();
        firstInventory.Items.TakenSlots.Should().Be(0);
        secondInventory.Items.TakenSlots.Should().Be(0);
    }

    [TestMethod]
    public void OfferInventoryItem_WhenPublicationFailsAfterCommit_CannotCompleteWithPreviousAcceptance()
    {
        var firstInventory = new ComposedTestContainer(14);
        var secondInventory = new ComposedTestContainer(14);
        var first = CreateCharacter(firstInventory);
        var second = CreateCharacter(secondInventory);
        var item = new TestItem(102, 1);
        firstInventory.Items.Add(item).Should().BeTrue();
        var inventoryItem = firstInventory.Items[0]!;
        inventoryItem.ItemScript.CanTradeItem(inventoryItem, first).Returns(true);
        var trade = CreatePreparedScript(first, second);
        var script = trade.Script;

        trade.AcceptOffers();
        trade.AcceptSelfFinal();
        script.SelfAccepted.Should().BeTrue();
        var publicationException = FailPublicationAfterCommit(firstInventory);

        trade.Handlers.SelfInventoryOffer!(0, ComponentClickType.Option5Click, item.Id, 0).Should().BeTrue();
        Action offer = () => script.SelfIntInputHandler!(1);
        offer.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(publicationException);

        firstInventory.OnUpdateAction = null;
        script.SelfAccepted.Should().BeFalse();
        script.TargetAccepted.Should().BeFalse();
        trade.AcceptTargetFinal();

        script.TradeSession.Should().BeTrue();
        script.SelfAccepted.Should().BeFalse();
        script.TargetAccepted.Should().BeTrue();
        script.SelfContainer.Items.GetCountById(102).Should().Be(1);
        firstInventory.Items.GetCountById(101).Should().Be(0);
        secondInventory.Items.GetCountById(100).Should().Be(0);
    }

    [TestMethod]
    public void TradeContainer_RevisionChangesOnlyForCommittedStorageMutations()
    {
        var offer = new TradingCharacterScript.TradeContainer();
        var initialRevision = offer.Revision;

        using (ItemContainerTransaction.Begin(offer.MutableItems))
        {
            offer.MutableItems.Add(new TestItem(102, 1)).Should().BeTrue();
        }

        offer.Revision.Should().Be(initialRevision);

        using (var transaction = ItemContainerTransaction.Begin(offer.MutableItems))
        {
            offer.MutableItems.Add(new TestItem(102, 1)).Should().BeTrue();
            transaction.Commit();
        }

        offer.Revision.Should().BeGreaterThan(initialRevision);
    }

    [TestMethod]
    public void FinishTradeSession_WhenExchangeAndRefundCannotFit_ResetsAcceptanceAndPreservesEscrow()
    {
        var firstInventory = new ComposedTestContainer(0);
        var secondInventory = new ComposedTestContainer(0);
        var first = CreateCharacter(firstInventory);
        var second = CreateCharacter(secondInventory);
        var trade = CreatePreparedScript(first, second);
        var script = trade.Script;
        var firstOffer = script.SelfContainer;
        var secondOffer = script.TargetContainer;

        trade.CompleteTrade();

        script.TradeSession.Should().BeTrue();
        script.SelfAccepted.Should().BeFalse();
        script.TargetAccepted.Should().BeFalse();
        firstInventory.Items.TakenSlots.Should().Be(0);
        secondInventory.Items.TakenSlots.Should().Be(0);
        firstOffer.Items.GetCountById(100).Should().Be(1);
        secondOffer.Items.GetCountById(101).Should().Be(1);

        trade.AcceptBothFinal();
        script.TradeSession.Should().BeTrue();
        firstOffer.Items.GetCountById(100).Should().Be(1);
        secondOffer.Items.GetCountById(101).Should().Be(1);
    }

    private static (ComposedTestContainer FirstInventory, ComposedTestContainer SecondInventory,
        ICharacter First, ICharacter Second, PreparedTrade Trade) CreatePreparedTradeScenario()
    {
        var firstInventory = new ComposedTestContainer(14);
        var secondInventory = new ComposedTestContainer(14);
        var first = CreateCharacter(firstInventory);
        var second = CreateCharacter(secondInventory);
        return (firstInventory, secondInventory, first, second, CreatePreparedScript(first, second));
    }

    private static PreparedTrade CreatePreparedScript(ICharacter first, ICharacter second)
    {
        var handlers = new TradeClickHandlers();
        var script = ConfigureTradeCharacter(first, handlers, self: true);
        var targetScript = ConfigureTradeCharacter(second, handlers, self: false);
        first.GetScript<TradingCharacterScript>().Returns(script);
        second.GetScript<TradingCharacterScript>().Returns(targetScript);

        script.StartTradeSession(second);
        AddOfferItem(script.SelfContainer, new TestItem(100, 1)).Should().BeTrue();
        AddOfferItem(script.TargetContainer, new TestItem(101, 1)).Should().BeTrue();

        return new PreparedTrade(first.Inventory.Items, second.Inventory.Items, first, second, script, targetScript, handlers);
    }

    private static TradingCharacterScript ConfigureTradeCharacter(
        ICharacter character, TradeClickHandlers handlers, bool self)
    {
        var characterContext = Substitute.For<ICharacterContext>();
        characterContext.Character.Returns(character);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(characterContext);
        var script = new TradingCharacterScript(contextAccessor, CreateItemBuilder());
        var tradeScript = new TradingCharacterScript.TradeInterfaceScript(contextAccessor);
        var defaultWidgetScript = new DefaultWidgetScript(contextAccessor);
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(TradingCharacterScript.TradeInterfaceScript)).Returns(tradeScript);
        provider.GetService(typeof(DefaultWidgetScript)).Returns(defaultWidgetScript);

        var offerWidget = Substitute.For<IWidget>();
        var overlayWidget = Substitute.For<IWidget>();
        var confirmationWidget = Substitute.For<IWidget>();
        offerWidget.Id.Returns(335);
        overlayWidget.Id.Returns(336);
        confirmationWidget.Id.Returns(334);
        offerWidget.Script.Returns(tradeScript);
        confirmationWidget.Script.Returns(tradeScript);

        var widgets = Substitute.For<IWidgetContainer>();
        widgets.OpenWidget(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IWidgetScript>(), Arg.Any<bool>()).Returns(true);
        widgets.OpenInventoryOverlay(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IWidgetScript>()).Returns(true);
        widgets.GetOpenWidget(Arg.Any<int>()).Returns(call => call.ArgAt<int>(0) switch
        {
            335 => offerWidget,
            336 => overlayWidget,
            334 => confirmationWidget,
            _ => null
        });

        overlayWidget.When(x => x.AttachClickHandler(0, Arg.Any<OnComponentClick>()))
            .Do(call => SetInventoryOfferHandler(handlers, self, call.ArgAt<OnComponentClick>(1)));
        offerWidget.When(x => x.AttachClickHandler(18, Arg.Any<OnComponentClick>()))
            .Do(call => SetOfferAcceptHandler(handlers, self, call.ArgAt<OnComponentClick>(1)));
        confirmationWidget.When(x => x.AttachClickHandler(21, Arg.Any<OnComponentClick>()))
            .Do(call => SetFinalAcceptHandler(handlers, self, call.ArgAt<OnComponentClick>(1)));

        character.Widgets.Returns(widgets);
        character.Configurations.Returns(Substitute.For<IConfigurations>());
        character.ServiceProvider.Returns(provider);
        return script;
    }

    private static void SetInventoryOfferHandler(TradeClickHandlers handlers, bool self, OnComponentClick handler)
    {
        if (self) handlers.SelfInventoryOffer = handler;
        else handlers.TargetInventoryOffer = handler;
    }

    private static void SetOfferAcceptHandler(TradeClickHandlers handlers, bool self, OnComponentClick handler)
    {
        if (self) handlers.SelfOfferAccept = handler;
        else handlers.TargetOfferAccept = handler;
    }

    private static void SetFinalAcceptHandler(TradeClickHandlers handlers, bool self, OnComponentClick handler)
    {
        if (self) handlers.SelfFinalAccept = handler;
        else handlers.TargetFinalAccept = handler;
    }

    private sealed class TradeClickHandlers
    {
        public OnComponentClick? SelfInventoryOffer { get; set; }
        public OnComponentClick? TargetInventoryOffer { get; set; }
        public OnComponentClick? SelfOfferAccept { get; set; }
        public OnComponentClick? TargetOfferAccept { get; set; }
        public OnComponentClick? SelfFinalAccept { get; set; }
        public OnComponentClick? TargetFinalAccept { get; set; }
    }

    private sealed record PreparedTrade(
        IReadOnlyItemContainer FirstInventory,
        IReadOnlyItemContainer SecondInventory,
        ICharacter First,
        ICharacter Second,
        TradingCharacterScript Script,
        TradingCharacterScript TargetScript,
        TradeClickHandlers Handlers)
    {
        public void AcceptOffers()
        {
            Handlers.SelfOfferAccept!(18, ComponentClickType.LeftClick, 0, 0);
            Handlers.TargetOfferAccept!(18, ComponentClickType.LeftClick, 0, 0);
        }

        public void AcceptSelfFinal() => Handlers.SelfFinalAccept!(21, ComponentClickType.LeftClick, 0, 0);

        public void AcceptTargetFinal() => Handlers.TargetFinalAccept!(21, ComponentClickType.LeftClick, 0, 0);

        public void AcceptBothFinal()
        {
            AcceptSelfFinal();
            AcceptTargetFinal();
        }

        public void CompleteTrade()
        {
            AcceptOffers();
            AcceptBothFinal();
        }
    }

    private static void AssertTradeClosed(TradingCharacterScript script, IReadOnlyItemContainer firstOffer, IReadOnlyItemContainer secondOffer)
    {
        firstOffer.TakenSlots.Should().Be(0);
        secondOffer.TakenSlots.Should().Be(0);
        script.TradeSession.Should().BeFalse();
    }

    private static int TotalCount(int itemId, params IReadOnlyItemContainer[] containers) =>
        containers.Sum(container => container.GetCountById(itemId));

    private static bool TryCompleteTrade(ICharacter first, TradingCharacterScript.TradeContainer firstOffer, ICharacter second, TradingCharacterScript.TradeContainer secondOffer)
    {
        using var transaction = ItemContainerTransaction.Begin(firstOffer.MutableItems, secondOffer.MutableItems, first.MoneyPouch, second.MoneyPouch);
        if (!CreateTradeExchange().TryStageCompletion(first, firstOffer, second, secondOffer)) return false;
        transaction.Commit();
        firstOffer.PublishChanges();
        secondOffer.PublishChanges();
        return true;
    }

    private static bool TryRefundTrade(ICharacter first, TradingCharacterScript.TradeContainer firstOffer, ICharacter second, TradingCharacterScript.TradeContainer secondOffer)
    {
        using var transaction = ItemContainerTransaction.Begin(firstOffer.MutableItems, secondOffer.MutableItems, first.MoneyPouch, second.MoneyPouch);
        if (!CreateTradeExchange().TryStageRefund(first, firstOffer, second, secondOffer)) return false;
        transaction.Commit();
        firstOffer.PublishChanges();
        secondOffer.PublishChanges();
        return true;
    }

    private static bool TryRecoverTrade(ICharacter first, TradingCharacterScript.TradeContainer firstOffer, ICharacter second, TradingCharacterScript.TradeContainer secondOffer)
    {
        var participants = new System.Collections.Generic.List<IItemTransactional>
        {
            firstOffer.MutableItems,
            secondOffer.MutableItems
        };
        if (first.Rewards != null) participants.Add(first.Rewards);
        if (first.Bank != null) participants.Add(first.Bank);
        if (second.Rewards != null) participants.Add(second.Rewards);
        if (second.Bank != null) participants.Add(second.Bank);
        using var transaction = ItemContainerTransaction.Begin(participants.ToArray());
        if (!CreateTradeExchange().TryStageEscrowRecovery(first, firstOffer, second, secondOffer)) return false;
        transaction.Commit();
        firstOffer.PublishChanges();
        secondOffer.PublishChanges();
        return true;
    }

    private static bool AddOfferItem(TradingCharacterScript.TradeContainer offer, IItem item)
    {
        var added = offer.AddItem(item);
        if (added) offer.PublishChanges();
        return added;
    }

    private static TradeExchange CreateTradeExchange() => new();

    private static ICharacter CreateCharacter(
        IInventoryContainer inventory,
        IRewardContainer? rewards = null,
        IBankContainer? bank = null,
        IItemBuilder? moneyPouchItemBuilder = null)
    {
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        var eventManager = Substitute.For<IEventManager>();
        character.EventManager.Returns(eventManager);
        var moneyPouch = new MoneyPouchContainer(character, moneyPouchItemBuilder ?? CreateItemBuilder());
        character.MoneyPouch.Returns(moneyPouch);
        character.Rewards.Returns(rewards);
        character.Bank.Returns(bank);
        character.Widgets.Returns(Substitute.For<IWidgetContainer>());
        character.DisplayName.Returns("Test character");
        return character;
    }

    private static void CreateTradeParticipants(
        out ComposedTestContainer firstInventory,
        out ComposedTestContainer secondInventory,
        out ICharacter first,
        out ICharacter second,
        out TradingCharacterScript.TradeContainer firstOffer,
        out TradingCharacterScript.TradeContainer secondOffer)
    {
        firstInventory = new ComposedTestContainer(4);
        secondInventory = new ComposedTestContainer(4);
        first = CreateCharacter(firstInventory);
        second = CreateCharacter(secondInventory);
        firstOffer = new TradingCharacterScript.TradeContainer();
        secondOffer = new TradingCharacterScript.TradeContainer();
    }

    private static RewardContainer CreateRewardContainer(ICharacter owner)
    {
        owner.EventManager.Returns(Substitute.For<IEventManager>());
        return new RewardContainer(owner, CreateItemBuilder());
    }

    private static IItemBuilder CreateItemBuilder()
    {
        return new TestItemBuilder((id, count) => new TestItem(id, count, stackable: id == 995));
    }

    private static InvalidOperationException FailPublicationAfterCommit(ComposedTestContainer publisher)
    {
        var exception = new InvalidOperationException("publication failed");
        publisher.OnUpdateAction = () => throw exception;
        return exception;
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

}
