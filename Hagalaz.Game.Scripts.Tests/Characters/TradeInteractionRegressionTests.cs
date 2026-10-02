using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Characters;
using Hagalaz.Game.Scripts.Model.Widgets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Characters;

[TestClass]
public sealed class TradeInteractionRegressionTests
{
    [TestMethod]
    public void SelfAndTargetOfferHandlers_MoveItemsFromTheirOwnInventories()
    {
        var selfInventory = new ComposedTestContainer(4);
        var targetInventory = new ComposedTestContainer(4);
        var selfItem = CreateTradeableItem(100, 20);
        var targetItem = CreateTradeableItem(101, 20);
        selfInventory.SetItem(0, selfItem);
        targetInventory.SetItem(0, targetItem);
        var selfWidgets = CreateWidgets(out var selfInterface, out var selfOverlay);
        var targetWidgets = CreateWidgets(out var targetInterface, out var targetOverlay);
        var self = CreateCharacter("self", selfInventory, selfWidgets, out var selfAccessor);
        var target = CreateCharacter("target", targetInventory, targetWidgets, out var targetAccessor);
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
        selfOverlay.When(x => x.AttachClickHandler(0, Arg.Any<OnComponentClick>()))
            .Do(call => selfOffer = call.ArgAt<OnComponentClick>(1));
        targetOverlay.When(x => x.AttachClickHandler(0, Arg.Any<OnComponentClick>()))
            .Do(call => targetOffer = call.ArgAt<OnComponentClick>(1));
        selfInterface.When(x => x.AttachClickHandler(32, Arg.Any<OnComponentClick>()))
            .Do(call => selfOfferedItems = call.ArgAt<OnComponentClick>(1));
        targetInterface.When(x => x.AttachClickHandler(32, Arg.Any<OnComponentClick>()))
            .Do(call => targetOfferedItems = call.ArgAt<OnComponentClick>(1));

        selfScript.StartTradeSession(target);

        Assert.IsTrue(selfOffer!(0, ComponentClickType.Option2Click, selfItem.Id, 0));
        Assert.IsTrue(targetOffer!(0, ComponentClickType.Option3Click, targetItem.Id, 0));
        Assert.IsTrue(selfOfferedItems!(32, ComponentClickType.LeftClick, selfItem.Id, 0));
        Assert.IsTrue(targetOfferedItems!(32, ComponentClickType.Option2Click, targetItem.Id, 0));

        Assert.AreEqual(16, selfInventory.Items.GetCountById(selfItem.Id));
        Assert.AreEqual(4, selfScript.SelfContainer.Items.GetCountById(selfItem.Id));
        Assert.AreEqual(15, targetInventory.Items.GetCountById(targetItem.Id));
        Assert.AreEqual(5, selfScript.TargetContainer.Items.GetCountById(targetItem.Id));
        Assert.AreEqual(0, targetScript.SelfContainer.Items.TakenSlots);
    }

    private static ICharacter CreateCharacter(string name, ComposedTestContainer inventory, IWidgetContainer widgets,
        out ICharacterContextAccessor accessor)
    {
        var character = Substitute.For<ICharacter>();
        character.DisplayName.Returns(name);
        character.Inventory.Returns(inventory);
        character.Widgets.Returns(widgets);
        character.Configurations.Returns(Substitute.For<IConfigurations>());
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

    private static IItemBuilder CreateItemBuilder() => new TestItemBuilder(ComposedTestContainer.CreateTestItem);

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
