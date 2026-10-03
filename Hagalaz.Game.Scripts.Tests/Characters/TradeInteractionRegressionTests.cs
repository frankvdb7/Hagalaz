using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Characters;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Characters;

[TestClass]
public sealed class TradeInteractionRegressionTests
{
    [TestMethod]
    public void SelfAndTargetOfferHandlers_MoveItemsFromTheirOwnInventories()
    {
        var session = CreateSession(20, 20);

        Assert.IsTrue(session.SelfOffer!(0, ComponentClickType.Option2Click, session.SelfItem.Id, 0));
        Assert.IsTrue(session.TargetOffer!(0, ComponentClickType.Option3Click, session.TargetItem.Id, 0));
        Assert.IsTrue(session.SelfOfferedItems!(32, ComponentClickType.LeftClick, session.SelfItem.Id, 0));
        Assert.IsTrue(session.TargetOfferedItems!(32, ComponentClickType.Option2Click, session.TargetItem.Id, 0));

        Assert.AreEqual(16, session.SelfInventory.Items.GetCountById(session.SelfItem.Id));
        Assert.AreEqual(4, session.SelfScript.SelfContainer.Items.GetCountById(session.SelfItem.Id));
        Assert.AreEqual(15, session.TargetInventory.Items.GetCountById(session.TargetItem.Id));
        Assert.AreEqual(5, session.SelfScript.TargetContainer.Items.GetCountById(session.TargetItem.Id));
        Assert.AreEqual(0, session.TargetScript.SelfContainer.Items.TakenSlots);
    }

    [DataTestMethod]
    [DataRow(ComponentClickType.LeftClick, 1)]
    [DataRow(ComponentClickType.Option2Click, 5)]
    [DataRow(ComponentClickType.Option3Click, 10)]
    [DataRow(ComponentClickType.Option4Click, 20)]
    public void OfferHandler_UsesExpectedPresetAmount(ComponentClickType clickType, int expected)
    {
        var session = CreateSession(20, 20);

        Assert.IsTrue(session.SelfOffer!(0, clickType, session.SelfItem.Id, 0));

        Assert.AreEqual(expected, session.SelfScript.SelfContainer.Items.GetCountById(session.SelfItem.Id));
        Assert.AreEqual(20 - expected, session.SelfInventory.Items.GetCountById(session.SelfItem.Id));
        Assert.AreEqual(0, session.TargetScript.SelfContainer.Items.GetCountById(session.SelfItem.Id));
    }

    [DataTestMethod]
    [DataRow(ComponentClickType.LeftClick, 1)]
    [DataRow(ComponentClickType.Option2Click, 5)]
    [DataRow(ComponentClickType.Option3Click, 10)]
    [DataRow(ComponentClickType.Option4Click, 20)]
    public void RemoveOfferHandler_UsesExpectedPresetAmount(ComponentClickType clickType, int expected)
    {
        var session = CreateSession(20, 20);
        session.SelfOffer!(0, ComponentClickType.Option4Click, session.SelfItem.Id, 0);

        Assert.IsTrue(session.SelfOfferedItems!(32, clickType, session.SelfItem.Id, 0));

        Assert.AreEqual(20 - expected, session.SelfScript.SelfContainer.Items.GetCountById(session.SelfItem.Id));
        Assert.AreEqual(expected, session.SelfInventory.Items.GetCountById(session.SelfItem.Id));
        Assert.AreEqual(0, session.TargetInventory.Items.GetCountById(session.SelfItem.Id));
    }

    [TestMethod]
    public void OfferX_UsesOwnerBoundedInputAndRejectsStaleCallback()
    {
        var session = CreateSession(20, 20);
        Assert.IsTrue(session.TargetOffer!(0, ComponentClickType.Option5Click, session.TargetItem.Id, 0));
        var staleHandler = session.Target.Widgets.IntInputHandler!;
        Assert.IsTrue(session.TargetOffer(0, ComponentClickType.Option5Click, session.TargetItem.Id, 0));
        var activeHandler = session.Target.Widgets.IntInputHandler!;

        staleHandler(7);
        Assert.AreEqual(0, session.SelfScript.TargetContainer.Items.TakenSlots);
        Assert.AreSame(activeHandler, session.Target.Widgets.IntInputHandler);

        activeHandler(50);
        Assert.AreEqual(20, session.SelfScript.TargetContainer.Items.GetCountById(session.TargetItem.Id));
        Assert.AreEqual(0, session.TargetInventory.Items.GetCountById(session.TargetItem.Id));
        Assert.IsNull(session.Target.Widgets.IntInputHandler);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-4)]
    public void OfferX_NonpositiveInputDoesNotTransfer(int amount)
    {
        var session = CreateSession(20, 20);
        Assert.IsTrue(session.SelfOffer!(0, ComponentClickType.Option5Click, session.SelfItem.Id, 0));

        session.Self.Widgets.IntInputHandler!(amount);

        Assert.AreEqual(20, session.SelfInventory.Items.GetCountById(session.SelfItem.Id));
        Assert.AreEqual(0, session.SelfScript.SelfContainer.Items.TakenSlots);
    }

    [TestMethod]
    public void RemoveOfferX_UsesCorrectCharacterAndBoundsAmount()
    {
        var session = CreateSession(20, 20);
        session.SelfOffer!(0, ComponentClickType.Option4Click, session.SelfItem.Id, 0);
        Assert.IsTrue(session.SelfOfferedItems!(32, ComponentClickType.Option5Click, session.SelfItem.Id, 0));

        session.Self.Widgets.IntInputHandler!(50);

        Assert.AreEqual(20, session.SelfInventory.Items.GetCountById(session.SelfItem.Id));
        Assert.AreEqual(0, session.SelfScript.SelfContainer.Items.GetCountById(session.SelfItem.Id));
        Assert.IsNull(session.Self.Widgets.IntInputHandler);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-2)]
    public void RemoveOfferX_NonpositiveInputDoesNotTransfer(int amount)
    {
        var session = CreateSession(20, 20);
        session.SelfOffer!(0, ComponentClickType.Option4Click, session.SelfItem.Id, 0);
        Assert.IsTrue(session.SelfOfferedItems!(32, ComponentClickType.Option5Click, session.SelfItem.Id, 0));

        session.Self.Widgets.IntInputHandler!(amount);

        Assert.AreEqual(0, session.SelfInventory.Items.GetCountById(session.SelfItem.Id));
        Assert.AreEqual(20, session.SelfScript.SelfContainer.Items.GetCountById(session.SelfItem.Id));
        Assert.IsNull(session.Self.Widgets.IntInputHandler);
    }

    [TestMethod]
    public void SelfMoneyPouchX_StaleCallbackPreservesNewHandlerAndCurrentCallbackOffersCoins()
    {
        var session = CreateSession(20, 20);
        Assert.IsTrue(session.SelfPouch!(53, ComponentClickType.LeftClick, 0, 0));
        var staleHandler = session.Self.Widgets.IntInputHandler!;
        Assert.IsTrue(session.SelfPouch(53, ComponentClickType.LeftClick, 0, 0));
        var activeHandler = session.Self.Widgets.IntInputHandler!;

        staleHandler(7);

        Assert.AreSame(activeHandler, session.Self.Widgets.IntInputHandler);
        Assert.AreEqual(0, session.SelfScript.SelfContainer.Items.GetCountById(995));
        activeHandler(25);

        Assert.AreEqual(75, session.Self.MoneyPouch.Count);
        Assert.AreEqual(25, session.SelfScript.SelfContainer.Items.GetCountById(995));
        session.Self.Configurations.Received(2).SendIntegerInput(
            "Your money pouch currently contains 100 coins.<br>How many would you like to offer?");
        Assert.IsNull(session.Self.Widgets.IntInputHandler);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void SelfMoneyPouchX_NonpositiveAmountDoesNotOfferCoins(int amount)
    {
        var session = CreateSession(20, 20);
        Assert.IsTrue(session.SelfPouch!(53, ComponentClickType.LeftClick, 0, 0));

        session.Self.Widgets.IntInputHandler!(amount);

        Assert.AreEqual(100, session.Self.MoneyPouch.Count);
        Assert.AreEqual(0, session.SelfScript.SelfContainer.Items.GetCountById(995));
    }

    [TestMethod]
    public void TargetMoneyPouchX_UsesTargetHandlerAndPouch()
    {
        var session = CreateSession(20, 20);
        Assert.IsTrue(session.TargetPouch!(53, ComponentClickType.LeftClick, 0, 0));
        var targetHandler = session.Target.Widgets.IntInputHandler!;

        targetHandler(20);

        Assert.AreEqual(80, session.Target.MoneyPouch.Count);
        Assert.AreEqual(20, session.SelfScript.TargetContainer.Items.GetCountById(995));
        Assert.AreEqual(100, session.Self.MoneyPouch.Count);
        Assert.IsNull(session.Target.Widgets.IntInputHandler);
    }

    [TestMethod]
    public void OfferHandler_AfterTradeSessionIsCancelledDoesNotMutate()
    {
        var session = CreateSession(20, 20);
        session.SelfScript.CancelTradeSession();

        Assert.IsFalse(session.SelfOffer!(0, ComponentClickType.Option4Click, session.SelfItem.Id, 0));
        Assert.AreEqual(20, session.SelfInventory.Items.GetCountById(session.SelfItem.Id));
    }

    [TestMethod]
    public async Task FinalAcceptHandlers_ConcurrentCallbacksExchangeOnceAndStaleCallbackCannotAffectNextTrade()
    {
        var session = CreateSession(1, 1);
        Assert.IsTrue(session.SelfOffer!(0, ComponentClickType.Option4Click, session.SelfItem.Id, 0));
        Assert.IsTrue(session.TargetOffer!(0, ComponentClickType.Option4Click, session.TargetItem.Id, 0));

        Assert.IsTrue(session.Handlers.SelfOfferAccept!(18, ComponentClickType.LeftClick, 0, 0));
        Assert.IsTrue(session.Handlers.TargetOfferAccept!(18, ComponentClickType.LeftClick, 0, 0));

        var selfFinalAccept = session.Handlers.SelfFinalAccept!;
        var targetFinalAccept = session.Handlers.TargetFinalAccept!;
        using var start = new Barrier(3);
        var selfAccept = Task.Run(() =>
        {
            start.SignalAndWait();
            selfFinalAccept(21, ComponentClickType.LeftClick, 0, 0);
        });
        var targetAccept = Task.Run(() =>
        {
            start.SignalAndWait();
            targetFinalAccept(21, ComponentClickType.LeftClick, 0, 0);
        });

        start.SignalAndWait();
        await Task.WhenAll(selfAccept, targetAccept);

        Assert.AreEqual(1, session.SelfInventory.Items.GetCountById(session.TargetItem.Id));
        Assert.AreEqual(1, session.TargetInventory.Items.GetCountById(session.SelfItem.Id));
        Assert.IsFalse(session.SelfScript.TradeSession);

        selfFinalAccept(21, ComponentClickType.LeftClick, 0, 0);
        targetFinalAccept(21, ComponentClickType.LeftClick, 0, 0);
        Assert.AreEqual(1, session.SelfInventory.Items.GetCountById(session.TargetItem.Id));
        Assert.AreEqual(1, session.TargetInventory.Items.GetCountById(session.SelfItem.Id));

        session.SelfScript.StartTradeSession(session.Target);
        selfFinalAccept(21, ComponentClickType.LeftClick, 0, 0);
        Assert.IsTrue(session.SelfScript.TradeSession);
        Assert.IsFalse(session.SelfScript.SelfAccepted);
        Assert.IsFalse(session.SelfScript.TargetAccepted);
        Assert.AreEqual(0, session.SelfScript.SelfContainer.Items.TakenSlots);
        Assert.AreEqual(0, session.SelfScript.TargetContainer.Items.TakenSlots);
    }

    [TestMethod]
    public void FinalAccept_WhenExchangeAndRefundCannotFit_ResetsConfirmationUiAndPreservesEscrow()
    {
        var session = CreateSession(1, 1);
        Assert.IsTrue(session.SelfOffer!(0, ComponentClickType.Option4Click, session.SelfItem.Id, 0));
        Assert.IsTrue(session.TargetOffer!(0, ComponentClickType.Option4Click, session.TargetItem.Id, 0));
        for (var itemId = 200; itemId < 204; itemId++)
        {
            Assert.IsTrue(session.SelfInventory.Items.Add(ComposedTestContainer.CreateTestItem(itemId)));
            Assert.IsTrue(session.TargetInventory.Items.Add(ComposedTestContainer.CreateTestItem(itemId + 10)));
        }

        Assert.IsTrue(session.Handlers.SelfOfferAccept!(18, ComponentClickType.LeftClick, 0, 0));
        Assert.IsTrue(session.Handlers.TargetOfferAccept!(18, ComponentClickType.LeftClick, 0, 0));
        Assert.IsTrue(session.Handlers.SelfFinalAccept!(21, ComponentClickType.LeftClick, 0, 0));
        Assert.IsTrue(session.Handlers.TargetFinalAccept!(21, ComponentClickType.LeftClick, 0, 0));

        Assert.IsTrue(session.SelfScript.TradeSession);
        Assert.IsFalse(session.SelfScript.SelfAccepted);
        Assert.IsFalse(session.SelfScript.TargetAccepted);
        Assert.AreEqual(1, session.SelfScript.SelfContainer.Items.GetCountById(session.SelfItem.Id));
        Assert.AreEqual(1, session.SelfScript.TargetContainer.Items.GetCountById(session.TargetItem.Id));
        session.Self.Widgets.GetOpenWidget(334)!.Received().DrawString(34, "Are you sure you want to make this trade?");
        session.Target.Widgets.GetOpenWidget(334)!.Received().DrawString(34, "Are you sure you want to make this trade?");
    }

    private static TradeSession CreateSession(int selfCount, int targetCount)
    {
        var selfInventory = new ComposedTestContainer(4);
        var targetInventory = new ComposedTestContainer(4);
        var selfItem = CreateTradeableItem(100, selfCount);
        var targetItem = CreateTradeableItem(101, targetCount);
        selfInventory.SetItem(0, selfItem);
        targetInventory.SetItem(0, targetItem);
        var selfWidgets = CreateWidgets(out var selfInterface, out var selfOverlay, out var selfConfirmationInterface);
        var targetWidgets = CreateWidgets(out var targetInterface, out var targetOverlay, out var targetConfirmationInterface);
        var self = CreateCharacter("self", selfInventory, selfWidgets, out var selfAccessor);
        var target = CreateCharacter("target", targetInventory, targetWidgets, out var targetAccessor);
        var selfMoneyPouch = CreateMoneyPouch(self);
        var targetMoneyPouch = CreateMoneyPouch(target);
        self.MoneyPouch.Returns(selfMoneyPouch);
        target.MoneyPouch.Returns(targetMoneyPouch);
        var selfScript = new TradingCharacterScript(selfAccessor, CreateItemBuilder());
        var targetScript = new TradingCharacterScript(targetAccessor, CreateItemBuilder());
        self.GetScript<TradingCharacterScript>().Returns(selfScript);
        target.GetScript<TradingCharacterScript>().Returns(targetScript);
        ConfigureScripts(self, selfAccessor, selfInterface, selfConfirmationInterface);
        ConfigureScripts(target, targetAccessor, targetInterface, targetConfirmationInterface);
        var handlers = new TradeClickHandlers();
        OnComponentClick? selfOffer = null;
        OnComponentClick? targetOffer = null;
        OnComponentClick? selfOfferedItems = null;
        OnComponentClick? targetOfferedItems = null;
        OnComponentClick? selfPouch = null;
        OnComponentClick? targetPouch = null;
        selfOverlay.When(x => x.AttachClickHandler(0, Arg.Any<OnComponentClick>()))
            .Do(call => selfOffer = call.ArgAt<OnComponentClick>(1));
        targetOverlay.When(x => x.AttachClickHandler(0, Arg.Any<OnComponentClick>()))
            .Do(call => targetOffer = call.ArgAt<OnComponentClick>(1));
        selfInterface.When(x => x.AttachClickHandler(32, Arg.Any<OnComponentClick>()))
            .Do(call => selfOfferedItems = call.ArgAt<OnComponentClick>(1));
        targetInterface.When(x => x.AttachClickHandler(32, Arg.Any<OnComponentClick>()))
            .Do(call => targetOfferedItems = call.ArgAt<OnComponentClick>(1));
        selfInterface.When(x => x.AttachClickHandler(53, Arg.Any<OnComponentClick>()))
            .Do(call => selfPouch = call.ArgAt<OnComponentClick>(1));
        targetInterface.When(x => x.AttachClickHandler(53, Arg.Any<OnComponentClick>()))
            .Do(call => targetPouch = call.ArgAt<OnComponentClick>(1));
        selfInterface.When(x => x.AttachClickHandler(18, Arg.Any<OnComponentClick>()))
            .Do(call => handlers.SelfOfferAccept = call.ArgAt<OnComponentClick>(1));
        targetInterface.When(x => x.AttachClickHandler(18, Arg.Any<OnComponentClick>()))
            .Do(call => handlers.TargetOfferAccept = call.ArgAt<OnComponentClick>(1));
        selfConfirmationInterface.When(x => x.AttachClickHandler(21, Arg.Any<OnComponentClick>()))
            .Do(call => handlers.SelfFinalAccept = call.ArgAt<OnComponentClick>(1));
        targetConfirmationInterface.When(x => x.AttachClickHandler(21, Arg.Any<OnComponentClick>()))
            .Do(call => handlers.TargetFinalAccept = call.ArgAt<OnComponentClick>(1));
        selfScript.StartTradeSession(target);
        return new TradeSession(selfInventory, targetInventory, selfItem, targetItem, self, target, selfScript,
            targetScript, selfOffer, targetOffer, selfOfferedItems, targetOfferedItems, selfPouch, targetPouch, handlers);
    }

    private static MoneyPouchContainer CreateMoneyPouch(ICharacter character)
    {
        var pouch = new MoneyPouchContainer(character, CreateItemBuilder());
        Assert.IsTrue(pouch.Add(100));
        return pouch;
    }

    private sealed record TradeSession(ComposedTestContainer SelfInventory, ComposedTestContainer TargetInventory,
        IItem SelfItem, IItem TargetItem, ICharacter Self, ICharacter Target, TradingCharacterScript SelfScript,
        TradingCharacterScript TargetScript, OnComponentClick? SelfOffer, OnComponentClick? TargetOffer,
        OnComponentClick? SelfOfferedItems, OnComponentClick? TargetOfferedItems, OnComponentClick? SelfPouch,
        OnComponentClick? TargetPouch, TradeClickHandlers Handlers);

    private sealed class TradeClickHandlers
    {
        public OnComponentClick? SelfOfferAccept { get; set; }
        public OnComponentClick? TargetOfferAccept { get; set; }
        public OnComponentClick? SelfFinalAccept { get; set; }
        public OnComponentClick? TargetFinalAccept { get; set; }
    }

    private static ICharacter CreateCharacter(string name, ComposedTestContainer inventory, IWidgetContainer widgets,
        out ICharacterContextAccessor accessor)
    {
        var character = Substitute.For<ICharacter>();
        character.DisplayName.Returns(name);
        character.Inventory.Returns(inventory);
        character.Widgets.Returns(widgets);
        character.Configurations.Returns(Substitute.For<IConfigurations>());
        character.EventManager.Returns(Substitute.For<IEventManager>());
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        return character;
    }

    private static IWidgetContainer CreateWidgets(out IWidget tradeInterface, out IWidget inventoryOverlay,
        out IWidget confirmationInterface)
    {
        var widgets = Substitute.For<IWidgetContainer>();
        var offerWidget = Substitute.For<IWidget>();
        var overlayWidget = Substitute.For<IWidget>();
        var confirmWidget = Substitute.For<IWidget>();
        tradeInterface = offerWidget;
        inventoryOverlay = overlayWidget;
        confirmationInterface = confirmWidget;
        offerWidget.Id.Returns(335);
        overlayWidget.Id.Returns(336);
        confirmWidget.Id.Returns(334);
        widgets.OpenWidget(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IWidgetScript>(), Arg.Any<bool>()).Returns(true);
        widgets.OpenInventoryOverlay(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IWidgetScript>()).Returns(true);
        widgets.GetOpenWidget(Arg.Any<int>()).Returns(call => call.ArgAt<int>(0) switch
        {
            335 => offerWidget,
            336 => overlayWidget,
            334 => confirmWidget,
            _ => null
        });
        return widgets;
    }

    private static void ConfigureScripts(ICharacter character, ICharacterContextAccessor accessor,
        IWidget offerWidget, IWidget confirmationWidget)
    {
        var tradeScript = new TradingCharacterScript.TradeInterfaceScript(accessor);
        var defaultWidget = new DefaultWidgetScript(accessor);
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(TradingCharacterScript.TradeInterfaceScript)).Returns(tradeScript);
        provider.GetService(typeof(DefaultWidgetScript)).Returns(defaultWidget);
        character.ServiceProvider.Returns(provider);
        offerWidget.Script.Returns(tradeScript);
        confirmationWidget.Script.Returns(tradeScript);
    }

    private static IItem CreateTradeableItem(int id, int count)
        => new TradeableItem(id, count);

    private static IItemBuilder CreateItemBuilder() => new TestItemBuilder((id, count) => CreateTradeableItem(id, count));

    private sealed class TradeableItem(int id, int count) : IItem
    {
        public int Id { get; } = id;
        public long[] ExtraData => [];
        public int Count { get; set; } = count;
        public string Name => $"Item {Id}";
        public IItemDefinition ItemDefinition { get; } = CreateDefinition();
        public IEquipmentDefinition EquipmentDefinition { get; } = Substitute.For<IEquipmentDefinition>();
        public IItemScript ItemScript { get; } = CreateScript();
        public IEquipmentScript EquipmentScript { get; } = Substitute.For<IEquipmentScript>();
        public IItem Clone() => new TradeableItem(Id, Count);
        public IItem Clone(int newCount) => new TradeableItem(Id, newCount);
        public bool Equals(IItem otherItem, bool ignoreCount = true) => otherItem is not null && Id == otherItem.Id &&
            (ignoreCount || Count == otherItem.Count) && ExtraData.SequenceEqual(otherItem.ExtraData);
        public string? SerializeExtraData() => null;

        private static IItemDefinition CreateDefinition()
        {
            var definition = Substitute.For<IItemDefinition>();
            definition.Stackable.Returns(true);
            return definition;
        }

        private static IItemScript CreateScript()
        {
            var script = Substitute.For<IItemScript>();
            script.CanTradeItem(Arg.Any<IItem>(), Arg.Any<ICharacter>()).Returns(true);
            script.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(true);
            return script;
        }
    }
}
