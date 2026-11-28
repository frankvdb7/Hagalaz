using System.Linq.Expressions;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Scripts.Dialogues;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Npcs;

[TestClass]
public sealed class SellStolenGoodsDialogueTests
{
    [TestMethod]
    public void SellingGoods_WhenPriceExceedsCoinLimit_AddsMaximumCoinCount()
    {
        var character = Substitute.For<ICharacter>();
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(context);

        var inventory = Substitute.For<IInventoryContainer>();
        inventory.RemoveItem(Arg.Any<IItem>()).Returns(2);
        character.Inventory.Returns(inventory);
        var moneyPouch = Substitute.For<IMoneyPouchContainer>();
        character.MoneyPouch.Returns(moneyPouch);
        character.Widgets.Returns(Substitute.For<IWidgetContainer>());

        var itemDefinition = Substitute.For<IItemDefinition>();
        itemDefinition.TradeValue.Returns(int.MaxValue);
        var item = Substitute.For<IItem>();
        item.ItemDefinition.Returns(itemDefinition);

        var itemOptional = Substitute.For<IItemOptional>();
        itemOptional.WithCount(Arg.Any<int>()).Returns(itemOptional);
        itemOptional.Build().Returns(item);
        var itemId = Substitute.For<IItemId>();
        itemId.WithId(Arg.Any<int>()).Returns(itemOptional);
        var itemBuilder = Substitute.For<IItemBuilder>();
        itemBuilder.Create().Returns(itemId);

        var rates = Substitute.For<IRatesService>();
        rates.GetRate<ItemOptions>(Arg.Any<Expression<Func<ItemOptions, double>>>()).Returns(1d);
        var dialogue = new SellStolenGoodsDialogue(contextAccessor, rates, itemBuilder);
        dialogue.Initialize(Substitute.For<IWidget>());
        var shopkeeper = Substitute.For<INpc>();
        var shopkeeperDefinition = Substitute.For<INpcDefinition>();
        shopkeeper.Definition.Returns(shopkeeperDefinition);
        dialogue.SetSource(shopkeeper);
        dialogue.OnOpen();

        dialogue.OnDialogueOptionClick("Yes, please!", -1, -1);

        moneyPouch.Received(16).Add(int.MaxValue);
    }
}
