using System.Reflection;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Features.Shops;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Widgets.Bank;
using Hagalaz.Game.Scripts.Widgets.Shop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets;

[TestClass]
public sealed class ItemWidgetCallerRegressionTests
{
    [TestMethod]
    public void BankInventoryHandler_UsesCurrentItemAndConfiguredOptionXAndRejectsStaleId()
    {
        var inv = new ComposedTestContainer(28); var item = ComposedTestContainer.CreateTestItem(100, 20); inv.SetItem(0, item);
        var bankItems = new ComposedTestContainer(100); var bank = Substitute.For<IBankContainer>(); bank.Items.Returns(bankItems.Items);
        var deposit = new List<(IItem Item, int Count)>();
        bank.DepositFromInventory(Arg.Any<IItem>(), Arg.Any<int>(), out Arg.Any<IItem?>()).Returns(call =>
        { deposit.Add((call.ArgAt<IItem>(0), call.ArgAt<int>(1))); call[2] = null; return true; });
        var (character, interfaceWidget, inventoryWidget, mediator) = CreateBase(inv);
        character.Bank.Returns(bank);
        var profile = Substitute.For<IProfile>(); character.Profile.Returns(profile);
        profile.GetValue(ProfileConstants.BankSettingsOptionX, ProfileConstants.BankSettingsOptionXDefault).Returns(23);
        var click = AttachBank(character, interfaceWidget, inventoryWidget, mediator);

        Assert.IsTrue(click(0, ComponentClickType.LeftClick, item.Id, 0));
        Assert.AreEqual(1, deposit.Single().Count); deposit.Clear();
        Assert.IsTrue(click(0, ComponentClickType.Option4Click, item.Id, 0));
        Assert.AreEqual(23, deposit.Single().Count); deposit.Clear();
        Assert.IsFalse(click(0, ComponentClickType.LeftClick, 999, 0));
        Assert.AreEqual(0, deposit.Count);
    }

    [TestMethod]
    public void ShopInventoryHandler_RejectsStaleIdAndRoutesValidSellAction()
    {
        var inv = new ComposedTestContainer(28); var item = ComposedTestContainer.CreateTestItem(100, 20); inv.SetItem(0, item);
        var shopStock = Substitute.For<IShopStockContainer>(); shopStock.Items.Returns(new ComposedTestContainer(20).Items);
        var samples = Substitute.For<IShopStockContainer>(); samples.Items.Returns(new ComposedTestContainer(12).Items);
        var shop = Substitute.For<IShop>(); shop.Name.Returns("Test shop"); shop.CurrencyId.Returns(995);
        shop.MainStockContainer.Returns(shopStock); shop.SampleStockContainer.Returns(samples);
        var (character, interfaceWidget, inventoryWidget, _) = CreateBase(inv);
        character.CurrentShop.Returns(shop);
        var itemService = Substitute.For<IItemService>(); var currency = Substitute.For<IItemDefinition>(); currency.Name.Returns("Coins");
        itemService.FindItemDefinitionById(995).Returns(currency);
        var eventManager = Substitute.For<IEventManager>();
        var options = Substitute.For<IWidgetOptionBuilder>();
        var script = new ShopScreenScript(CreateAccessor(character), itemService, new TestItemBuilder(ComposedTestContainer.CreateTestItem), eventManager, options);
        script.Initialize(interfaceWidget); SetField(script, "_inventoryInterface", inventoryWidget);
        OnComponentClick? click = null;
        inventoryWidget.When(x => x.AttachClickHandler(0, Arg.Any<OnComponentClick>())).Do(call => click = call.ArgAt<OnComponentClick>(1));
        script.Setup();

        Assert.IsFalse(click!(0, ComponentClickType.Option2Click, 999, 0));
        shopStock.DidNotReceive().SellFromInventory(Arg.Any<ICharacter>(), Arg.Any<IItem>(), Arg.Any<int>());
        Assert.IsTrue(click(0, ComponentClickType.Option2Click, item.Id, 0));
        shopStock.Received(1).SellFromInventory(character, item, 1);
    }

    private static OnComponentClick AttachBank(ICharacter character, IWidget bankWidget, IWidget inventoryWidget, IScopedGameMediator mediator)
    {
        var script = new BankScreen(CreateAccessor(character), mediator); script.Initialize(bankWidget);
        SetField(script, "_inventoryInterface", inventoryWidget);
        OnComponentClick? click = null;
        inventoryWidget.When(x => x.AttachClickHandler(0, Arg.Any<OnComponentClick>())).Do(call => click = call.ArgAt<OnComponentClick>(1));
        script.Setup();
        return click!;
    }

    private static (ICharacter Character, IWidget Interface, IWidget Inventory, IScopedGameMediator Mediator) CreateBase(ComposedTestContainer inventory)
    {
        var character = Substitute.For<ICharacter>(); character.Inventory.Returns(inventory);
        character.Configurations.Returns(Substitute.For<IConfigurations>());
        character.Equipment.Returns(Substitute.For<IEquipmentContainer>());
        var widgets = Substitute.For<IWidgetContainer>(); character.Widgets.Returns(widgets);
        var context = Substitute.For<ICharacterContext>(); context.Character.Returns(character);
        var bankWidget = Substitute.For<IWidget>(); var inventoryWidget = Substitute.For<IWidget>();
        var mediator = Substitute.For<IScopedGameMediator>();
        return (character, bankWidget, inventoryWidget, mediator);
    }

    private static ICharacterContextAccessor CreateAccessor(ICharacter character)
    { var context = Substitute.For<ICharacterContext>(); context.Character.Returns(character); var accessor = Substitute.For<ICharacterContextAccessor>(); accessor.Context.Returns(context); return accessor; }

    private static void SetField(object instance, string name, object value) => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);
}
