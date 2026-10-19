using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Characters;
using Hagalaz.Game.Scripts.Equipment.Auras;
using Hagalaz.Game.Scripts.Items.Auras;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Npcs.Familiars;
using Hagalaz.Game.Scripts.Widgets;
using Hagalaz.Game.Scripts.Widgets.Orbs;
using Hagalaz.Game.Scripts.Widgets.Tabs;
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
    public void TryOfferMoneyFromPouch_WhenPouchHasInsufficientCoins_RollsBackOfferMutation()
    {
        var inventory = new ComposedTestContainer(4);
        var character = CreateCharacter(inventory);
        character.MoneyPouch.Add(25).Should().BeTrue();
        var offer = new TradingCharacterScript.TradeContainer();

        var result = CreateTradeExchange().TryOfferMoneyFromPouch(character, offer,
            new TestItem(995, 30, stackable: true));

        result.Should().BeFalse();
        character.MoneyPouch.Count.Should().Be(25);
        inventory.Items.GetCountById(995).Should().Be(0);
        offer.Items.GetCountById(995).Should().Be(0);
    }

    [TestMethod]
    public void TryOfferMoneyFromPouch_WhenOfferIsFull_LeavesPouchUnchanged()
    {
        var inventory = new ComposedTestContainer(4);
        var character = CreateCharacter(inventory);
        character.MoneyPouch.Add(25).Should().BeTrue();
        var offer = new TradingCharacterScript.TradeContainer();
        for (var id = 100; id < 114; id++)
        {
            AddOfferItem(offer, new TestItem(id, 1)).Should().BeTrue();
        }

        var revision = offer.Revision;
        var result = CreateTradeExchange().TryOfferMoneyFromPouch(character, offer,
            new TestItem(995, 25, stackable: true));

        result.Should().BeFalse();
        character.MoneyPouch.Count.Should().Be(25);
        inventory.Items.GetCountById(995).Should().Be(0);
        offer.Items.GetCountById(995).Should().Be(0);
        offer.Revision.Should().Be(revision);
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
    public void TradeSession_MarksBothParticipantsBusyUntilCancelled()
    {
        var first = CreateCharacter(new ComposedTestContainer(14));
        var second = CreateCharacter(new ComposedTestContainer(14));
        var trade = CreateActiveTrade(first, second);

        trade.Script.IsBusy().Should().BeTrue();
        trade.TargetScript.IsBusy().Should().BeTrue();

        trade.Script.CancelTradeSession();

        trade.Script.IsBusy().Should().BeFalse();
        trade.TargetScript.IsBusy().Should().BeFalse();
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
        var postCommitException = finish.Should().Throw<ItemContainerPostCommitPublicationException>().Which;
        postCommitException.InnerException.Should().BeSameAs(publicationException);

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
        var postCommitException = finish.Should().Throw<ItemContainerPostCommitPublicationException>().Which;
        postCommitException.InnerException.Should().BeSameAs(publicationException);

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
        var postCommitException = cancel.Should().Throw<ItemContainerPostCommitPublicationException>().Which;
        postCommitException.InnerException.Should().BeSameAs(publicationException);

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
        var postCommitException = offer.Should().Throw<ItemContainerPostCommitPublicationException>().Which;
        postCommitException.InnerException.Should().BeSameAs(publicationException);

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
    public void TradeScriptDetach_FundedOfferIsRefundedBeforeSnapshotWhenPouchWithdrawalIsRejected()
    {
        var (first, firstInventory, firstMoneyPouch) = CreateCharacterWithProductionContainers(2);
        var (second, _, _) = CreateCharacterWithProductionContainers(4);
        firstMoneyPouch.Add(10).Should().BeTrue();

        var offeredItems = new[] { new TestItem(100, 1), new TestItem(101, 1) };
        for (var slot = 0; slot < offeredItems.Length; slot++)
        {
            var item = offeredItems[slot];
            firstInventory.AddItem(item).Should().BeTrue();
            var storedItem = firstInventory.Items[slot]!;
            storedItem.ItemScript.CanTradeItem(storedItem, first).Returns(true);
        }

        first.Configurations.Returns(Substitute.For<IConfigurations>());
        var moneyPouchClick = CreateMoneyPouchClickHandler(first);
        moneyPouchClick(250, ComponentClickType.Option2Click, 0, 0).Should().BeTrue();
        var pendingPouchInput = first.Widgets.IntInputHandler;
        pendingPouchInput.Should().NotBeNull();

        var trade = CreateActiveTrade(first, second);
        trade.First.IsBusy().Returns(_ => trade.Script.TradeSession);

        for (var slot = 0; slot < offeredItems.Length; slot++)
        {
            var item = offeredItems[slot];
            trade.Handlers.SelfInventoryOffer!(0, ComponentClickType.LeftClick, item.Id, slot).Should().BeTrue();
        }

        firstInventory.Items.TakenSlots.Should().Be(0);
        trade.Script.SelfContainer.Items.GetCountById(100).Should().Be(1);
        trade.Script.SelfContainer.Items.GetCountById(101).Should().Be(1);
        var selfOffer = trade.Script.SelfContainer;

        // Starting a trade does not replace the pending global input handler.
        first.Widgets.IntInputHandler.Should().BeSameAs(pendingPouchInput);
        first.Widgets.IntInputHandler!(5);

        firstInventory.Items.GetCountById(995).Should().Be(0);
        firstMoneyPouch.Count.Should().Be(10);
        trade.Script.TradeSession.Should().BeTrue();

        // Character.OnDetach runs the trade script's detach callback before dehydration.
        trade.Script.OnDetach();

        var persistedInventory = firstInventory.Dehydrate();
        persistedInventory.Select(item => item.ItemId).Should().BeEquivalentTo([100, 101]);
        persistedInventory.Sum(item => item.Count).Should().Be(2);
        var persistedPouch = firstMoneyPouch.Dehydrate();
        persistedPouch.Should().ContainSingle();
        persistedPouch[0].ItemId.Should().Be(995);
        persistedPouch[0].Count.Should().Be(10);
        selfOffer.Items.TakenSlots.Should().Be(0);
        trade.Script.TradeSession.Should().BeFalse();
    }

    [TestMethod]
    public void InactiveAuraUnequip_CancelsTradeBeforeUsingTheRefundSlot()
    {
        AssertUnequipCancelsTradeBeforeUsingRefundSlot(new TestAuraItemScript());
    }

    [TestMethod]
    public void CosmeticAuraUnequip_CancelsTradeBeforeUsingTheRefundSlot()
    {
        AssertUnequipCancelsTradeBeforeUsingRefundSlot(new CosmeticAuraActivated());
    }

    [TestMethod]
    public void MoneyPouchWithdrawal_WhileTradeAmountInputIsPendingPreservesHandler()
    {
        var (first, firstInventory, _) = CreateCharacterWithProductionContainers(2);
        var (second, _, _) = CreateCharacterWithProductionContainers(2);
        var item = new TestItem(100, 3, stackable: true);
        firstInventory.AddItem(item).Should().BeTrue();
        var storedItem = firstInventory.Items[0]!;
        storedItem.ItemScript.CanTradeItem(storedItem, first).Returns(true);

        var trade = CreateActiveTrade(first, second);
        trade.First.IsBusy().Returns(_ => trade.Script.TradeSession);
        trade.Handlers.SelfInventoryOffer!(0, ComponentClickType.Option5Click, storedItem.Id, 0).Should().BeTrue();
        var pendingTradeInput = first.Widgets.IntInputHandler;
        pendingTradeInput.Should().NotBeNull();

        var moneyPouchClick = CreateMoneyPouchClickHandler(first);
        moneyPouchClick(250, ComponentClickType.Option2Click, 0, 0).Should().BeTrue();

        first.Widgets.IntInputHandler.Should().BeSameAs(pendingTradeInput);
        first.Widgets.IntInputHandler!(2);

        trade.Script.SelfContainer.Items.GetCountById(storedItem.Id).Should().Be(2);
        firstInventory.Items.GetCountById(storedItem.Id).Should().Be(1);
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
        var trade = CreateActiveTrade(first, second);
        AddOfferItem(trade.Script.SelfContainer, new TestItem(100, 1)).Should().BeTrue();
        AddOfferItem(trade.Script.TargetContainer, new TestItem(101, 1)).Should().BeTrue();
        return trade;
    }

    private static PreparedTrade CreateActiveTrade(ICharacter first, ICharacter second)
    {
        var handlers = new TradeClickHandlers();
        var script = ConfigureTradeCharacter(first, handlers, self: true);
        var targetScript = ConfigureTradeCharacter(second, handlers, self: false);
        first.GetScript<TradingCharacterScript>().Returns(script);
        second.GetScript<TradingCharacterScript>().Returns(targetScript);

        script.StartTradeSession(second);
        return new PreparedTrade(first.Inventory.Items, second.Inventory.Items, first, second, script, targetScript, handlers);
    }

    private static (ICharacter Character, InventoryContainer Inventory, MoneyPouchContainer MoneyPouch)
        CreateCharacterWithProductionContainers(int capacity)
    {
        var character = Substitute.For<ICharacter>();
        var eventManager = Substitute.For<IEventManager>();
        character.EventManager.Returns(eventManager);
        var inventory = new InventoryContainer(
            character,
            capacity,
            Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(),
            CreateItemBuilder());
        character.Inventory.Returns(inventory);
        var moneyPouch = new MoneyPouchContainer(character, CreateItemBuilder());
        character.MoneyPouch.Returns(moneyPouch);
        character.GameClient.Returns(Substitute.For<IGameClient>());
        character.Widgets.Returns(Substitute.For<IWidgetContainer>());
        character.DisplayName.Returns("Test character");
        return (character, inventory, moneyPouch);
    }

    private static void AssertUnequipCancelsTradeBeforeUsingRefundSlot(IItemScript auraItemScript)
    {
        var (first, firstInventory, _) = CreateCharacterWithProductionContainers(2);
        var (second, _, _) = CreateCharacterWithProductionContainers(4);
        first.Appearance.Returns(Substitute.For<ICharacterAppearance>());
        first.Statistics.Returns(Substitute.For<ICharacterStatistics>());
        first.Configurations.Returns(Substitute.For<IConfigurations>());

        var offeredItem = new TestItem(100, 1);
        firstInventory.AddItem(offeredItem).Should().BeTrue();
        var storedOfferedItem = firstInventory.Items[0]!;
        storedOfferedItem.ItemScript.CanTradeItem(storedOfferedItem, first).Returns(true);
        var trade = CreateActiveTrade(first, second);
        var selfOffer = trade.Script.SelfContainer;
        trade.Handlers.SelfInventoryOffer!(0, ComponentClickType.LeftClick, storedOfferedItem.Id, 0).Should().BeTrue();
        firstInventory.Items.GetCountById(storedOfferedItem.Id).Should().Be(0);
        selfOffer.Items.GetCountById(storedOfferedItem.Id).Should().Be(1);

        first.When(character => character.Interrupt(Arg.Any<object>()))
            .Do(call => trade.Script.OnInterrupt(call.ArgAt<object>(0)));
        var equipment = new EquipmentContainer(first, 14, CreateItemBuilder());
        first.Equipment.Returns(equipment);
        var aura = new TestItem(300, 1, itemScript: auraItemScript, equipmentScript: new TestAuraEquipmentScript(),
            extraData: [0, 0]);
        equipment.TryRestoreEquippedItem(EquipmentSlot.Hat, aura).Should().BeTrue();

        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(first);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(context);
        var widget = Substitute.For<IWidget>();
        OnComponentClick? equipmentClick = null;
        widget.When(x => x.AttachClickHandler(6, Arg.Any<OnComponentClick>()))
            .Do(call => equipmentClick = call.ArgAt<OnComponentClick>(1));
        var equipmentTab = new EquipmentTab(contextAccessor);
        equipmentTab.Initialize(widget);
        equipmentTab.OnOpen();

        equipmentClick!(6, ComponentClickType.Option4Click, aura.Id, 0).Should().BeTrue();

        trade.Script.TradeSession.Should().BeFalse();
        firstInventory.Items.GetCountById(offeredItem.Id).Should().Be(1);
        firstInventory.Items.GetCountById(aura.Id).Should().Be(1);
        selfOffer.Items.TakenSlots.Should().Be(0);
    }

    private static OnComponentClick CreateMoneyPouchClickHandler(ICharacter character)
    {
        var characterContext = Substitute.For<ICharacterContext>();
        characterContext.Character.Returns(character);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(characterContext);
        var gameFrame = new GameFrame(
            contextAccessor,
            Substitute.For<IScopedGameMediator>(),
            Substitute.For<IWidgetBuilder>());
        var widget = Substitute.For<IWidget>();
        OnComponentClick? moneyPouchClick = null;
        widget.When(x => x.AttachClickHandler(250, Arg.Any<OnComponentClick>()))
            .Do(call => moneyPouchClick = call.ArgAt<OnComponentClick>(1));
        gameFrame.Initialize(widget);
        gameFrame.Setup();
        return moneyPouchClick!;
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

        var widgets = character.Widgets;
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

    private static bool AddOfferItem(TradingCharacterScript.TradeContainer offer, IItem item)
    {
        var added = offer.AddItem(item);
        if (added) offer.PublishChanges();
        return added;
    }

    private static TradeExchange CreateTradeExchange() => new();

    private static ICharacter CreateCharacter(
        IInventoryContainer inventory,
        IItemBuilder? moneyPouchItemBuilder = null)
    {
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        var eventManager = Substitute.For<IEventManager>();
        character.EventManager.Returns(eventManager);
        var moneyPouch = new MoneyPouchContainer(character, moneyPouchItemBuilder ?? CreateItemBuilder());
        character.MoneyPouch.Returns(moneyPouch);
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
        public long[] ExtraData { get; }
        public int Count { get; set; }
        public string Name => $"Test item {Id}";
        public IItemDefinition ItemDefinition { get; }
        public IEquipmentDefinition EquipmentDefinition { get; } = Substitute.For<IEquipmentDefinition>();
        public IItemScript ItemScript { get; }
        public IEquipmentScript EquipmentScript { get; }
        public TestItem(int id, int count, bool stackable = false, bool noted = false,
            IItemScript? itemScript = null, IEquipmentScript? equipmentScript = null, long[]? extraData = null)
        {
            Id = id;
            Count = count;
            ExtraData = extraData ?? [];
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
            ItemScript = itemScript ?? script;
            EquipmentScript = equipmentScript ?? Substitute.For<IEquipmentScript>();
        }

        public IItem Clone() => new TestItem(
            Id,
            Count,
            ItemDefinition.Stackable,
            ItemDefinition.Noted,
            extraData: ExtraData);

        public IItem Clone(int newCount) => new TestItem(
            Id,
            newCount,
            ItemDefinition.Stackable,
            ItemDefinition.Noted,
            extraData: ExtraData);

        public bool Equals(IItem otherItem, bool ignoreCount = true)
        {
            return otherItem != null && Id == otherItem.Id && ExtraData.SequenceEqual(otherItem.ExtraData) &&
                   (ignoreCount || Count == otherItem.Count);
        }

        public string? SerializeExtraData() => null;
    }

    private sealed class TestAuraItemScript : AuraItemScript
    {
        public override int GetTier(ICharacter character, IItem aura) => 0;
    }

    private sealed class TestAuraEquipmentScript : AuraEquipmentScript
    {
        public override bool CanUnEquipItem(IItem item, ICharacter character) => true;
    }

}
