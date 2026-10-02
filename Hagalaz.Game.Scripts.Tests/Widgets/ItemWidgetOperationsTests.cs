using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Widgets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Game.Scripts.Tests.Widgets;

[TestClass]
public sealed class ItemWidgetOperationsTests
{
    [TestMethod]
    public void TryGetItem_RequiresValidMatchingOccupiedSlot()
    {
        var inventory = new ComposedTestContainer(2);
        var exactItem = ComposedTestContainer.CreateTestItem(100);
        inventory.Items.Add(exactItem);

        Assert.IsFalse(ItemWidgetOperations.TryGetItem(inventory.Items, 100, -1, out _));
        Assert.IsFalse(ItemWidgetOperations.TryGetItem(inventory.Items, 100, 2, out _));
        Assert.IsFalse(ItemWidgetOperations.TryGetItem(inventory.Items, 101, 1, out _));
        Assert.IsFalse(ItemWidgetOperations.TryGetItem(inventory.Items, 101, 0, out _));
        Assert.IsTrue(ItemWidgetOperations.TryGetItem(inventory.Items, 100, 0, out var actual));
        Assert.AreSame(exactItem, actual);
    }

    [DataTestMethod]
    [DataRow(ComponentClickType.LeftClick, 1)]
    [DataRow(ComponentClickType.Option2Click, 5)]
    [DataRow(ComponentClickType.Option3Click, 10)]
    [DataRow(ComponentClickType.Option4Click, 0)]
    public void GetCommonAmount_MapsOnlySupportedFixedCounts(ComponentClickType clickType, int expected)
    {
        Assert.AreEqual(expected, ItemWidgetOperations.GetCommonAmount(clickType));
    }
}
