using System.Reflection;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Npcs.Familiars;
using Hagalaz.Game.Scripts.Skills.Summoning;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
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

    [TestMethod]
    public void FamiliarWidget_BusyOwnerCannotWithdrawFromActualFamiliarInventory()
    {
        var ownerItems = new ComposedTestContainer(28);
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(ownerItems);
        character.Configurations.Returns(Substitute.For<IConfigurations>());
        var combat = Substitute.For<ICreatureCombat>();
        combat.IsDead.Returns(false);
        combat.IsInCombat().Returns(false);
        character.Combat.Returns(combat);
        var busyScript = Substitute.For<ICharacterScript>();
        busyScript.IsBusy().Returns(true);
        character.GetScripts().Returns([busyScript]);

        var itemBuilder = new TestItemBuilder(ComposedTestContainer.CreateTestItem);
        var familiarInventory = new FamiliarInventoryContainer(character, StorageType.Normal, 30, itemBuilder);
        var containerFactory = Substitute.For<IItemContainerFactory>();
        containerFactory.Create(character, StorageType.Normal, 30).Returns(familiarInventory);
        var bob = CreateBob(containerFactory);
        bob.AttachToSummoner(character, new SummoningDto());
        familiarInventory.Hydrate([new HydratedItem(100, 1, 0, null)]);
        character.FamiliarScript.Returns(bob);

        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        character.ServiceProvider.GetService(typeof(DefaultWidgetScript)).Returns(new DefaultWidgetScript(accessor));

        var overlay = Substitute.For<IWidget>();
        var widgets = Substitute.For<IWidgetContainer>();
        widgets.OpenInventoryOverlay(665, 1, Arg.Any<IWidgetScript>()).Returns(true);
        widgets.GetOpenWidget(665).Returns(overlay);
        character.Widgets.Returns(widgets);

        OnComponentClick? withdrawClick = null;
        var widget = Substitute.For<IWidget>();
        widget.When(x => x.AttachClickHandler(27, Arg.Any<OnComponentClick>()))
            .Do(call => withdrawClick = call.ArgAt<OnComponentClick>(1));

        var script = new FamiliarInventoryWidget(accessor);
        script.Initialize(widget);
        script.OnOpen();

        Assert.IsTrue(withdrawClick!(27, ComponentClickType.LeftClick, 100, 0));

        Assert.AreEqual(1, familiarInventory.Items.GetCountById(100));
        Assert.AreEqual(0, ownerItems.Items.GetCountById(100));
        character.Received(1).SendChatMessage(
            "Please finish what you're doing before withdrawing items from your familiar.");
    }

    [TestMethod]
    public void FamiliarWidget_WithInventoryOverlayOpenWithdrawsItemsFromActualFamiliarInventory()
    {
        var ownerItems = new ComposedTestContainer(28);
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(ownerItems);
        character.Configurations.Returns(Substitute.For<IConfigurations>());
        character.IsBusy().Returns(true);
        character.GetScripts().Returns(Array.Empty<ICharacterScript>());
        var combat = Substitute.For<ICreatureCombat>();
        combat.IsDead.Returns(false);
        combat.IsInCombat().Returns(false);
        character.Combat.Returns(combat);

        var itemBuilder = new TestItemBuilder(ComposedTestContainer.CreateTestItem);
        var familiarInventory = new FamiliarInventoryContainer(character, StorageType.Normal, 30, itemBuilder);
        var containerFactory = Substitute.For<IItemContainerFactory>();
        containerFactory.Create(character, StorageType.Normal, 30).Returns(familiarInventory);
        var bob = CreateBob(containerFactory);
        bob.AttachToSummoner(character, new SummoningDto());
        familiarInventory.Hydrate([
            new HydratedItem(100, 1, 0, null),
            new HydratedItem(101, 1, 1, null)
        ]);
        character.FamiliarScript.Returns(bob);

        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        character.ServiceProvider.GetService(typeof(DefaultWidgetScript)).Returns(new DefaultWidgetScript(accessor));

        var overlay = Substitute.For<IWidget>();
        var widgets = Substitute.For<IWidgetContainer>();
        widgets.OpenInventoryOverlay(665, 1, Arg.Any<IWidgetScript>()).Returns(true);
        widgets.GetOpenWidget(665).Returns(overlay);
        character.Widgets.Returns(widgets);

        OnComponentClick? withdrawClick = null;
        OnComponentClick? takeAllClick = null;
        var widget = Substitute.For<IWidget>();
        widget.When(x => x.AttachClickHandler(27, Arg.Any<OnComponentClick>()))
            .Do(call => withdrawClick = call.ArgAt<OnComponentClick>(1));
        widget.When(x => x.AttachClickHandler(29, Arg.Any<OnComponentClick>()))
            .Do(call => takeAllClick = call.ArgAt<OnComponentClick>(1));

        var script = new FamiliarInventoryWidget(accessor);
        script.Initialize(widget);
        script.OnOpen();

        Assert.IsTrue(withdrawClick!(27, ComponentClickType.LeftClick, 100, 0));
        Assert.AreEqual(1, ownerItems.Items.GetCountById(100));
        Assert.AreEqual(1, familiarInventory.Items.GetCountById(101));

        Assert.IsTrue(takeAllClick!(29, ComponentClickType.LeftClick, 0, 0));

        Assert.AreEqual(1, ownerItems.Items.GetCountById(100));
        Assert.AreEqual(1, ownerItems.Items.GetCountById(101));
        Assert.AreEqual(0, familiarInventory.Items.TakenSlots);
        character.DidNotReceive().SendChatMessage(
            "Please finish what you're doing before withdrawing items from your familiar.");
    }

    private static PackYak CreateBob(IItemContainerFactory? itemContainerFactory = null) => new(
        Substitute.For<INpc>(), itemContainerFactory ?? Substitute.For<IItemContainerFactory>(), Substitute.For<ISmartPathFinder>(),
        Substitute.For<INpcService>(), Substitute.For<IItemService>(), Substitute.For<IGroundItemBuilder>(),
        new TestItemBuilder(ComposedTestContainer.CreateTestItem), Substitute.For<IWidgetScriptActivator>());
}
