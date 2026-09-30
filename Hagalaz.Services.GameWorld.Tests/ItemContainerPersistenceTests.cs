using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Features.Shops;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Logic.Shops;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class ItemContainerPersistenceTests
{
    [TestMethod]
    public void InventoryRoundTrip_PreservesBeginningMiddleAndEndGaps()
    {
        using var scenario = new Scenario();
        AssertRoundTrip(() => new InventoryContainer(scenario.Owner, 7, Substitute.For<IMapRegionService>(),
                Substitute.For<IGroundItemBuilder>(), scenario.Builder),
            container => container.Items,
            [new(101, 1, 1, null), new(102, 2, 3, null), new(103, 1, 5, null)]);
    }

    [TestMethod]
    public void InventoryDehydrate_CapturesSlotsBeforeMappingItems()
    {
        using var scenario = new Scenario();
        var container = new InventoryContainer(scenario.Owner, 7, Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(), scenario.Builder);
        var first = Substitute.For<IItem>();
        first.Id.Returns(101);
        first.Count.Returns(1);
        var second = scenario.Builder.Create().WithId(102).WithCount(2).Build();
        var items = new IItem[container.Items.Capacity];
        items[0] = first;
        items[4] = second;
        for (var slot = 0; slot < items.Length; slot++)
        {
            if (items[slot] is { } item) container.Items.Replace(slot, item);
        }
        first.SerializeExtraData().Returns(_ =>
        {
            container.Items.Clear(false);
            return null;
        });

        var saved = container.Dehydrate();

        CollectionAssert.AreEqual(new[] { 0, 4 }, saved.Select(item => item.SlotId).ToArray());
        Assert.AreEqual(102, saved[1].ItemId);
        Assert.AreEqual(0, container.Items.TakenSlots);
    }

    [TestMethod]
    public void BankRoundTrip_PreservesSparseSlots()
    {
        using var scenario = new Scenario();
        AssertRoundTrip(() => new BankContainer(scenario.Owner, 12, scenario.Builder), container => container.Items,
            [new(101, 2, 2, null), new(102, 3, 9, null)]);
    }















    [TestMethod]
    public void EquipmentRoundTrip_PreservesSemanticSlotsAndGaps()
    {
        using var scenario = new Scenario();
        AssertRoundTrip(() => new EquipmentContainer(scenario.Owner, 15, scenario.Builder), container => container,
            [new(101, 1, (int)EquipmentSlot.Hat, null), new(102, 1, (int)EquipmentSlot.Weapon, null),
                new(103, 1, (int)EquipmentSlot.Shield, null), new(104, 1, (int)EquipmentSlot.Ring, null)]);
    }

    [TestMethod]
    public void FamiliarInventoryRoundTrip_PreservesSparseSlots()
    {
        using var scenario = new Scenario();
        FamiliarInventoryContainer Create() => new(scenario.Owner, StorageType.Normal, 8, scenario.Builder);
        var source = Create();
        source.Hydrate([new HydratedItem(101, 1, 1, null), new HydratedItem(102, 2, 6, null)]);

        var saved = source.Dehydrate();
        CollectionAssert.AreEqual(new[] { 1, 6 }, saved.Select(item => item.SlotId).ToArray());

        var restored = Create();
        restored.Hydrate(saved);
        AssertSlots(restored.Items, (1, 101), (6, 102));
    }

    [TestMethod]
    public void RewardRoundTrip_PreservesSparseSlots()
    {
        using var scenario = new Scenario();
        AssertRoundTrip(() => new RewardContainer(scenario.Owner, scenario.Builder), container => container.Items,
            [new(101, 1, 4, null), new(102, 2, 200, null)]);
    }

    [TestMethod]
    public void MoneyPouchRoundTrip_PreservesZeroCoinSentinel()
    {
        using var scenario = new Scenario();
        var source = new MoneyPouchContainer(scenario.Owner, scenario.Builder);
        var saved = source.Dehydrate();
        Assert.HasCount(1, saved);
        Assert.AreEqual(0, saved[0].SlotId);
        Assert.AreEqual(0, saved[0].Count);

        var restored = new MoneyPouchContainer(scenario.Owner, scenario.Builder);
        restored.Hydrate(saved);
        Assert.AreEqual(0, restored.Count);
        Assert.AreEqual(995, restored.Dehydrate()[0].ItemId);
    }

    [TestMethod]
    public void MoneyPouchHydration_EmptyStateRestoresZeroCoinSentinel()
    {
        using var scenario = new Scenario();
        var container = new MoneyPouchContainer(scenario.Owner, scenario.Builder);
        container.Hydrate([new HydratedItemDto(995, 25, 0, null)]);

        container.Hydrate([]);

        Assert.AreEqual(0, container.Count);
        var saved = container.Dehydrate();
        Assert.HasCount(1, saved);
        Assert.AreEqual(new HydratedItemDto(995, 0, 0, null), saved[0]);
    }

    [TestMethod]
    public void MoneyPouchHydration_RejectsNonCoinItemWithoutChangingState()
    {
        using var scenario = new Scenario();
        var container = new MoneyPouchContainer(scenario.Owner, scenario.Builder);
        container.Hydrate([new HydratedItemDto(995, 25, 0, null)]);

        Assert.ThrowsExactly<ArgumentException>(() => container.Hydrate([new HydratedItemDto(101, 5, 0, null)]));

        Assert.AreEqual(25, container.Count);
        Assert.AreEqual(new HydratedItemDto(995, 25, 0, null), container.Dehydrate()[0]);
    }

    [TestMethod]
    public void InventoryRoundTrip_PreservesItemExtraData()
    {
        using var scenario = new Scenario();
        InventoryContainer Create() => new(scenario.Owner, 7, Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(), scenario.Builder);
        var source = Create();
        source.Hydrate([new HydratedItemDto(101, 1, 4, "11,22")]);

        var saved = source.Dehydrate();
        Assert.AreEqual("11,22", saved[0].ExtraData);

        var restored = Create();
        restored.Hydrate(saved);
        CollectionAssert.AreEqual(new long[] { 11, 22 }, restored.Items[4]!.ExtraData);
    }

    [TestMethod]
    [DataRow("Inventory")]
    [DataRow("Bank")]
    [DataRow("Reward")]
    [DataRow("Familiar")]
    [DataRow("Equipment")]
    public void OrdinaryHydration_InvalidPhysicalSlot_ThrowsOutOfRangeWithoutChangingState(string containerName)
    {
        using var scenario = new Scenario();
        var subject = CreateHydrationSubject(containerName, scenario);
        subject.Hydrate([new HydrationEntry(101, 1, 0)]);
        var original = subject.Container[0];

        foreach (var invalidSlot in new[] { -1, subject.Container.Capacity })
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                subject.Hydrate([new HydrationEntry(102, 1, invalidSlot)]));
            Assert.AreSame(original, subject.Container[0]);
        }
    }

    [TestMethod]
    [DataRow("Inventory")]
    [DataRow("Bank")]
    [DataRow("Reward")]
    [DataRow("Familiar")]
    [DataRow("Equipment")]
    public void OrdinaryHydration_NonPositiveCount_ThrowsOutOfRangeWithoutChangingState(string containerName)
    {
        using var scenario = new Scenario();
        var subject = CreateHydrationSubject(containerName, scenario);
        subject.Hydrate([new HydrationEntry(101, 1, 0)]);
        var original = subject.Container[0];

        foreach (var invalidCount in new[] { 0, -1 })
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                subject.Hydrate([new HydrationEntry(102, invalidCount, 1)]));
            Assert.AreSame(original, subject.Container[0]);
        }
    }

    [TestMethod]
    [DataRow("Inventory")]
    [DataRow("Bank")]
    [DataRow("Reward")]
    [DataRow("Familiar")]
    [DataRow("Equipment")]
    public void OrdinaryHydration_DuplicatePhysicalSlot_ThrowsArgumentExceptionWithoutChangingState(string containerName)
    {
        using var scenario = new Scenario();
        var subject = CreateHydrationSubject(containerName, scenario);
        subject.Hydrate([new HydrationEntry(101, 1, 0)]);
        var original = subject.Container[0];

        Assert.ThrowsExactly<ArgumentException>(() => subject.Hydrate(
            [new HydrationEntry(102, 1, 1), new HydrationEntry(103, 1, 1)]));

        Assert.AreSame(original, subject.Container[0]);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(1)]
    public void MoneyPouchHydration_InvalidCoinSlot_ThrowsOutOfRangeWithoutChangingState(int invalidSlot)
    {
        using var scenario = new Scenario();
        var container = new MoneyPouchContainer(scenario.Owner, scenario.Builder);
        container.Hydrate([new HydratedItemDto(995, 25, 0, null)]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => container.Hydrate(
            [new HydratedItemDto(995, 5, invalidSlot, null)]));

        Assert.AreEqual(25, container.Count);
        Assert.AreEqual(new HydratedItemDto(995, 25, 0, null), container.Dehydrate()[0]);
    }

    [TestMethod]
    public void MoneyPouchHydration_NegativeCoinCount_ThrowsOutOfRangeWithoutChangingState()
    {
        using var scenario = new Scenario();
        var container = new MoneyPouchContainer(scenario.Owner, scenario.Builder);
        container.Hydrate([new HydratedItemDto(995, 25, 0, null)]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => container.Hydrate(
            [new HydratedItemDto(995, -1, 0, null)]));

        Assert.AreEqual(25, container.Count);
        Assert.AreEqual(new HydratedItemDto(995, 25, 0, null), container.Dehydrate()[0]);
    }

    [TestMethod]
    public void MoneyPouchHydration_DuplicateCoinSlot_ThrowsArgumentExceptionWithoutChangingState()
    {
        using var scenario = new Scenario();
        var container = new MoneyPouchContainer(scenario.Owner, scenario.Builder);
        container.Hydrate([new HydratedItemDto(995, 25, 0, null)]);

        Assert.ThrowsExactly<ArgumentException>(() => container.Hydrate(
            [new HydratedItemDto(995, 1, 0, null), new HydratedItemDto(995, 2, 0, null)]));

        Assert.AreEqual(25, container.Count);
        Assert.AreEqual(new HydratedItemDto(995, 25, 0, null), container.Dehydrate()[0]);
    }

    [TestMethod]
    public void InventoryHydration_KeepsIdenticalStackableItemsInTheirPersistedSlots()
    {
        using var scenario = new Scenario();
        var container = new InventoryContainer(scenario.Owner, 7, Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(), scenario.Builder);

        container.Hydrate([new HydratedItemDto(101, 2, 2, null), new HydratedItemDto(101, 3, 5, null)]);

        AssertSlots(container.Items, (2, 101), (5, 101));
        Assert.AreEqual(2, container.Items[2]!.Count);
        Assert.AreEqual(3, container.Items[5]!.Count);
    }

    private static void AssertRoundTrip<T>(Func<T> create, Func<T, IContainer<IItem?>> getItems,
        IReadOnlyList<HydratedItemDto> initial)
        where T : IHydratable<IReadOnlyList<HydratedItemDto>>, IDehydratable<IReadOnlyList<HydratedItemDto>>
    {
        var source = create();
        source.Hydrate(initial);

        var saved = source.Dehydrate();
        CollectionAssert.AreEqual(initial.Select(item => item.SlotId).ToArray(), saved.Select(item => item.SlotId).ToArray());

        var restored = create();
        restored.Hydrate(saved);
        var items = getItems(restored);
        AssertSlots(items, initial.Select(item => (item.SlotId, item.ItemId)).ToArray());
        foreach (var item in initial)
        {
            Assert.AreEqual(item.Count, items[item.SlotId]!.Count);
        }
    }

    private static void AssertSlots(IContainer<IItem?> container, params (int Slot, int ItemId)[] occupied)
    {
        for (var slot = 0; slot < container.Capacity; slot++)
        {
            var match = occupied.FirstOrDefault(item => item.Slot == slot);
            Assert.AreEqual(match.ItemId == 0 ? (int?)null : match.ItemId, container[slot]?.Id, $"Slot {slot}");
        }
    }

    private static HydrationSubject CreateHydrationSubject(string containerName, Scenario scenario)
    {
        switch (containerName)
        {
            case "Inventory":
            {
                var container = new InventoryContainer(scenario.Owner, 7, Substitute.For<IMapRegionService>(),
                    Substitute.For<IGroundItemBuilder>(), scenario.Builder);
                return new(container.Items, entries =>
                    container.Hydrate(entries.Select(entry => new HydratedItemDto(entry.ItemId, entry.Count, entry.SlotId, null)).ToArray()));
            }
            case "Bank":
            {
                var container = new BankContainer(scenario.Owner, 12, scenario.Builder);
                return new(container.Items, entries =>
                    container.Hydrate(entries.Select(entry => new HydratedItemDto(entry.ItemId, entry.Count, entry.SlotId, null)).ToArray()));
            }
            case "Reward":
            {
                var container = new RewardContainer(scenario.Owner, scenario.Builder);
                return new(container.Items, entries =>
                    container.Hydrate(entries.Select(entry => new HydratedItemDto(entry.ItemId, entry.Count, entry.SlotId, null)).ToArray()));
            }
            case "Familiar":
            {
                var container = new FamiliarInventoryContainer(scenario.Owner, StorageType.Normal, 8, scenario.Builder);
                return new(container.Items, entries =>
                    container.Hydrate(entries.Select(entry => new HydratedItem(entry.ItemId, entry.Count, entry.SlotId, null)).ToArray()));
            }
            case "Equipment":
            {
                var container = new EquipmentContainer(scenario.Owner, 15, scenario.Builder);
                return new(container, entries =>
                    container.Hydrate(entries.Select(entry => new HydratedItemDto(entry.ItemId, entry.Count, entry.SlotId, null)).ToArray()));
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(containerName));
        }
    }

    private sealed record HydrationSubject(IContainer<IItem?> Container, Action<HydrationEntry[]> Hydrate);
    private readonly record struct HydrationEntry(int ItemId, int Count, int SlotId);

}
