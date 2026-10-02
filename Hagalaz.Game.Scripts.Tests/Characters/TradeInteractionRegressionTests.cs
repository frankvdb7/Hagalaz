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

    private static TradeSession CreateSession(int selfCount, int targetCount)
    {
        var selfInventory = new ComposedTestContainer(4);
        var targetInventory = new ComposedTestContainer(4);
        var selfItem = CreateTradeableItem(100, selfCount);
        var targetItem = CreateTradeableItem(101, targetCount);
        selfInventory.SetItem(0, selfItem);
        targetInventory.SetItem(0, targetItem);
        var selfWidgets = CreateWidgets(out var selfInterface, out var selfOverlay);
        var targetWidgets = CreateWidgets(out var targetInterface, out var targetOverlay);
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
        ConfigureScripts(self, selfAccessor);
        ConfigureScripts(target, targetAccessor);
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
        selfScript.StartTradeSession(target);
        return new TradeSession(selfInventory, targetInventory, selfItem, targetItem, self, target, selfScript,
            targetScript, selfOffer, targetOffer, selfOfferedItems, targetOfferedItems, selfPouch, targetPouch);
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
        OnComponentClick? TargetPouch);

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

    private static IWidgetContainer CreateWidgets(out IWidget tradeInterface, out IWidget inventoryOverlay)
    {
        var widgets = Substitute.For<IWidgetContainer>();
        tradeInterface = Substitute.For<IWidget>();
        inventoryOverlay = Substitute.For<IWidget>();
        widgets.OpenWidget(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IWidgetScript>(), Arg.Any<bool>()).Returns(true);
        widgets.OpenInventoryOverlay(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IWidgetScript>()).Returns(true);
        widgets.GetOpenWidget(335).Returns(tradeInterface);
        widgets.GetOpenWidget(336).Returns(inventoryOverlay);
        return widgets;
    }

    private static void ConfigureScripts(ICharacter character, ICharacterContextAccessor accessor)
    {
        var tradeInterface = new TradingCharacterScript.TradeInterfaceScript(accessor);
        var defaultWidget = new DefaultWidgetScript(accessor);
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(TradingCharacterScript.TradeInterfaceScript)).Returns(tradeInterface);
        provider.GetService(typeof(DefaultWidgetScript)).Returns(defaultWidget);
        character.ServiceProvider.Returns(provider);
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
