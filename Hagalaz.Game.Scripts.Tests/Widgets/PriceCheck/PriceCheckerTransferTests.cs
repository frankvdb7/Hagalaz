using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Widgets.PriceCheck;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets.PriceCheck;

[TestClass]
public sealed class PriceCheckerTransferTests
{
    [TestMethod]
    public void Insert_ClonesSelectionAndProjectsRemainingCountWithoutChangingInventory()
    {
        var harness = CreateHarness();
        var inventoryItem = CreateItem(100, 100);
        Assert.IsTrue(harness.Inventory.Add(inventoryItem));

        Assert.IsTrue(AddSelection(harness, inventoryItem, 80));

        Assert.AreEqual(100, harness.Inventory.GetCount(inventoryItem));
        Assert.AreEqual(100, inventoryItem.Count);
        Assert.AreEqual(80, harness.Selections.GetCount(inventoryItem));
        Assert.AreNotSame(inventoryItem, harness.Selections.GetById(inventoryItem.Id));
        Assert.AreEqual(20, GetCount(harness.ProjectedInventory!, inventoryItem));
        Assert.AreNotSame(inventoryItem, harness.ProjectedInventory![0]);
    }

    [TestMethod]
    public void InsertAll_AfterExistingSelectionAddsOnlyAvailableQuantity()
    {
        var harness = CreateHarness();
        var item = CreateItem(100, 100);
        Assert.IsTrue(harness.Inventory.Add(item));
        Assert.IsTrue(AddSelection(harness, item, 80));

        Assert.IsTrue(AddSelection(harness, item, int.MaxValue));

        Assert.AreEqual(100, harness.Inventory.GetCount(item));
        Assert.AreEqual(100, harness.Selections.GetCount(item));
        Assert.AreEqual(0, GetCount(harness.ProjectedInventory!, item));
    }

    [TestMethod]
    public void Remove_ChangesOnlySelectionAndRestoresProjectedQuantity()
    {
        var harness = CreateHarness();
        var item = CreateItem(100, 100);
        Assert.IsTrue(harness.Inventory.Add(item));
        Assert.IsTrue(AddSelection(harness, item, 80));

        Assert.IsTrue(RemoveSelection(harness, item, 30));

        Assert.AreEqual(100, harness.Inventory.GetCount(item));
        Assert.AreEqual(50, harness.Selections.GetCount(item));
        Assert.AreEqual(50, GetCount(harness.ProjectedInventory!, item));
    }

    [TestMethod]
    public void InventoryChanged_ReducesSelectionToCurrentOwnedQuantity()
    {
        var harness = CreateHarness();
        var item = CreateItem(100, 100);
        Assert.IsTrue(harness.Inventory.Add(item));
        Assert.IsTrue(AddSelection(harness, item, 80));
        Assert.AreEqual(40, harness.Inventory.Remove(item.Clone(40)));

        Assert.IsFalse(harness.InventoryChangedHandler!(new InventoryChangedEvent(harness.Character)));

        Assert.AreEqual(60, harness.Inventory.GetCount(item));
        Assert.AreEqual(60, harness.Selections.GetCount(item));
        Assert.AreEqual(0, GetCount(harness.ProjectedInventory!, item));
    }

    [TestMethod]
    public void InventoryChanged_RemovesSelectionWhenItemDisappears()
    {
        var harness = CreateHarness();
        var item = CreateItem(100, 3, stackable: false);
        Assert.IsTrue(harness.Inventory.Add(item));
        Assert.IsTrue(AddSelection(harness, item, 2));
        Assert.AreEqual(3, harness.Inventory.Remove(item.Clone(3)));

        harness.InventoryChangedHandler!(new InventoryChangedEvent(harness.Character));

        Assert.AreEqual(0, harness.Selections.GetCount(item));
        Assert.AreEqual(0, GetCount(harness.ProjectedInventory!, item));
    }

    [TestMethod]
    public void InventoryChanged_DoesNotIncreaseSelectionWhenInventoryGrows()
    {
        var harness = CreateHarness();
        var item = CreateItem(100, 100);
        Assert.IsTrue(harness.Inventory.Add(item));
        Assert.IsTrue(AddSelection(harness, item, 80));
        Assert.IsTrue(harness.Inventory.Add(item.Clone(20)));

        harness.InventoryChangedHandler!(new InventoryChangedEvent(harness.Character));

        Assert.AreEqual(120, harness.Inventory.GetCount(item));
        Assert.AreEqual(80, harness.Selections.GetCount(item));
        Assert.AreEqual(40, GetCount(harness.ProjectedInventory!, item));
    }

    [TestMethod]
    public void Projection_RespectsExtraDataAndMultipleNonStackableSlots()
    {
        var harness = CreateHarness();
        var firstVariant = CreateItem(100, 2, stackable: false, extraData: [1]);
        var secondVariant = CreateItem(100, 2, stackable: false, extraData: [2]);
        Assert.IsTrue(harness.Inventory.Add(firstVariant));
        Assert.IsTrue(harness.Inventory.Add(secondVariant));

        Assert.IsTrue(AddSelection(harness, firstVariant, 1));
        harness.Script.GetType().GetMethod("RefreshProjectedInventory", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(harness.Script, null);

        Assert.AreEqual(1, GetCount(harness.ProjectedInventory!, firstVariant));
        Assert.AreEqual(2, GetCount(harness.ProjectedInventory!, secondVariant));
        Assert.IsTrue(harness.Selections.GetById(firstVariant.Id)!.ExtraData.SequenceEqual(new long[] { 1 }));
        Assert.AreEqual(4, harness.Inventory.TakenSlots);
        Assert.IsTrue(Enumerable.Range(0, harness.ProjectedInventory!.Capacity)
            .Where(slot => harness.ProjectedInventory[slot] != null)
            .All(slot => !ReferenceEquals(harness.ProjectedInventory[slot], harness.Inventory[slot])));
    }

    [TestMethod]
    public void InsertX_RecomputesAvailabilityWhenPromptCompletes()
    {
        var harness = CreateHarness();
        var item = CreateItem(100, 100);
        Assert.IsTrue(harness.Inventory.Add(item));
        Assert.IsTrue(AddSelection(harness, item, 80));

        Assert.IsFalse(harness.InventoryClick!(0, ComponentClickType.Option5Click, item.Id, 0));
        Assert.IsNotNull(harness.Character.Widgets.IntInputHandler);
        Assert.IsTrue(harness.Inventory.Add(item.Clone(20)));
        harness.InventoryChangedHandler!(new InventoryChangedEvent(harness.Character));
        ((OnIntInput)GetField(harness.Script, "_inputHandler")!)(50);

        Assert.AreEqual(120, harness.Inventory.GetCount(item));
        Assert.AreEqual(120, harness.Selections.GetCount(item));
        Assert.AreEqual(0, GetCount(harness.ProjectedInventory!, item));
    }

    [TestMethod]
    public void Close_UnregistersHandlerClearsPendingInputAndRestoresRealInventoryView()
    {
        var harness = CreateHarness();
        var item = CreateItem(100, 10);
        Assert.IsTrue(harness.Inventory.Add(item));
        Assert.IsTrue(AddSelection(harness, item, 4));
        Assert.IsFalse(harness.InventoryClick!(0, ComponentClickType.Option5Click, item.Id, 0));
        var pendingInput = (OnIntInput)GetField(harness.Script, "_inputHandler")!;
        harness.Widgets.IntInputHandler = pendingInput;

        harness.Script.OnClose();

        Assert.AreEqual(10, harness.Inventory.GetCount(item));
        Assert.AreSame(harness.Inventory, harness.ProjectedInventory);
        Assert.IsNull(harness.Character.Widgets.IntInputHandler);
        Assert.IsNull(GetField(harness.Script, "_priceCheckInterface"));
        harness.Character.Received(1).UnregisterEventHandler<InventoryChangedEvent>(harness.InventoryChangeHandle!);
        Assert.IsNotNull(pendingInput);
        harness.Configurations.Received().SendItems(93, false, harness.Inventory, null);
    }

    private static bool AddSelection(Harness harness, IItem item, int amount) =>
        (bool)typeof(PriceChecker).GetMethod("AddItemToPriceChecker", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(harness.Script, [item, amount])!;

    private static bool RemoveSelection(Harness harness, IItem item, int amount) =>
        (bool)typeof(PriceChecker).GetMethod("RemoveSelection", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(harness.Script, [item, amount])!;

    private static int GetCount(IContainer<IItem?> container, IItem matching)
    {
        var count = 0;
        for (var slot = 0; slot < container.Capacity; slot++)
        {
            if (container[slot] is { } item && item.Equals(matching, true))
            {
                count += item.Count;
            }
        }

        return count;
    }

    private static object? GetField(object instance, string name) =>
        instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance);

    private static Harness CreateHarness()
    {
        var inventory = new TestInventory(28);
        var character = Substitute.For<ICharacter>();
        var configurations = Substitute.For<IConfigurations>();
        character.Inventory.Returns(inventory);
        character.Configurations.Returns(configurations);

        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        var defaultScript = new DefaultWidgetScript(accessor);
        using var provider = new ServiceCollection().AddSingleton(defaultScript).BuildServiceProvider();
        character.ServiceProvider.Returns(provider);

        var widgets = Substitute.For<IWidgetContainer>();
        var interfaceInstance = Substitute.For<IWidget>();
        var inventoryOverlay = Substitute.For<IWidget>();
        character.Widgets.Returns(widgets);
        widgets.OpenInventoryOverlay(207, 1, Arg.Any<IWidgetScript>()).Returns(true);
        widgets.GetOpenWidget(207).Returns(inventoryOverlay);

        OnComponentClick? inventoryClick = null;
        inventoryOverlay.When(widget => widget.AttachClickHandler(0, Arg.Any<OnComponentClick>()))
            .Do(call => inventoryClick = call.ArgAt<OnComponentClick>(1));

        EventHappened<InventoryChangedEvent>? inventoryChangedHandler = null;
        EventHappened? inventoryChangeHandle = _ => false;
        character.RegisterEventHandler<InventoryChangedEvent>(Arg.Any<EventHappened<InventoryChangedEvent>>())
            .Returns(call =>
            {
                inventoryChangedHandler = call.ArgAt<EventHappened<InventoryChangedEvent>>(0);
                return inventoryChangeHandle;
            });

        var script = new PriceChecker(accessor);
        script.Initialize(interfaceInstance);
        script.OnOpen();
        var selections = (BaseItemContainer)GetField(script, "_priceCheckInterface")!;

        IContainer<IItem?>? projectedInventory = null;
        configurations.When(configuration => configuration.SendItems(
                Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<IContainer<IItem?>>(), Arg.Any<HashSet<int>?>()))
            .Do(call =>
            {
                if (call.ArgAt<int>(0) == 93)
                {
                    projectedInventory = call.ArgAt<IContainer<IItem?>>(2);
                }
            });

        return new Harness(character, configurations, inventory, widgets, script, selections, inventoryOverlay,
            inventoryClick, inventoryChangedHandler, inventoryChangeHandle, () => projectedInventory);
    }

    private static IItem CreateItem(int id, int count, bool stackable = true, long[]? extraData = null)
    {
        var definition = Substitute.For<IItemDefinition>();
        definition.Stackable.Returns(stackable);
        var itemScript = Substitute.For<IItemScript>();
        itemScript.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(call =>
        {
            var left = call.ArgAt<IItem>(0);
            var right = call.ArgAt<IItem>(1);
            return left.Id == right.Id && left.ExtraData.SequenceEqual(right.ExtraData) &&
                   (call.ArgAt<bool>(2) || left.ItemDefinition.Stackable || left.ItemDefinition.Noted);
        });
        itemScript.CanTradeItem(Arg.Any<IItem>(), Arg.Any<ICharacter>()).Returns(true);

        IItem Make(int itemCount)
        {
            var currentCount = itemCount;
            var item = Substitute.For<IItem>();
            item.Id.Returns(id);
            item.Count.Returns(_ => currentCount);
            item.When(value => value.Count = Arg.Any<int>()).Do(call => currentCount = call.Arg<int>());
            item.ItemDefinition.Returns(definition);
            item.ItemScript.Returns(itemScript);
            item.ExtraData.Returns(extraData ?? []);
            item.Clone().Returns(_ => Make(currentCount));
            item.Clone(Arg.Any<int>()).Returns(call => Make(call.Arg<int>()));
            item.Equals(Arg.Any<IItem>(), Arg.Any<bool>()).Returns(call =>
            {
                var other = call.ArgAt<IItem>(0);
                var ignoreCount = call.ArgAt<bool>(1);
                return other != null && other.Id == id && (ignoreCount || other.Count == currentCount) &&
                       (extraData ?? []).SequenceEqual(other.ExtraData);
            });
            return item;
        }

        return Make(count);
    }

    private sealed class Harness(
        ICharacter character,
        IConfigurations configurations,
        TestInventory inventory,
        IWidgetContainer widgets,
        PriceChecker script,
        BaseItemContainer selections,
        IWidget inventoryOverlay,
        OnComponentClick? inventoryClick,
        EventHappened<InventoryChangedEvent>? inventoryChangedHandler,
        EventHappened? inventoryChangeHandle,
        Func<IContainer<IItem?>?> getProjectedInventory)
    {
        public ICharacter Character { get; } = character;
        public IConfigurations Configurations { get; } = configurations;
        public TestInventory Inventory { get; } = inventory;
        public IWidgetContainer Widgets { get; } = widgets;
        public PriceChecker Script { get; } = script;
        public BaseItemContainer Selections { get; } = selections;
        public IWidget InventoryOverlay { get; } = inventoryOverlay;
        public OnComponentClick? InventoryClick { get; } = inventoryClick;
        public EventHappened<InventoryChangedEvent>? InventoryChangedHandler { get; } = inventoryChangedHandler;
        public EventHappened? InventoryChangeHandle { get; } = inventoryChangeHandle;
        public IContainer<IItem?>? ProjectedInventory => getProjectedInventory();
    }

    private sealed class TestInventory(int capacity) : TradeItemContainer(StorageType.Normal, capacity), IInventoryContainer
    {
        public bool DropItem(IItem item) => false;

        public override void OnUpdate(HashSet<int>? slots = null) { }
    }
}
