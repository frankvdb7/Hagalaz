using Hagalaz.Game.Abstractions.Builders.HintIcon;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Minigames.DuelArena;
using Hagalaz.Game.Scripts.Minigames.DuelArena.Interfaces;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Widgets.Tabs;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Minigames.DuelArena;

[TestClass]
public sealed class DuelArenaInteractionRegressionTests
{
    [TestMethod]
    public void StartConfirmationStage_RejectsStakesThatOnlyFitBeforeRestrictedEquipmentIsReturned()
    {
        var duel = CreateDuel(1, 1, additionalSelfItems: 14);
        var restrictedEquipment = CreateItem(300, 1);
        var equipment = Substitute.For<IEquipmentContainer>();
        equipment[EquipmentSlot.Hat].Returns(restrictedEquipment);
        duel.Self.Equipment.Returns(equipment);
        duel.SessionScript.CurrentRules.ToggleRule(DuelRules.Rule.NoHelm);

        Assert.IsTrue(duel.SelfStake!(0, ComponentClickType.Option4Click, duel.SelfItem.Id, 0));
        Assert.IsTrue(duel.TargetStake!(0, ComponentClickType.Option4Click, duel.TargetItem.Id, 0));

        var stakes = new[] { duel.SelfStakeContainer[0]!, duel.TargetStakeContainer[0]! };
        Assert.IsTrue(duel.SelfInventory.Items.HasSpaceForRange([restrictedEquipment]));
        Assert.IsTrue(duel.SelfInventory.Items.HasSpaceForRange(stakes));
        Assert.IsFalse(duel.SelfInventory.Items.HasSpaceForRange([restrictedEquipment, .. stakes]));

        Assert.IsTrue(duel.SelfAccept!(56, ComponentClickType.LeftClick, 0, 0));
        Assert.IsTrue(duel.TargetAccept!(56, ComponentClickType.LeftClick, 0, 0));

        duel.Self.Widgets.DidNotReceive().OpenWidget(1366, 0, Arg.Any<IWidgetScript>(), false);
        duel.Self.Received(1).SendChatMessage("You don't have enough space in your inventory for this duel.");
        duel.Target.Received(1).SendChatMessage("Your opponent doesn't have enough space in their inventory for this duel.");
        Assert.AreEqual(1, duel.SelfStakeContainer.TakenSlots);
        Assert.AreEqual(1, duel.TargetStakeContainer.TakenSlots);
    }

    [TestMethod]
    public void CheckRules_CoinOnlyStakesUsePouchCapacityWhenInventoriesAreFull()
    {
        var duel = CreateDuel(1, 1, selfPouchCoins: 5, targetPouchCoins: 7);
        FillInventories(duel);

        Assert.IsTrue(duel.SelfPouch!(8, ComponentClickType.LeftClick, 0, 0));
        duel.Self.Widgets.IntInputHandler!(5);
        Assert.IsTrue(duel.TargetPouch!(8, ComponentClickType.LeftClick, 0, 0));
        duel.Target.Widgets.IntInputHandler!(7);

        var stakes = new[] { duel.SelfStakeContainer[0]!, duel.TargetStakeContainer[0]! };
        Assert.AreEqual(16, duel.SelfInventory.Items.TakenSlots);
        Assert.AreEqual(16, duel.TargetInventory.Items.TakenSlots);
        Assert.IsTrue(duel.SessionScript.CurrentRules.CheckRules(duel.Self, duel.Target, stakes));
        Assert.IsTrue(duel.SessionScript.CurrentRules.CheckRules(duel.Target, duel.Self, stakes));

        Assert.IsTrue(DuelStakeExchange.TrySettleWinner(duel.Self, duel.SelfStakeContainer, duel.TargetStakeContainer));

        Assert.AreEqual(12, duel.Self.MoneyPouch.Count);
        Assert.AreEqual(0, duel.SelfInventory.Items.GetCountById(995));
        Assert.AreEqual(0, duel.SelfStakeContainer.TakenSlots);
        Assert.AreEqual(0, duel.TargetStakeContainer.TakenSlots);
    }

    [TestMethod]
    public void CheckRules_RequiresInventorySpaceForCoinsThatOverflowPouch()
    {
        var duel = CreateDuel(1, 1, selfPouchCoins: int.MaxValue, targetPouchCoins: 1);
        FillInventories(duel);

        Assert.IsTrue(duel.TargetPouch!(8, ComponentClickType.LeftClick, 0, 0));
        duel.Target.Widgets.IntInputHandler!(1);
        var stakes = new[] { duel.TargetStakeContainer[0]! };

        Assert.IsFalse(duel.SessionScript.CurrentRules.CheckRules(duel.Self, duel.Target, stakes));
        Assert.IsTrue(duel.SessionScript.CurrentRules.CheckRules(duel.Target, duel.Self, stakes));
    }

    [TestMethod]
    public void DuelStageFlow_ActiveCombatBlocksInventoryCreditsAndSettlesWinner()
    {
        var duel = CreateDuel(1, 1);
        for (var index = 0; index < 15; index++)
        {
            Assert.IsTrue(duel.TargetInventory.Items.Add(CreateItem(300 + index, 1)));
        }

        var equippedItem = CreateItem(450, 1);
        var equipment = Substitute.For<IEquipmentContainer>();
        equipment[EquipmentSlot.Hat].Returns(equippedItem);
        duel.Target.Equipment.Returns(equipment);
        var familiar = new FamiliarInventoryContainer(duel.Target, StorageType.Normal, 1, CreateItemBuilder());
        var familiarItem = duel.TargetInventory.Items[1]!;
        Assert.IsTrue(familiar.DepositFromInventory(familiarItem, 1));

        Assert.IsTrue(duel.SelfStake!(0, ComponentClickType.Option4Click, duel.SelfItem.Id, 0));
        Assert.IsTrue(duel.TargetStake!(0, ComponentClickType.Option4Click, duel.TargetItem.Id, 0));

        Assert.AreEqual(14, duel.TargetInventory.Items.TakenSlots);
        var stakes = new[] { duel.SelfStakeContainer[0]!, duel.TargetStakeContainer[0]! };
        Assert.IsTrue(duel.TargetInventory.Items.HasSpaceForRange(stakes));
        Assert.IsFalse(duel.TargetInventory.Items.HasSpaceForRange([equippedItem, familiarItem, .. stakes]));

        var selfConfirmation = Substitute.For<IWidget>();
        var targetConfirmation = Substitute.For<IWidget>();
        var selfConfirmationScript = (DuelConfirmScreenScript)duel.Self.ServiceProvider
            .GetService(typeof(DuelConfirmScreenScript))!;
        var targetConfirmationScript = (DuelConfirmScreenScript)duel.Target.ServiceProvider
            .GetService(typeof(DuelConfirmScreenScript))!;
        selfConfirmation.Script.Returns(selfConfirmationScript);
        targetConfirmation.Script.Returns(targetConfirmationScript);
        duel.Self.Widgets.GetOpenWidget(1366).Returns(selfConfirmation);
        duel.Target.Widgets.GetOpenWidget(1366).Returns(targetConfirmation);

        OnComponentClick? selfConfirm = null;
        OnComponentClick? targetConfirm = null;
        selfConfirmation.When(x => x.AttachClickHandler(24, Arg.Any<OnComponentClick>()))
            .Do(call => selfConfirm = call.ArgAt<OnComponentClick>(1));
        targetConfirmation.When(x => x.AttachClickHandler(24, Arg.Any<OnComponentClick>()))
            .Do(call => targetConfirm = call.ArgAt<OnComponentClick>(1));

        var regionService = Substitute.For<IMapRegionService>();
        regionService.IsAccessible(Arg.Any<ILocation>()).Returns(true);
        duel.Self.ServiceProvider.GetService(typeof(IMapRegionService)).Returns(regionService);
        duel.Self.Prayers.Returns(Substitute.For<IPrayers>());
        duel.Target.Prayers.Returns(Substitute.For<IPrayers>());
        duel.Self.Profile.Returns(Substitute.For<IProfile>());
        duel.Target.Profile.Returns(Substitute.For<IProfile>());
        var selfAccessor = (ICharacterContextAccessor)duel.Self.ServiceProvider
            .GetService(typeof(ICharacterContextAccessor))!;
        var targetAccessor = (ICharacterContextAccessor)duel.Target.ServiceProvider
            .GetService(typeof(ICharacterContextAccessor))!;
        duel.Self.ServiceProvider.GetService(typeof(DuelEndScreenScript))
            .Returns(new DuelEndScreenScript(selfAccessor));
        duel.Target.ServiceProvider.GetService(typeof(DuelEndScreenScript))
            .Returns(new DuelEndScreenScript(targetAccessor));

        DuelArenaCombatScript? losingCombat = null;
        DuelArenaCombatScript? winningCombat = null;
        duel.Self.IsBusy().Returns(_ => losingCombat?.IsBusy() ?? false);
        duel.Target.IsBusy().Returns(_ => winningCombat?.IsBusy() ?? false);
        duel.Self.AddScript(Arg.Any<DuelArenaCombatScript>()).Returns(call =>
        {
            losingCombat = call.ArgAt<DuelArenaCombatScript>(0);
            losingCombat.OnRegistered();
            return losingCombat;
        });
        duel.Target.AddScript(Arg.Any<DuelArenaCombatScript>()).Returns(call =>
        {
            winningCombat = call.ArgAt<DuelArenaCombatScript>(0);
            winningCombat.OnRegistered();
            return winningCombat;
        });

        Assert.IsTrue(duel.SelfAccept!(56, ComponentClickType.LeftClick, 0, 0));
        Assert.IsTrue(duel.TargetAccept!(56, ComponentClickType.LeftClick, 0, 0));
        duel.Self.Widgets.Received(1).OpenWidget(1366, 0, Arg.Any<IWidgetScript>(), false);
        duel.Target.Widgets.Received(1).OpenWidget(1366, 0, Arg.Any<IWidgetScript>(), false);
        Assert.AreSame(selfConfirmation, duel.Self.Widgets.GetOpenWidget(1366));
        Assert.AreSame(targetConfirmation, duel.Target.Widgets.GetOpenWidget(1366));
        Assert.AreSame(selfConfirmationScript, selfConfirmation.Script);
        Assert.AreSame(targetConfirmationScript, targetConfirmation.Script);
        Assert.IsTrue(selfConfirm!(24, ComponentClickType.LeftClick, 0, 0));
        Assert.IsTrue(targetConfirm!(24, ComponentClickType.LeftClick, 0, 0));
        Assert.IsNotNull(losingCombat);
        Assert.IsTrue(winningCombat!.IsBusy());
        Assert.IsTrue(duel.Self.IsBusy());
        Assert.IsTrue(duel.Target.IsBusy());

        var equipmentTabWidget = Substitute.For<IWidget>();
        OnComponentClick? openEquipment = null;
        equipmentTabWidget.When(widget => widget.AttachClickHandler(38, Arg.Any<OnComponentClick>()))
            .Do(call => openEquipment = call.ArgAt<OnComponentClick>(1));
        var targetContext = Substitute.For<ICharacterContext>();
        targetContext.Character.Returns(duel.Target);
        var targetContextAccessor = Substitute.For<ICharacterContextAccessor>();
        targetContextAccessor.Context.Returns(targetContext);
        var equipmentTab = new EquipmentTab(targetContextAccessor);
        equipmentTab.Initialize(equipmentTabWidget);
        equipmentTab.OnOpen();
        Assert.IsTrue(openEquipment!(38, ComponentClickType.LeftClick, 0, 0));
        duel.Target.Widgets.DidNotReceive().OpenWidget(667, 0, Arg.Any<IWidgetScript>(), true);
        duel.Target.Received(1).SendChatMessage("Please finish what you're doing before opening equipment interface.");

        familiar.WithdrawAvailableToInventory();
        Assert.AreEqual(1, familiar.Items.GetCountById(familiarItem.Id));
        Assert.AreEqual(14, duel.TargetInventory.Items.TakenSlots);
        duel.Target.Received(1).SendChatMessage("Please finish what you're doing before withdrawing items from your familiar.");

        losingCombat!.OnKilledBy(duel.Target);

        Assert.AreEqual(0, duel.SelfStakeContainer.TakenSlots);
        Assert.AreEqual(0, duel.TargetStakeContainer.TakenSlots);
        Assert.AreEqual(1, duel.TargetInventory.Items.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(1, duel.TargetInventory.Items.GetCountById(duel.TargetItem.Id));
        Assert.AreEqual(16, duel.TargetInventory.Items.TakenSlots);
        Assert.AreEqual(1, familiar.Items.GetCountById(familiarItem.Id));
        duel.Target.Widgets.Received(1).OpenWidget(1365, 0, Arg.Any<IWidgetScript>(), false);
    }

    [TestMethod]
    public void OnDestroy_CancelsPreCombatDuelAndRefundsBothStakes()
    {
        var duel = CreateDuel(6, 7);
        Assert.IsTrue(duel.SelfStake!(0, ComponentClickType.Option4Click, duel.SelfItem.Id, 0));
        Assert.IsTrue(duel.TargetStake!(0, ComponentClickType.Option4Click, duel.TargetItem.Id, 0));

        duel.SessionScript.OnDestroy();

        Assert.AreEqual(7, duel.SelfInventory.Items.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(6, duel.TargetInventory.Items.GetCountById(duel.TargetItem.Id));
        Assert.AreEqual(0, duel.SelfStakeContainer.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(0, duel.TargetStakeContainer.GetCountById(duel.TargetItem.Id));
    }

    [TestMethod]
    public void OnDetach_CancelsPreCombatDuelAndRefundsBothStakesBeforeSnapshot()
    {
        var duel = CreateDuel(6, 7);
        Assert.IsTrue(duel.SelfStake!(0, ComponentClickType.Option4Click, duel.SelfItem.Id, 0));
        Assert.IsTrue(duel.TargetStake!(0, ComponentClickType.Option4Click, duel.TargetItem.Id, 0));

        duel.SessionScript.OnDetach();

        Assert.AreEqual(7, duel.SelfInventory.Items.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(6, duel.TargetInventory.Items.GetCountById(duel.TargetItem.Id));
        Assert.AreEqual(0, duel.SelfStakeContainer.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(0, duel.TargetStakeContainer.GetCountById(duel.TargetItem.Id));
    }

    [TestMethod]
    public void SelfAndTargetStakeHandlers_UseTheirOwnInventoryAndStake()
    {
        var duel = CreateDuel(6, 7);

        duel.SentItems.Clear();
        duel.StakeValueUpdates.Clear();
        Assert.IsTrue(duel.SelfStake!(0, ComponentClickType.Option4Click, duel.SelfItem.Id, 0));
        AssertFullStakeRefresh(duel);
        AssertStakeValueUpdates(duel, 707, 0);

        duel.SentItems.Clear();
        duel.StakeValueUpdates.Clear();
        Assert.IsTrue(duel.TargetStake!(0, ComponentClickType.Option4Click, duel.TargetItem.Id, 0));
        AssertFullStakeRefresh(duel);
        AssertStakeValueUpdates(duel, 707, 600);

        Assert.IsTrue(duel.SelfAccept!(56, ComponentClickType.LeftClick, 0, 0));
        duel.Self.Configurations.ClearReceivedCalls();
        duel.Target.Configurations.ClearReceivedCalls();
        duel.SentItems.Clear();
        duel.StakeValueUpdates.Clear();
        Assert.IsTrue(duel.TargetUnstake!(7, ComponentClickType.Option4Click, duel.TargetItem.Id, 0));
        AssertFullStakeRefresh(duel);
        AssertStakeValueUpdates(duel, 707, 0);
        duel.SessionScript.RefreshDuelConfirmationStatus();
        duel.Self.Configurations.Received(2).SendBitConfiguration(12247, 0);
        duel.Target.Configurations.Received(2).SendGlobalCs2Int(9, 0);
        duel.Self.Configurations.DidNotReceive().SendBitConfiguration(12247, 1);
        duel.Target.Configurations.DidNotReceive().SendGlobalCs2Int(9, 1);

        Assert.AreEqual(0, duel.TargetStakeContainer.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(7, duel.SelfStakeContainer.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(0, duel.SelfStakeContainer.GetCountById(duel.TargetItem.Id));
        Assert.AreEqual(0, duel.TargetStakeContainer.GetCountById(duel.TargetItem.Id));
        Assert.AreEqual(0, duel.TargetStakeContainer.TakenSlots);
        Assert.AreEqual(0, duel.SelfInventory.Items.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(6, duel.TargetInventory.Items.GetCountById(duel.TargetItem.Id));
    }

    [TestMethod]
    public void RefreshDuelStakeScreen_RepeatsFullStateWithoutChangingItemsOrAcceptance()
    {
        var duel = CreateDuel(6, 7);
        Assert.IsTrue(duel.SelfStake!(0, ComponentClickType.Option4Click, duel.SelfItem.Id, 0));
        Assert.IsTrue(duel.TargetStake!(0, ComponentClickType.Option4Click, duel.TargetItem.Id, 0));
        Assert.IsTrue(duel.SelfAccept!(56, ComponentClickType.LeftClick, 0, 0));

        var selfStakedItem = duel.SelfStakeContainer[0];
        var targetStakedItem = duel.TargetStakeContainer[0];
        var selfInventoryCount = duel.SelfInventory.Items.GetCountById(duel.SelfItem.Id);
        var targetInventoryCount = duel.TargetInventory.Items.GetCountById(duel.TargetItem.Id);
        duel.Self.Configurations.ClearReceivedCalls();
        duel.Target.Configurations.ClearReceivedCalls();
        duel.SentItems.Clear();
        duel.StakeValueUpdates.Clear();

        duel.SessionScript.RefreshDuelStakeScreen();
        duel.SessionScript.RefreshDuelStakeScreen();

        Assert.AreSame(selfStakedItem, duel.SelfStakeContainer[0]);
        Assert.AreSame(targetStakedItem, duel.TargetStakeContainer[0]);
        Assert.AreEqual(7, duel.SelfStakeContainer.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(6, duel.TargetStakeContainer.GetCountById(duel.TargetItem.Id));
        Assert.AreEqual(selfInventoryCount, duel.SelfInventory.Items.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(targetInventoryCount, duel.TargetInventory.Items.GetCountById(duel.TargetItem.Id));
        Assert.AreEqual(8, duel.SentItems.Count);
        Assert.AreEqual(8, duel.StakeValueUpdates.Count);
        AssertFullStakeRefresh(duel, 0);
        AssertFullStakeRefresh(duel, 4);
        AssertStakeValueUpdates(duel, 707, 600, 0);
        AssertStakeValueUpdates(duel, 707, 600, 4);

        duel.SessionScript.RefreshDuelConfirmationStatus();
        duel.Self.Configurations.Received(1).SendBitConfiguration(12247, 1);
        duel.Target.Configurations.Received(1).SendGlobalCs2Int(9, 1);
    }

    [DataTestMethod]
    [DataRow(ComponentClickType.LeftClick, 1)]
    [DataRow(ComponentClickType.Option2Click, 5)]
    [DataRow(ComponentClickType.Option3Click, 10)]
    [DataRow(ComponentClickType.Option4Click, 20)]
    public void SelfStakeHandler_UsesExpectedPresetAmount(ComponentClickType clickType, int expected)
    {
        var duel = CreateDuel(20, 20);
        Assert.AreNotSame(duel.SelfStakeContainer, duel.TargetStakeContainer);
        Assert.AreEqual(0, duel.TargetStakeContainer.TakenSlots);
        Assert.AreEqual(20, duel.SelfInventory.Items.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(20, duel.SelfInventory.Items.GetCount(duel.SelfItem));
        Assert.IsTrue(duel.SelfItem.ItemScript.CanTradeItem(duel.SelfItem, duel.Self));

        Assert.IsTrue(duel.SelfStake!(0, clickType, duel.SelfItem.Id, 0));

        Assert.AreEqual(expected, duel.SelfStakeContainer.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(20 - expected, duel.SelfInventory.Items.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(0, duel.TargetStakeContainer.GetCountById(duel.SelfItem.Id));
    }

    [DataTestMethod]
    [DataRow(ComponentClickType.LeftClick, 1)]
    [DataRow(ComponentClickType.Option2Click, 5)]
    [DataRow(ComponentClickType.Option3Click, 10)]
    [DataRow(ComponentClickType.Option4Click, 20)]
    public void SelfUnstakeHandler_UsesExpectedPresetAmount(ComponentClickType clickType, int expected)
    {
        var duel = CreateDuel(20, 20);
        duel.SelfStake!(0, ComponentClickType.Option4Click, duel.SelfItem.Id, 0);

        Assert.IsTrue(duel.SelfUnstake!(7, clickType, duel.SelfItem.Id, 0));

        Assert.AreEqual(expected, duel.SelfInventory.Items.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(20 - expected, duel.SelfStakeContainer.GetCountById(duel.SelfItem.Id));
    }

    [TestMethod]
    public void StakeX_UsesPlayerOwnedBoundedInputAndIgnoresStaleCallback()
    {
        var duel = CreateDuel(20, 20);
        Assert.IsTrue(duel.SelfStake!(0, ComponentClickType.Option5Click, duel.SelfItem.Id, 0));
        var stale = duel.Self.Widgets.IntInputHandler!;
        Assert.IsTrue(duel.SelfStake(0, ComponentClickType.Option5Click, duel.SelfItem.Id, 0));
        var current = duel.Self.Widgets.IntInputHandler!;

        stale(4);
        Assert.AreSame(current, duel.Self.Widgets.IntInputHandler);
        Assert.AreEqual(0, duel.SelfStakeContainer.TakenSlots);
        current(50);

        Assert.AreEqual(20, duel.SelfStakeContainer.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(0, duel.SelfInventory.Items.GetCountById(duel.SelfItem.Id));
        Assert.IsNull(duel.Self.Widgets.IntInputHandler);
    }

    [TestMethod]
    public void UnstakeX_UsesCorrectPlayerAndBoundsToStakedCount()
    {
        var duel = CreateDuel(20, 20);
        duel.SelfStake!(0, ComponentClickType.Option4Click, duel.SelfItem.Id, 0);
        Assert.IsTrue(duel.SelfUnstake!(7, ComponentClickType.Option5Click, duel.SelfItem.Id, 0));

        duel.Self.Widgets.IntInputHandler!(50);

        Assert.AreEqual(20, duel.SelfInventory.Items.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(0, duel.SelfStakeContainer.GetCountById(duel.SelfItem.Id));
        Assert.IsNull(duel.Self.Widgets.IntInputHandler);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-2)]
    public void UnstakeX_NonpositiveInputDoesNotReturnItem(int amount)
    {
        var duel = CreateDuel(20, 20);
        duel.SelfStake!(0, ComponentClickType.Option4Click, duel.SelfItem.Id, 0);
        Assert.IsTrue(duel.SelfUnstake!(7, ComponentClickType.Option5Click, duel.SelfItem.Id, 0));

        duel.Self.Widgets.IntInputHandler!(amount);

        Assert.AreEqual(0, duel.SelfInventory.Items.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(20, duel.SelfStakeContainer.GetCountById(duel.SelfItem.Id));
    }

    [TestMethod]
    public void UnstakeHandler_RejectsStaleItemIdWithoutReturningStake()
    {
        var duel = CreateDuel(20, 20);
        duel.SelfStake!(0, ComponentClickType.Option4Click, duel.SelfItem.Id, 0);

        Assert.IsFalse(duel.SelfUnstake!(7, ComponentClickType.LeftClick, duel.SelfItem.Id + 1, 0));

        Assert.AreEqual(0, duel.SelfInventory.Items.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(20, duel.SelfStakeContainer.GetCountById(duel.SelfItem.Id));
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-3)]
    public void StakeX_NonpositiveInputDoesNotMutate(int amount)
    {
        var duel = CreateDuel(20, 20);
        Assert.IsTrue(duel.SelfStake!(0, ComponentClickType.Option5Click, duel.SelfItem.Id, 0));

        duel.Self.Widgets.IntInputHandler!(amount);

        Assert.AreEqual(20, duel.SelfInventory.Items.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(0, duel.SelfStakeContainer.TakenSlots);
    }

    [TestMethod]
    public void SelfMoneyPouchX_StaleInputPreservesNewHandlerAndCurrentInputStakesCoins()
    {
        var duel = CreateDuel(20, 20, selfPouchCoins: 100);
        Assert.IsTrue(duel.SelfPouch!(8, ComponentClickType.LeftClick, 0, 0));
        var staleHandler = duel.Self.Widgets.IntInputHandler!;
        Assert.IsTrue(duel.SelfPouch(8, ComponentClickType.LeftClick, 0, 0));
        var activeHandler = duel.Self.Widgets.IntInputHandler!;

        staleHandler(7);

        Assert.AreSame(activeHandler, duel.Self.Widgets.IntInputHandler);
        Assert.AreEqual(0, duel.SelfStakeContainer.GetCountById(995));
        activeHandler(40);

        Assert.AreEqual(60, duel.Self.MoneyPouch.Count);
        Assert.AreEqual(40, duel.SelfStakeContainer.GetCountById(995));
        Assert.AreEqual(0, duel.TargetStakeContainer.GetCountById(995));
        Assert.IsNull(duel.Self.Widgets.IntInputHandler);
    }

    [TestMethod]
    public void TargetMoneyPouchX_UsesTargetHandlerAndStake()
    {
        var duel = CreateDuel(20, 20, targetPouchCoins: 60);
        var selfHandler = duel.Self.Widgets.IntInputHandler;
        Assert.IsTrue(duel.TargetPouch!(8, ComponentClickType.LeftClick, 0, 0));
        var targetHandler = duel.Target.Widgets.IntInputHandler!;
        Assert.AreSame(selfHandler, duel.Self.Widgets.IntInputHandler);

        targetHandler(15);

        Assert.AreEqual(45, duel.Target.MoneyPouch.Count);
        Assert.AreEqual(15, duel.TargetStakeContainer.GetCountById(995));
        Assert.AreEqual(0, duel.SelfStakeContainer.GetCountById(995));
    }

    [TestMethod]
    public void SelfMoneyPouchX_WhenPouchIsShort_UsesSelfInventoryCoins()
    {
        var duel = CreateDuel(20, 20, selfPouchCoins: 5);
        Assert.IsTrue(duel.SelfInventory.Items.Add(CreateItem(995, 10)));
        Assert.IsTrue(duel.SelfPouch!(8, ComponentClickType.LeftClick, 0, 0));

        duel.Self.Widgets.IntInputHandler!(10);

        Assert.AreEqual(0, duel.Self.MoneyPouch.Count);
        Assert.AreEqual(5, duel.SelfInventory.Items.GetCountById(995));
        Assert.AreEqual(0, duel.TargetInventory.Items.GetCountById(995));
        Assert.AreEqual(20, duel.TargetInventory.Items.GetCountById(duel.TargetItem.Id));
        Assert.AreEqual(10, duel.SelfStakeContainer.GetCountById(995));
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void SelfMoneyPouchX_NonpositiveInputDoesNotStake(int amount)
    {
        var duel = CreateDuel(20, 20, selfPouchCoins: 100);
        Assert.IsTrue(duel.SelfPouch!(8, ComponentClickType.LeftClick, 0, 0));

        duel.Self.Widgets.IntInputHandler!(amount);

        Assert.AreEqual(100, duel.Self.MoneyPouch.Count);
        Assert.AreEqual(0, duel.SelfStakeContainer.GetCountById(995));
    }

    [TestMethod]
    public void SelfMoneyPouchX_WhenStakeIsFullKeepsCoinsInPouchAndReportsCurrentMessage()
    {
        var duel = CreateDuel(20, 20, selfPouchCoins: 100);
        for (var slot = 0; slot < 9; slot++)
        {
            Assert.IsTrue(duel.SelfStakeContainer.Add(CreateItem(300 + slot, 1)));
        }
        Assert.IsTrue(duel.SelfPouch!(8, ComponentClickType.LeftClick, 0, 0));

        duel.Self.Widgets.IntInputHandler!(5);

        Assert.AreEqual(100, duel.Self.MoneyPouch.Count);
        Assert.AreEqual(0, duel.SelfStakeContainer.GetCountById(995));
        duel.Self.Received(1).SendChatMessage("The stake is full.");
    }

    [TestMethod]
    public void StakeHandler_RejectsStaleSlotAndItemId()
    {
        var duel = CreateDuel(20, 20);

        Assert.IsFalse(duel.SelfStake!(0, ComponentClickType.LeftClick, duel.SelfItem.Id + 1, 0));
        Assert.IsFalse(duel.SelfStake(0, ComponentClickType.LeftClick, duel.SelfItem.Id, 2));

        Assert.AreEqual(20, duel.SelfInventory.Items.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(0, duel.SelfStakeContainer.TakenSlots);
    }

    [TestMethod]
    public void StakeHandler_RejectsUntradeableItemWithCurrentMessage()
    {
        var duel = CreateDuel(1, 1, selfTradeable: false);

        Assert.IsFalse(duel.SelfStake!(0, ComponentClickType.LeftClick, duel.SelfItem.Id, 0));

        Assert.AreEqual(1, duel.SelfInventory.Items.GetCountById(duel.SelfItem.Id));
        Assert.AreEqual(0, duel.SelfStakeContainer.TakenSlots);
        duel.Self.Received(1).SendChatMessage("You can't trade this item.");
    }

    [TestMethod]
    public void StakeHandler_WhenStakeIsFullDoesNotTransferAndReportsCurrentMessage()
    {
        var duel = CreateDuel(1, 1, additionalSelfItems: 9);
        for (var slot = 0; slot < 9; slot++)
        {
            var item = duel.SelfInventory.Items[slot]!;
            Assert.IsTrue(duel.SelfStake!(0, ComponentClickType.LeftClick, item.Id, slot));
        }
        var tenthItem = duel.SelfInventory.Items[9]!;

        Assert.IsFalse(duel.SelfStake!(0, ComponentClickType.LeftClick, tenthItem.Id, 9));

        Assert.AreEqual(1, duel.SelfInventory.Items.GetCountById(tenthItem.Id));
        Assert.AreEqual(9, duel.SelfStakeContainer.TakenSlots);
        duel.Self.Received(1).SendChatMessage("The stake is full.");
    }

    private static DuelHarness CreateDuel(int selfCount, int targetCount, bool selfTradeable = true,
        int additionalSelfItems = 0, int selfPouchCoins = 0, int targetPouchCoins = 0)
    {
        var selfInventory = new ComposedTestContainer(16);
        var targetInventory = new ComposedTestContainer(16);
        var selfItem = CreateItem(100, selfCount, selfTradeable, 100);
        var targetItem = CreateItem(101, targetCount, selfTradeable, 101);
        selfInventory.SetItem(0, selfItem);
        targetInventory.SetItem(0, targetItem);
        for (var index = 0; index < additionalSelfItems; index++)
        {
            targetInventory.SetItem(index + 1, CreateItem(200 + index, 1));
        }

        var selfWidgets = CreateWidgets(out var selfScreen, out var selfOverlay);
        var targetWidgets = CreateWidgets(out var targetScreen, out var targetOverlay);
        ITaskItem? selfReachTask = null;
        ITaskItem? targetReachTask = null;
        var self = CreateCharacter("self", selfInventory, selfWidgets, selfScreen, selfOverlay, out var selfAccessor,
            task => selfReachTask = task);
        var target = CreateCharacter("target", targetInventory, targetWidgets, targetScreen, targetOverlay,
            out var targetAccessor, task => targetReachTask = task);
        // The active script is backed by `target`, which becomes logical Self below.
        // Seed each owner-bound pouch for its logical role, but return it only to its owner.
        var pouchOwnedByInitialSelf = CreateMoneyPouch(self, targetPouchCoins);
        var pouchOwnedByInitialTarget = CreateMoneyPouch(target, selfPouchCoins);
        self.MoneyPouch.Returns(pouchOwnedByInitialSelf);
        target.MoneyPouch.Returns(pouchOwnedByInitialTarget);
        self.Viewport.VisibleCreatures.Returns([target]);
        target.Viewport.VisibleCreatures.Returns([self]);
        var selfScript = new DuelArenaScript(selfAccessor, CreateHintIconBuilder(), CreateItemBuilder());
        var targetScript = new DuelArenaScript(targetAccessor, CreateHintIconBuilder(), CreateItemBuilder());
        self.GetScript<DuelArenaScript>().Returns(selfScript);
        target.GetScript<DuelArenaScript>().Returns(targetScript);
        var sentItems = new List<SentItems>();
        var stakeValueUpdates = new List<StakeValueUpdate>();
        IItemContainer? selfStakeContainer = null;
        IItemContainer? targetStakeContainer = null;
        self.Configurations.When(x => x.SendItems(134, false, Arg.Any<IContainer<IItem?>>(), Arg.Any<HashSet<int>?>()))
            .Do(call =>
            {
                var container = call.ArgAt<IContainer<IItem?>>(2);
                selfStakeContainer = container as IItemContainer;
                sentItems.Add(new(self, 134, false, container, call.ArgAt<HashSet<int>?>(3)));
            });
        self.Configurations.When(x => x.SendItems(134, true, Arg.Any<IContainer<IItem?>>(), Arg.Any<HashSet<int>?>()))
            .Do(call => sentItems.Add(new(self, 134, true, call.ArgAt<IContainer<IItem?>>(2), call.ArgAt<HashSet<int>?>(3))));
        target.Configurations.When(x => x.SendItems(134, false, Arg.Any<IContainer<IItem?>>(), Arg.Any<HashSet<int>?>()))
            .Do(call =>
            {
                var container = call.ArgAt<IContainer<IItem?>>(2);
                targetStakeContainer = container as IItemContainer;
                sentItems.Add(new(target, 134, false, container, call.ArgAt<HashSet<int>?>(3)));
            });
        target.Configurations.When(x => x.SendItems(134, true, Arg.Any<IContainer<IItem?>>(), Arg.Any<HashSet<int>?>()))
            .Do(call => sentItems.Add(new(target, 134, true, call.ArgAt<IContainer<IItem?>>(2), call.ArgAt<HashSet<int>?>(3))));
        self.Configurations.When(x => x.SendGlobalCs2Int(Arg.Any<int>(), Arg.Any<int>()))
            .Do(call =>
            {
                var id = call.ArgAt<int>(0);
                if (id is 546 or 1333) stakeValueUpdates.Add(new(self, id, call.ArgAt<int>(1)));
            });
        target.Configurations.When(x => x.SendGlobalCs2Int(Arg.Any<int>(), Arg.Any<int>()))
            .Do(call =>
            {
                var id = call.ArgAt<int>(0);
                if (id is 546 or 1333) stakeValueUpdates.Add(new(target, id, call.ArgAt<int>(1)));
            });
        var selfChoice = new DuelChoiceScreenScript(selfAccessor);
        var targetChoice = new DuelChoiceScreenScript(targetAccessor);
        var selfDuelScreen = new DuelScreenScript(selfAccessor);
        var targetDuelScreen = new DuelScreenScript(targetAccessor);
        selfScreen.Script.Returns(selfDuelScreen);
        targetScreen.Script.Returns(targetDuelScreen);
        ConfigureScripts(self, selfAccessor, selfChoice, selfDuelScreen, target);
        ConfigureScripts(target, targetAccessor, targetChoice, targetDuelScreen, self);

        OnComponentClick? selfStake = null;
        OnComponentClick? targetStake = null;
        OnComponentClick? selfUnstake = null;
        OnComponentClick? targetUnstake = null;
        OnComponentClick? selfAccept = null;
        OnComponentClick? targetAccept = null;
        OnComponentClick? selfPouch = null;
        OnComponentClick? targetPouch = null;
        targetScreen.When(x => x.AttachClickHandler(56, Arg.Any<OnComponentClick>()))
            .Do(call => selfAccept = call.ArgAt<OnComponentClick>(1));
        selfScreen.When(x => x.AttachClickHandler(56, Arg.Any<OnComponentClick>()))
            .Do(call => targetAccept = call.ArgAt<OnComponentClick>(1));
        targetOverlay.When(x => x.AttachClickHandler(0, Arg.Any<OnComponentClick>()))
            .Do(call => selfStake = call.ArgAt<OnComponentClick>(1));
        selfOverlay.When(x => x.AttachClickHandler(0, Arg.Any<OnComponentClick>()))
            .Do(call => targetStake = call.ArgAt<OnComponentClick>(1));
        targetScreen.When(x => x.AttachClickHandler(7, Arg.Any<OnComponentClick>()))
            .Do(call => selfUnstake = call.ArgAt<OnComponentClick>(1));
        selfScreen.When(x => x.AttachClickHandler(7, Arg.Any<OnComponentClick>()))
            .Do(call => targetUnstake = call.ArgAt<OnComponentClick>(1));
        targetScreen.When(x => x.AttachClickHandler(8, Arg.Any<OnComponentClick>()))
            .Do(call => selfPouch = call.ArgAt<OnComponentClick>(1));
        selfScreen.When(x => x.AttachClickHandler(8, Arg.Any<OnComponentClick>()))
            .Do(call => targetPouch = call.ArgAt<OnComponentClick>(1));

        CharacterOptionClicked? selfChallenge = null;
        CharacterOptionClicked? targetChallenge = null;
        self.When(x => x.RegisterCharactersOptionHandler(CharacterClickType.Option1Click, "Challenge", 65535,
                false, Arg.Any<CharacterOptionClicked>()))
            .Do(call => selfChallenge = call.ArgAt<CharacterOptionClicked>(4));
        target.When(x => x.RegisterCharactersOptionHandler(CharacterClickType.Option1Click, "Challenge", 65535,
                false, Arg.Any<CharacterOptionClicked>()))
            .Do(call => targetChallenge = call.ArgAt<CharacterOptionClicked>(4));
        selfScript.RegisterChallengeOptionHandler();
        targetScript.RegisterChallengeOptionHandler();
        selfChallenge!(target, false);
        selfReachTask!.Tick();
        selfChoice.Callback!(true);
        targetChallenge!(self, false);
        targetReachTask!.Tick();

        return new DuelHarness(target, self, targetInventory, selfInventory, targetItem, selfItem, targetScript,
            targetStakeContainer!, selfStakeContainer!, selfStake, targetStake, selfUnstake, targetUnstake,
            selfAccept, targetAccept, selfPouch, targetPouch, sentItems, stakeValueUpdates);
    }

    private static void FillInventories(DuelHarness duel)
    {
        for (var index = 0; index < 15; index++)
        {
            Assert.IsTrue(duel.SelfInventory.Items.Add(CreateItem(300 + index, 1)));
            Assert.IsTrue(duel.TargetInventory.Items.Add(CreateItem(400 + index, 1)));
        }
    }

    private static MoneyPouchContainer CreateMoneyPouch(ICharacter character, int coins)
    {
        var pouch = new MoneyPouchContainer(character, CreateItemBuilder());
        if (coins > 0) Assert.IsTrue(pouch.Add(coins));
        return pouch;
    }

    private static ICharacter CreateCharacter(string name, ComposedTestContainer inventory, IWidgetContainer widgets,
        IWidget screen, IWidget overlay, out ICharacterContextAccessor accessor, Action<ITaskItem> captureTask)
    {
        var character = Substitute.For<ICharacter>();
        character.DisplayName.Returns(name);
        character.Name.Returns(name);
        character.PreviousDisplayName.Returns(name);
        character.Inventory.Returns(inventory);
        character.Widgets.Returns(widgets);
        character.Configurations.Returns(Substitute.For<IConfigurations>());
        character.EventManager.Returns(Substitute.For<IEventManager>());
        character.Movement.Returns(Substitute.For<IMovement>());
        character.Statistics.Returns(Substitute.For<ICharacterStatistics>());
        character.IsBusy().Returns(false);
        character.QueueTask(Arg.Do<ITaskItem>(captureTask)).Returns(Substitute.For<IRsTaskHandle>());
        widgets.GetOpenWidget(1367).Returns(screen);
        widgets.GetOpenWidget(1368).Returns(overlay);
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        return character;
    }

    private static IWidgetContainer CreateWidgets(out IWidget screen, out IWidget overlay)
    {
        var widgets = Substitute.For<IWidgetContainer>();
        screen = Substitute.For<IWidget>();
        overlay = Substitute.For<IWidget>();
        widgets.OpenWidget(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IWidgetScript>(), Arg.Any<bool>()).Returns(true);
        widgets.OpenInventoryOverlay(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IWidgetScript>()).Returns(true);
        return widgets;
    }

    private static void ConfigureScripts(ICharacter character, ICharacterContextAccessor accessor,
        DuelChoiceScreenScript choice, DuelScreenScript duelScreen, ICharacter otherCharacter)
    {
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(DuelChoiceScreenScript)).Returns(choice);
        provider.GetService(typeof(DuelScreenScript)).Returns(duelScreen);
        provider.GetService(typeof(ICharacterContextAccessor)).Returns(accessor);
        provider.GetService(typeof(DuelConfirmScreenScript)).Returns(new DuelConfirmScreenScript(accessor));
        provider.GetService(typeof(DefaultWidgetScript)).Returns(new DefaultWidgetScript(accessor));
        var entityService = Substitute.For<IEntityService>();
        var pathFinder = Substitute.For<ISmartPathFinder>();
        var path = Substitute.For<IPath>();
        path.Successful.Returns(true);
        path.ReachedDestination.Returns(true);
        pathFinder.Find(Arg.Any<IEntity>(), Arg.Any<IEntity>(), true).Returns(path);
        var pathFinderProvider = Substitute.For<IPathFinderProvider>();
        pathFinderProvider.Smart.Returns(pathFinder);
        provider.GetService(typeof(IEntityService)).Returns(entityService);
        provider.GetService(typeof(IPathFinderProvider)).Returns(pathFinderProvider);
        entityService.TryResolve(Arg.Any<EntityHandle<ICreature>>(), out Arg.Any<ICreature>())
            .Returns(call =>
            {
                call[1] = otherCharacter;
                return true;
            });
        character.ServiceProvider.Returns(provider);
    }

    private static IItem CreateItem(int id, int count, bool tradeable = true, int tradeValue = 0)
    {
        var item = Substitute.For<IItem>();
        item.Id.Returns(id);
        item.Count.Returns(count);
        item.Name.Returns($"Item {id}");
        var definition = Substitute.For<IItemDefinition>();
        definition.Stackable.Returns(true);
        definition.TradeValue.Returns(tradeValue);
        item.ItemDefinition.Returns(definition);
        var itemScript = Substitute.For<IItemScript>();
        itemScript.CanTradeItem(Arg.Any<IItem>(), Arg.Any<ICharacter>()).Returns(tradeable);
        itemScript.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(true);
        item.ItemScript.Returns(itemScript);
        item.Clone().Returns(_ => CreateItem(id, count, tradeable, tradeValue));
        item.Clone(Arg.Any<int>()).Returns(call => CreateItem(id, call.ArgAt<int>(0), tradeable, tradeValue));
        item.Equals(Arg.Any<IItem>(), Arg.Any<bool>()).Returns(call =>
            call.ArgAt<IItem>(0)?.Id == id);
        return item;
    }

    private static IItemBuilder CreateItemBuilder() => new TestItemBuilder((id, count) => CreateItem(id, count));

    private static IHintIconBuilder CreateHintIconBuilder()
    {
        var builder = Substitute.For<IHintIconBuilder>();
        var hintIconType = Substitute.For<IHintIconType>();
        var hintIconEntity = Substitute.For<IHintIconEntityOptional>();
        builder.Create().Returns(hintIconType);
        hintIconType.AtEntity(Arg.Any<IEntity>()).Returns(hintIconEntity);
        hintIconEntity.Build().Returns(Substitute.For<IHintIcon>());
        return builder;
    }

    private static void AssertFullStakeRefresh(DuelHarness duel, int offset = 0)
    {
        AssertSentItems(duel.SentItems[offset], duel.Self, false, duel.SelfStakeContainer);
        AssertSentItems(duel.SentItems[offset + 1], duel.Self, true, duel.TargetStakeContainer);
        AssertSentItems(duel.SentItems[offset + 2], duel.Target, false, duel.TargetStakeContainer);
        AssertSentItems(duel.SentItems[offset + 3], duel.Target, true, duel.SelfStakeContainer);
    }

    private static void AssertSentItems(SentItems sentItems, ICharacter recipient, bool split, IItemContainer container)
    {
        Assert.AreSame(recipient, sentItems.Recipient);
        Assert.AreEqual(134, sentItems.ContainerId);
        Assert.AreEqual(split, sentItems.Split);
        Assert.AreSame(container, sentItems.Container);
        Assert.IsNull(sentItems.ChangedSlots);
    }

    private static void AssertStakeValueUpdates(DuelHarness duel, int selfTotal, int targetTotal, int offset = 0)
    {
        Assert.IsTrue(duel.StakeValueUpdates.Count >= offset + 4);
        AssertStakeValue(duel.StakeValueUpdates[offset], duel.Self, 546, selfTotal);
        AssertStakeValue(duel.StakeValueUpdates[offset + 1], duel.Self, 1333, targetTotal);
        AssertStakeValue(duel.StakeValueUpdates[offset + 2], duel.Target, 546, targetTotal);
        AssertStakeValue(duel.StakeValueUpdates[offset + 3], duel.Target, 1333, selfTotal);
    }

    private static void AssertStakeValue(StakeValueUpdate update, ICharacter recipient, int id, int value)
    {
        Assert.AreSame(recipient, update.Recipient);
        Assert.AreEqual(id, update.Id);
        Assert.AreEqual(value, update.Value);
    }

    private sealed record DuelHarness(ICharacter Self, ICharacter Target, ComposedTestContainer SelfInventory,
        ComposedTestContainer TargetInventory, IItem SelfItem, IItem TargetItem, DuelArenaScript SessionScript,
        IItemContainer SelfStakeContainer, IItemContainer TargetStakeContainer,
        OnComponentClick? SelfStake, OnComponentClick? TargetStake, OnComponentClick? SelfUnstake,
        OnComponentClick? TargetUnstake, OnComponentClick? SelfAccept, OnComponentClick? TargetAccept,
        OnComponentClick? SelfPouch, OnComponentClick? TargetPouch,
        List<SentItems> SentItems, List<StakeValueUpdate> StakeValueUpdates);

    private sealed record SentItems(ICharacter Recipient, int ContainerId, bool Split,
        IContainer<IItem?> Container, HashSet<int>? ChangedSlots);

    private sealed record StakeValueUpdate(ICharacter Recipient, int Id, int Value);
}
