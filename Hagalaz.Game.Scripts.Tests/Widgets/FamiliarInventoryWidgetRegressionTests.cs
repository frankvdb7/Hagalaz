using System.Reflection;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Npcs.Familiars;
using Hagalaz.Game.Scripts.Skills.Summoning;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets;

[TestClass]
public sealed class FamiliarInventoryWidgetRegressionTests
{
    [TestMethod]
    public void FamiliarWidget_UsesOwnerAndFamiliarInventorySourcesAndFamiliarAllCount()
    {
        var ownerItems = new ComposedTestContainer(28);
        var ownerItem = ComposedTestContainer.CreateTestItem(100, 9); ownerItems.SetItem(0, ownerItem);
        var familiarItems = new ComposedTestContainer(30);
        var familiarItem = ComposedTestContainer.CreateTestItem(100, 3); familiarItems.SetItem(0, familiarItem);
        var familiarInventory = Substitute.For<IFamiliarInventoryContainer>(); familiarInventory.Items.Returns(familiarItems.Items);
        var bob = CreateBob(); typeof(BobFamiliarScriptBase).GetField("<Inventory>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(bob, familiarInventory);
        var character = Substitute.For<ICharacter>(); character.Inventory.Returns(ownerItems); character.FamiliarScript.Returns(bob);
        character.Configurations.Returns(Substitute.For<IConfigurations>());
        var widgets = Substitute.For<IWidgetContainer>(); character.Widgets.Returns(widgets);
        var context = Substitute.For<ICharacterContext>(); context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>(); accessor.Context.Returns(context);
        var widget = Substitute.For<IWidget>(); var overlay = Substitute.For<IWidget>();
        var script = new FamiliarInventoryWidget(accessor); script.Initialize(widget);
        typeof(FamiliarInventoryWidget).GetField("_inventoryInterface", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(script, overlay);
        OnComponentClick? ownerClick = null; OnComponentClick? familiarClick = null;
        overlay.When(x => x.AttachClickHandler(0, Arg.Any<OnComponentClick>())).Do(call => ownerClick = call.ArgAt<OnComponentClick>(1));
        widget.When(x => x.AttachClickHandler(27, Arg.Any<OnComponentClick>())).Do(call => familiarClick = call.ArgAt<OnComponentClick>(1));
        script.Setup();

        Assert.IsTrue(ownerClick!(0, ComponentClickType.LeftClick, ownerItem.Id, 0));
        familiarInventory.Received(1).DepositFromInventory(ownerItem, 1);
        familiarInventory.DidNotReceive().DepositFromInventory(familiarItem, Arg.Any<int>());

        Assert.IsTrue(familiarClick!(27, ComponentClickType.Option5Click, familiarItem.Id, 0));
        familiarInventory.Received(1).WithdrawFromFamiliarInventory(familiarItem, 3);
        familiarInventory.DidNotReceive().WithdrawFromFamiliarInventory(ownerItem, Arg.Any<int>());
    }

    private static PackYak CreateBob() => new(
        Substitute.For<INpc>(), Substitute.For<IItemContainerFactory>(), Substitute.For<ISmartPathFinder>(),
        Substitute.For<INpcService>(), Substitute.For<IItemService>(), Substitute.For<IGroundItemBuilder>(),
        new TestItemBuilder(ComposedTestContainer.CreateTestItem), Substitute.For<IWidgetScriptActivator>());
}
