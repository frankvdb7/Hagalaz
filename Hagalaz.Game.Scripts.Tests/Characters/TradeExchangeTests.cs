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
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Characters;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Characters;

[TestClass]
public sealed class TradeExchangeTests
{
    [TestMethod]
    public void TryExchange_TransfersStackableUnstackableAndMoneyOffers()
    {
        var firstInventory = new TestInventory(14);
        var secondInventory = new TestInventory(14);
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var firstOffer = new TestItemContainer(StorageType.Normal, 14);
        var secondOffer = new TestItemContainer(StorageType.Normal, 14);
        firstOffer.Add(new TestItem(100, 20, stackable: true)).Should().BeTrue();
        firstOffer.Add(new TestItem(101, 1)).Should().BeTrue();
        secondOffer.Add(new TestItem(995, 250, stackable: true)).Should().BeTrue();

        var result = TradeExchange.TryExchange(first, firstOffer, second, secondOffer, CreateItemBuilder());

        result.Should().BeTrue();
        first.MoneyPouch.Count.Should().Be(250);
        second.Inventory.GetCountById(100).Should().Be(20);
        second.Inventory.GetCountById(101).Should().Be(1);
        firstOffer.GetCountById(100).Should().Be(20);
        secondOffer.GetCountById(995).Should().Be(250);
    }

    [TestMethod]
    public void TryExchange_WhenDestinationCapacityChanges_FailsWithoutMutation()
    {
        var firstInventory = new TestInventory(0);
        var secondInventory = new TestInventory(0);
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var firstOffer = new TestItemContainer(StorageType.Normal, 14);
        var secondOffer = new TestItemContainer(StorageType.Normal, 14);
        firstOffer.Add(new TestItem(100, 1)).Should().BeTrue();

        var result = TradeExchange.TryExchange(first, firstOffer, second, secondOffer, CreateItemBuilder());

        result.Should().BeFalse();
        firstInventory.TakenSlots.Should().Be(0);
        secondInventory.TakenSlots.Should().Be(0);
        firstOffer.GetCountById(100).Should().Be(1);
    }

    [TestMethod]
    public void TryExchange_WhenRecipientMutationFails_RollsBackBothRecipients()
    {
        var firstInventory = new TestInventory(14);
        var secondInventory = new TestInventory(14);
        firstInventory.Add(new TestItem(200, 1)).Should().BeTrue();
        firstInventory.FailNextUpdate = true;
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var firstOffer = new TestItemContainer(StorageType.Normal, 14);
        var secondOffer = new TestItemContainer(StorageType.Normal, 14);
        firstOffer.Add(new TestItem(100, 1)).Should().BeTrue();
        secondOffer.Add(new TestItem(101, 1)).Should().BeTrue();

        var result = TradeExchange.TryExchange(first, firstOffer, second, secondOffer, CreateItemBuilder());

        result.Should().BeFalse();
        firstInventory.GetCountById(200).Should().Be(1);
        firstInventory.GetCountById(100).Should().Be(0);
        secondInventory.TakenSlots.Should().Be(0);
        firstOffer.GetCountById(100).Should().Be(1);
        secondOffer.GetCountById(101).Should().Be(1);
    }

    [TestMethod]
    public void TryExchange_WhenPouchIsFull_UsesInventoryForCoinOverflow()
    {
        var firstInventory = new TestInventory(1);
        var secondInventory = new TestInventory(1);
        var firstMoneyPouch = new TestMoneyPouch(firstInventory);
        firstMoneyPouch.Add(int.MaxValue).Should().BeTrue();
        var first = CreateCharacter(firstInventory, firstMoneyPouch);
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var firstOffer = new TestItemContainer(StorageType.Normal, 14);
        var secondOffer = new TestItemContainer(StorageType.Normal, 14);
        secondOffer.Add(new TestItem(995, 2, stackable: true)).Should().BeTrue();

        var result = TradeExchange.TryExchange(first, firstOffer, second, secondOffer, CreateItemBuilder());

        result.Should().BeTrue();
        firstMoneyPouch.Count.Should().Be(int.MaxValue);
        firstInventory.GetCountById(995).Should().Be(2);
    }

    [TestMethod]
    public void TryRefund_ReturnsEscrowToTheOfferingCharacters()
    {
        var firstInventory = new TestInventory(1);
        var secondInventory = new TestInventory(1);
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var firstOffer = new TestItemContainer(StorageType.Normal, 14);
        var secondOffer = new TestItemContainer(StorageType.Normal, 14);
        firstOffer.Add(new TestItem(100, 1)).Should().BeTrue();
        secondOffer.Add(new TestItem(995, 125, stackable: true)).Should().BeTrue();

        var result = TradeExchange.TryRefund(first, firstOffer, second, secondOffer, CreateItemBuilder());

        result.Should().BeTrue();
        firstInventory.GetCountById(100).Should().Be(1);
        second.MoneyPouch.Count.Should().Be(125);
    }

    [TestMethod]
    public void RemoveMoney_WhenPouchIsShort_TracksInventoryCoinsForRollback()
    {
        var inventory = new TestInventory(14);
        var moneyPouch = new TestMoneyPouch(inventory);
        var character = CreateCharacter(inventory, moneyPouch);
        moneyPouch.Add(100).Should().BeTrue();
        inventory.Add(new TestItem(995, 50, stackable: true)).Should().BeTrue();

        var removed = TradeExchange.RemoveMoney(character, 120, out var delta);

        removed.Should().Be(120);
        delta.PouchCount.Should().Be(100);
        delta.InventoryCount.Should().Be(20);
        TradeExchange.RestoreRemovedMoney(delta).Should().BeTrue();
        moneyPouch.Count.Should().Be(100);
        inventory.GetCountById(995).Should().Be(50);
    }

    [TestMethod]
    public async Task FinishTradeSession_ConcurrentCallsTransferOnlyOnce()
    {
        var firstInventory = new TestInventory(14);
        var secondInventory = new TestInventory(14);
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

        firstInventory.GetCountById(101).Should().Be(1);
        secondInventory.GetCountById(100).Should().Be(1);
        script.TradeSession.Should().BeFalse();
    }

    [TestMethod]
    public void CancelTradeSession_IsIdempotentAndConservesEscrow()
    {
        var firstInventory = new TestInventory(14);
        var secondInventory = new TestInventory(14);
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var script = CreatePreparedScript(first, second, first.MoneyPouch, second.MoneyPouch);
        var firstOffer = script.SelfContainer;
        var secondOffer = script.TargetContainer;

        script.CancelTradeSession();
        script.CancelTradeSession();

        firstInventory.GetCountById(100).Should().Be(1);
        secondInventory.GetCountById(101).Should().Be(1);
        TotalCount(100, firstInventory, secondInventory, firstOffer, secondOffer).Should().Be(1);
        TotalCount(101, firstInventory, secondInventory, firstOffer, secondOffer).Should().Be(1);
        script.TradeSession.Should().BeFalse();
    }

    [TestMethod]
    public void TargetDestroy_ForwardsCancellationToOwner()
    {
        var firstInventory = new TestInventory(14);
        var secondInventory = new TestInventory(14);
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var script = CreatePreparedScript(first, second, first.MoneyPouch, second.MoneyPouch);
        var targetScript = CreateScript(second);
        var session = GetField(script, "_tradeSession");
        SetProperty(session!, "TargetScript", targetScript);
        SetField(targetScript, "_linkedTradeSession", session!);

        targetScript.OnDestroy();

        firstInventory.GetCountById(100).Should().Be(1);
        secondInventory.GetCountById(101).Should().Be(1);
        script.TradeSession.Should().BeFalse();
        GetField(targetScript, "_linkedTradeSession").Should().BeNull();
    }

    [TestMethod]
    public void TargetTick_RetriesPendingCancellationAfterCapacityIsRestored()
    {
        var firstInventory = new TestInventory(1);
        var secondInventory = new TestInventory(14);
        firstInventory.Add(new TestItem(200, 1)).Should().BeTrue();
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var script = CreatePreparedScript(first, second, first.MoneyPouch, second.MoneyPouch);
        var targetScript = CreateScript(second);
        var session = GetField(script, "_tradeSession");
        SetProperty(session!, "TargetScript", targetScript);
        SetField(targetScript, "_linkedTradeSession", session!);

        script.OnDestroy();

        script.TradeSession.Should().BeTrue();
        firstInventory.Remove(new TestItem(200, 1)).Should().Be(1);
        targetScript.Tick();

        firstInventory.GetCountById(100).Should().Be(1);
        secondInventory.GetCountById(101).Should().Be(1);
        script.TradeSession.Should().BeFalse();
    }

    [TestMethod]
    public void Destroy_WhenRefundCannotFit_StoresEscrowInPersistentRecoveryContainers()
    {
        var firstInventory = new TestInventory(0);
        var secondInventory = new TestInventory(0);
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
        firstRewards.GetCountById(100).Should().Be(1);
        secondRewards.GetCountById(101).Should().Be(1);
        firstOffer.GetCountById(100).Should().Be(0);
        secondOffer.GetCountById(101).Should().Be(0);

        var firstReloadedRewards = CreateRewardContainer(first);
        firstReloadedRewards.Hydrate(firstRewards.Dehydrate());
        firstReloadedRewards.GetCountById(100).Should().Be(1);

        var secondReloadedRewards = CreateRewardContainer(second);
        secondReloadedRewards.Hydrate(secondRewards.Dehydrate());
        secondReloadedRewards.GetCountById(101).Should().Be(1);
    }

    [TestMethod]
    public void FinishTradeSession_WhenCompensationPreservesUnrelatedMutation_CancelsTrade()
    {
        var firstInventory = new TestInventory(14);
        var secondInventory = new TestInventory(14) { FailNextUpdate = true };
        secondInventory.BeforeNextUpdateFailure = () => secondInventory.GetById(100)!.Count++;
        var firstMoneyPouch = new TestMoneyPouch(firstInventory);
        var secondMoneyPouch = new TestMoneyPouch(secondInventory);
        var first = CreateCharacter(firstInventory, firstMoneyPouch);
        var second = CreateCharacter(secondInventory, secondMoneyPouch);
        var script = CreatePreparedScript(first, second, firstMoneyPouch, secondMoneyPouch);
        script.FinishTradeSession();

        script.TradeSession.Should().BeFalse();
        firstInventory.GetCountById(100).Should().Be(1);
        secondInventory.GetCountById(100).Should().Be(1);
        secondInventory.GetCountById(101).Should().Be(1);
    }

    [TestMethod]
    public void Destroy_WhenCompensationRemainsPending_CompletesExchangeThroughRecoveryContainer()
    {
        var firstInventory = new TestInventory(14);
        var secondInventory = new TestInventory(14) { FailUpdateCount = 3 };
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var firstRewards = CreateRewardContainer(first);
        var secondRewards = CreateRewardContainer(second);
        first.Rewards.Returns(firstRewards);
        second.Rewards.Returns(secondRewards);
        var script = CreatePreparedScript(first, second, first.MoneyPouch, second.MoneyPouch);
        var secondOffer = script.TargetContainer;

        script.FinishTradeSession();
        script.TradeSession.Should().BeTrue();

        script.OnDestroy();

        script.TradeSession.Should().BeFalse();
        script.SelfContainer.Should().BeNull();
        script.TargetContainer.Should().BeNull();
        firstInventory.GetCountById(101).Should().Be(0);
        secondInventory.GetCountById(100).Should().Be(0);
        secondInventory.GetCountById(200).Should().Be(0);
        secondRewards.GetCountById(100).Should().Be(1);
        firstRewards.GetCountById(101).Should().Be(1);
        secondRewards.GetCountById(101).Should().Be(0);
        secondOffer.GetCountById(101).Should().Be(0);

        var firstReloadedRewards = CreateRewardContainer(first);
        firstReloadedRewards.Hydrate(firstRewards.Dehydrate());
        firstReloadedRewards.GetCountById(101).Should().Be(1);
    }

    [TestMethod]
    public void TryRefund_WhenCompensationPreservesUnrelatedMutation_ReturnsFailed()
    {
        var firstInventory = new TestInventory(14);
        var secondInventory = new TestInventory(14) { FailNextUpdate = true };
        secondInventory.BeforeNextUpdateFailure = () => secondInventory.GetById(101)!.Count++;
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var firstOffer = new TestItemContainer(StorageType.Normal, 14);
        var secondOffer = new TestItemContainer(StorageType.Normal, 14);
        firstOffer.Add(new TestItem(100, 1)).Should().BeTrue();
        secondOffer.Add(new TestItem(101, 1)).Should().BeTrue();

        var result = TradeExchange.TryRefundDetailed(first, firstOffer, second, secondOffer, CreateItemBuilder());

        result.Status.Should().Be(TradeExchange.TransferStatus.Failed);
        result.Compensation.Should().BeNull();
        firstOffer.GetCountById(100).Should().Be(1);
        secondOffer.GetCountById(101).Should().Be(1);
        secondInventory.GetCountById(101).Should().Be(1);
    }

    [TestMethod]
    public void TryConserveEscrow_WhenSourceRemovalFails_RollsBackExactDestinationDelta()
    {
        var firstInventory = new TestInventory(14);
        var secondInventory = new TestInventory(14);
        var firstRewards = new TestRewardContainer(14);
        var first = CreateCharacter(
            firstInventory,
            new TestMoneyPouch(firstInventory),
            firstRewards);
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var firstOffer = new TestItemContainer(StorageType.Normal, 14);
        var secondOffer = new TestItemContainer(StorageType.Normal, 14);
        firstOffer.Add(new TestItem(100, 1)).Should().BeTrue();
        firstOffer.FailNextUpdate = true;
        firstOffer.BeforeNextUpdateFailure = () => firstRewards.GetById(100)!.Count++;
        TradeExchange.TryConserveEscrow(
            first,
            firstOffer,
            second,
            secondOffer).Should().BeFalse();

        firstOffer.GetCountById(100).Should().Be(1);
        firstRewards.GetCountById(100).Should().Be(1);

        TradeExchange.TryConserveEscrow(
            first,
            firstOffer,
            second,
            secondOffer).Should().BeTrue();

        firstOffer.GetCountById(100).Should().Be(0);
        firstRewards.GetCountById(100).Should().Be(2);
    }

    [TestMethod]
    public void TryConserveEscrow_WhenSecondRecoveryFails_RollsBackFirstRecovery()
    {
        var firstInventory = new TestInventory(14);
        var secondInventory = new TestInventory(14);
        var firstRewards = new TestRewardContainer(14);
        var secondRewards = new TestRewardContainer(14);
        secondRewards.Add(new TestItem(101, 1)).Should().BeTrue();
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory), firstRewards);
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory), secondRewards);
        var firstOffer = new TestItemContainer(StorageType.Normal, 14);
        var secondOffer = new TestItemContainer(StorageType.Normal, 14);
        firstOffer.Add(new TestItem(100, 1)).Should().BeTrue();
        secondOffer.Add(new TestItem(101, 1)).Should().BeTrue();
        secondOffer.FailNextUpdate = true;
        secondOffer.BeforeNextUpdateFailure = () => secondRewards.GetById(101)!.Count++;

        TradeExchange.TryConserveEscrow(first, firstOffer, second, secondOffer).Should().BeFalse();

        firstOffer.GetCountById(100).Should().Be(1);
        firstRewards.GetCountById(100).Should().Be(0);
        secondOffer.GetCountById(101).Should().Be(1);
        secondRewards.GetCountById(101).Should().Be(2);
    }

    [TestMethod]
    public async Task FinishAndCancelRace_ConservesEscrow()
    {
        var firstInventory = new TestInventory(14);
        var secondInventory = new TestInventory(14);
        var first = CreateCharacter(firstInventory, new TestMoneyPouch(firstInventory));
        var second = CreateCharacter(secondInventory, new TestMoneyPouch(secondInventory));
        var script = CreatePreparedScript(first, second, first.MoneyPouch, second.MoneyPouch);
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

        TotalCount(100, firstInventory, secondInventory, firstOffer, secondOffer).Should().Be(1);
        TotalCount(101, firstInventory, secondInventory, firstOffer, secondOffer).Should().Be(1);
        script.TradeSession.Should().BeFalse();
    }

    [TestMethod]
    public async Task IndependentTrades_CompleteWithoutCrossSessionInterference()
    {
        var firstAInventory = new TestInventory(14);
        var secondAInventory = new TestInventory(14);
        var firstBInventory = new TestInventory(14);
        var secondBInventory = new TestInventory(14);
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

        firstAInventory.GetCountById(101).Should().Be(1);
        secondAInventory.GetCountById(100).Should().Be(1);
        firstBInventory.GetCountById(101).Should().Be(1);
        secondBInventory.GetCountById(100).Should().Be(1);
        scriptA.TradeSession.Should().BeFalse();
        scriptB.TradeSession.Should().BeFalse();
    }

    [TestMethod]
    public void FinishTradeSession_OfferRevisionChangeInvalidatesAcceptance()
    {
        var firstInventory = Substitute.For<IInventoryContainer>();
        var secondInventory = Substitute.For<IInventoryContainer>();
        ConfigureEmptyContainer(firstInventory);
        ConfigureEmptyContainer(secondInventory);
        var firstMoneyPouch = CreateEmptyMoneyPouch();
        var secondMoneyPouch = CreateEmptyMoneyPouch();
        var first = CreateCharacter(firstInventory, firstMoneyPouch);
        var second = CreateCharacter(secondInventory, secondMoneyPouch);
        var script = CreatePreparedScript(first, second, firstMoneyPouch, secondMoneyPouch);

        script.SelfContainer.Add(new TestItem(100, 1)).Should().BeTrue();
        script.FinishTradeSession();

        script.TradeSession.Should().BeTrue();
        script.SelfAccepted.Should().BeFalse();
        firstInventory.DidNotReceive().AddRange(Arg.Any<IEnumerable<IItem?>>());
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
        firstOffer.Add(new TestItem(100, 1)).Should().BeTrue();
        secondOffer.Add(new TestItem(101, 1)).Should().BeTrue();

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
        var services = new ServiceCollection()
            .AddSingleton<IItemBuilder>(CreateItemBuilder())
            .BuildServiceProvider();
        owner.ServiceProvider.Returns(services);
        owner.EventManager.Returns(Substitute.For<IEventManager>());
        return new RewardContainer(owner);
    }

    private static IMoneyPouchContainer CreateEmptyMoneyPouch()
    {
        var moneyPouch = Substitute.For<IMoneyPouchContainer>();
        moneyPouch.GetEnumerator().Returns(_ => Enumerable.Empty<IItem?>().GetEnumerator());
        moneyPouch.Count.Returns(0);
        moneyPouch.Add(Arg.Any<int>()).Returns(true);
        return moneyPouch;
    }

    private static void ConfigureEmptyContainer(IInventoryContainer container)
    {
        container.GetEnumerator().Returns(_ => Enumerable.Empty<IItem?>().GetEnumerator());
        container.HasSpaceForRange(Arg.Any<IEnumerable<IItem?>>()).Returns(true);
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

    private class TestItemContainer : TradeItemContainer
    {
        public bool FailNextUpdate { get; set; }

        public int FailUpdateCount { get; set; }

        public Action? BeforeNextUpdateFailure { get; set; }

        public TestItemContainer(StorageType type, int capacity) : base(type, capacity) { }

        public override void OnUpdate(HashSet<int>? slots = null)
        {
            if (!FailNextUpdate && FailUpdateCount == 0)
            {
                return;
            }

            BeforeNextUpdateFailure?.Invoke();
            BeforeNextUpdateFailure = null;
            if (FailUpdateCount > 0)
            {
                FailUpdateCount--;
            }
            else
            {
                FailNextUpdate = false;
            }
            throw new InvalidOperationException("Controlled container failure.");
        }
    }

    private class TestInventory : TestItemContainer, IInventoryContainer
    {
        public TestInventory(int capacity) : base(StorageType.Normal, capacity) { }

        public bool DropItem(IItem item) => false;
    }

    private sealed class TestRewardContainer : TestItemContainer, IRewardContainer
    {
        public TestRewardContainer(int capacity) : base(StorageType.Normal, capacity) { }

        public int Claim(IItem item, int count) => 0;
    }

    private sealed class TestMoneyPouch : TestItemContainer, IMoneyPouchContainer
    {
        private readonly IInventoryContainer _overflowInventory;

        public TestMoneyPouch(IInventoryContainer overflowInventory) : base(StorageType.AlwaysStack, 1)
        {
            _overflowInventory = overflowInventory;
        }

        public string Examine => Count.ToString();

        public int Count => this[0]?.Count ?? 0;

        public bool Add(int count)
        {
            if (count <= 0)
            {
                return false;
            }

            var available = int.MaxValue - Count;
            var inPouch = Math.Min(available, count);
            if (inPouch > 0 && !Add(new TestItem(995, inPouch, stackable: true)))
            {
                return false;
            }

            var overflow = count - inPouch;
            return overflow == 0 || _overflowInventory.Add(new TestItem(995, overflow, stackable: true));
        }

        public MoneyPouchMutation AddForTrade(int count)
        {
            if (count <= 0)
            {
                return MoneyPouchMutation.Empty(succeeded: false);
            }

            var inPouch = Math.Min(int.MaxValue - Count, count);
            var pouchMutation = inPouch > 0
                ? AddRangeForTrade([new TestItem(995, inPouch, stackable: true)])
                : null;
            if (pouchMutation != null && !pouchMutation.Succeeded)
            {
                return new MoneyPouchMutation(false, pouchMutation, null);
            }

            var remaining = count - (pouchMutation?.AppliedCount ?? 0);
            if (remaining <= 0)
            {
                return new MoneyPouchMutation(true, pouchMutation, null);
            }

            var inventoryMutation = _overflowInventory.AddRangeForTrade(
                [new TestItem(995, remaining, stackable: true)]);
            return new MoneyPouchMutation(
                (pouchMutation?.AppliedCount ?? 0) + inventoryMutation.AppliedCount == count,
                pouchMutation,
                inventoryMutation);
        }

        public bool AddFromInventory(int count) => false;

        public bool MoveToInventory(int count) => false;

        public int Remove(int count)
        {
            var removed = base.Remove(new TestItem(995, count, stackable: true));
            var remaining = count - removed;
            return remaining <= 0
                ? removed
                : removed + _overflowInventory.Remove(new TestItem(995, remaining, stackable: true));
        }

        public MoneyPouchMutation RemoveForTrade(int count)
        {
            if (count <= 0)
            {
                return MoneyPouchMutation.Empty(succeeded: false);
            }

            var fromPouch = Math.Min(Count, count);
            var pouchMutation = fromPouch > 0
                ? RemoveCheckedCore(new TestItem(995, fromPouch, stackable: true))
                : null;
            if (pouchMutation != null && !pouchMutation.Succeeded)
            {
                return new MoneyPouchMutation(false, pouchMutation, null);
            }

            var remaining = count - (pouchMutation?.AppliedCount ?? 0);
            if (remaining <= 0)
            {
                return new MoneyPouchMutation(true, pouchMutation, null);
            }

            var inventoryMutation = _overflowInventory.RemoveForTrade(
                new TestItem(995, remaining, stackable: true));
            var removed = (pouchMutation?.AppliedCount ?? 0) + inventoryMutation.AppliedCount;
            return new MoneyPouchMutation(
                removed == count,
                pouchMutation,
                inventoryMutation);
        }
    }

    private sealed class TestItem : IItem
    {
        public int Id { get; }
        public int Count { get; set; }
        public string Name => $"Test item {Id}";
        public IItemDefinition ItemDefinition { get; }
        public IEquipmentDefinition EquipmentDefinition { get; } = Substitute.For<IEquipmentDefinition>();
        public IItemScript ItemScript { get; }
        public IEquipmentScript EquipmentScript { get; } = Substitute.For<IEquipmentScript>();
        public long[] ExtraData => [];

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
                return info.ArgAt<bool>(2) || left.ItemDefinition.Stackable && left.Id == right.Id;
            });
            ItemScript = script;
        }

        public IItem Clone() => new TestItem(Id, Count, ItemDefinition.Stackable, ItemDefinition.Noted);

        public IItem Clone(int newCount) => new TestItem(Id, newCount, ItemDefinition.Stackable, ItemDefinition.Noted);

        public bool Equals(IItem otherItem, bool ignoreCount = true) =>
            otherItem != null && Id == otherItem.Id && (ignoreCount || Count == otherItem.Count);

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
