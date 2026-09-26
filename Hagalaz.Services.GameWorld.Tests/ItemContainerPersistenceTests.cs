using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Builders;
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
            [new(101, 1, 1, null), new(102, 2, 3, null), new(103, 1, 5, null)]);
    }

    [TestMethod]
    public void BankRoundTrip_PreservesSparseSlots()
    {
        using var scenario = new Scenario();
        AssertRoundTrip(() => new BankContainer(scenario.Owner, 12, scenario.Builder),
            [new(101, 2, 2, null), new(102, 3, 9, null)]);
    }

    [TestMethod]
    public void EquipmentRoundTrip_PreservesSemanticSlotsAndGaps()
    {
        using var scenario = new Scenario();
        AssertRoundTrip(() => new EquipmentContainer(scenario.Owner, 15, scenario.Builder),
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
        AssertSlots(restored, (1, 101), (6, 102));
    }

    [TestMethod]
    public void RewardRoundTrip_PreservesSparseSlots()
    {
        using var scenario = new Scenario();
        AssertRoundTrip(() => new RewardContainer(scenario.Owner, scenario.Builder),
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
        Assert.AreEqual(995, restored[0]!.Id);
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
        CollectionAssert.AreEqual(new long[] { 11, 22 }, restored[4]!.ExtraData);
    }

    [TestMethod]
    public void InventoryHydration_RejectsDuplicateSlotsWithoutChangingState()
    {
        using var scenario = new Scenario();
        var container = new InventoryContainer(scenario.Owner, 7, Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(), scenario.Builder);
        container.Hydrate([new HydratedItemDto(101, 1, 4, null)]);

        Assert.ThrowsExactly<ArgumentException>(() => container.Hydrate(
            [new HydratedItemDto(102, 1, 2, null), new HydratedItemDto(103, 1, 2, null)]));
        AssertSlots(container, (4, 101));
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(7)]
    public void InventoryHydration_RejectsOutOfBoundsSlotWithoutChangingState(int invalidSlot)
    {
        using var scenario = new Scenario();
        var container = new InventoryContainer(scenario.Owner, 7, Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(), scenario.Builder);
        container.Hydrate([new HydratedItemDto(101, 1, 4, null)]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => container.Hydrate(
            [new HydratedItemDto(102, 1, invalidSlot, null)]));
        AssertSlots(container, (4, 101));
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    public void InventoryHydration_RejectsInvalidCountWithoutChangingState(int invalidCount)
    {
        using var scenario = new Scenario();
        var container = new InventoryContainer(scenario.Owner, 7, Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(), scenario.Builder);
        container.Hydrate([new HydratedItemDto(101, 1, 4, null)]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => container.Hydrate(
            [new HydratedItemDto(102, invalidCount, 2, null)]));
        AssertSlots(container, (4, 101));
    }

    [TestMethod]
    public void InventoryHydration_KeepsIdenticalStackableItemsInTheirPersistedSlots()
    {
        using var scenario = new Scenario();
        var container = new InventoryContainer(scenario.Owner, 7, Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(), scenario.Builder);

        container.Hydrate([new HydratedItemDto(101, 2, 2, null), new HydratedItemDto(101, 3, 5, null)]);

        AssertSlots(container, (2, 101), (5, 101));
        Assert.AreEqual(2, container[2]!.Count);
        Assert.AreEqual(3, container[5]!.Count);
    }

    private static void AssertRoundTrip<T>(Func<T> create, IReadOnlyList<HydratedItemDto> initial)
        where T : BaseItemContainer, IHydratable<IReadOnlyList<HydratedItemDto>>, IDehydratable<IReadOnlyList<HydratedItemDto>>
    {
        var source = create();
        source.Hydrate(initial);

        var saved = source.Dehydrate();
        CollectionAssert.AreEqual(initial.Select(item => item.SlotId).ToArray(), saved.Select(item => item.SlotId).ToArray());

        var restored = create();
        restored.Hydrate(saved);
        AssertSlots(restored, initial.Select(item => (item.SlotId, item.ItemId)).ToArray());
        foreach (var item in initial)
        {
            Assert.AreEqual(item.Count, restored[item.SlotId]!.Count);
        }
    }

    private static void AssertSlots(IItemContainer container, params (int Slot, int ItemId)[] occupied)
    {
        for (var slot = 0; slot < container.Capacity; slot++)
        {
            var match = occupied.FirstOrDefault(item => item.Slot == slot);
            Assert.AreEqual(match.ItemId == 0 ? null : match.ItemId, container[slot]?.Id, $"Slot {slot}");
        }
    }

    private sealed class Scenario : IDisposable
    {
        private readonly ServiceProvider _services;

        public ICharacter Owner { get; } = Substitute.For<ICharacter>();
        public IItemBuilder Builder { get; }

        public Scenario()
        {
            Owner.Statistics.Returns(Substitute.For<ICharacterStatistics>());
            var itemDefinition = Substitute.For<IItemDefinition>();
            itemDefinition.Stackable.Returns(true);
            var equipmentDefinition = Substitute.For<IEquipmentDefinition>();
            var itemScript = Substitute.For<IItemScript>();
            var equipmentScript = Substitute.For<IEquipmentScript>();
            var itemService = Substitute.For<IItemService>();
            itemService.FindItemDefinitionById(Arg.Any<int>()).Returns(itemDefinition);
            var equipmentService = Substitute.For<IEquipmentService>();
            equipmentService.FindEquipmentDefinitionById(Arg.Any<int>()).Returns(equipmentDefinition);
            var itemProvider = Substitute.For<IItemScriptProvider>();
            itemProvider.FindItemScriptById(Arg.Any<int>()).Returns(itemScript);
            var equipmentProvider = Substitute.For<IEquipmentScriptProvider>();
            equipmentProvider.FindEquipmentScriptById(Arg.Any<int>()).Returns(equipmentScript);
            _services = new ServiceCollection()
                .AddSingleton(itemService)
                .AddSingleton(equipmentService)
                .BuildServiceProvider();
            Builder = new ItemBuilder(_services, itemProvider, equipmentProvider);
        }

        public void Dispose() => _services.Dispose();
    }
}
