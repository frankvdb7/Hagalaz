using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Widgets.Bank;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets;

[TestClass]
public sealed class DepositBoxScreenRegressionTests
{
    [TestMethod]
    public void OptionXDepositsTheExactInventoryItemWithEnteredCount()
    {
        var inventory = new ComposedTestContainer(2);
        var item = ComposedTestContainer.CreateTestItem(100, 20);
        inventory.SetItem(0, item);
        var bank = Substitute.For<IBankContainer>();
        var profile = Substitute.For<IProfile>();
        var widgets = Substitute.For<IWidgetContainer>();
        var configurations = Substitute.For<IConfigurations>();
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        character.Bank.Returns(bank);
        character.Profile.Returns(profile);
        character.Widgets.Returns(widgets);
        character.Configurations.Returns(configurations);
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        var widget = Substitute.For<IWidget>();
        OnComponentClick? clickHandler = null;
        widget.When(x => x.AttachClickHandler(17, Arg.Any<OnComponentClick>()))
            .Do(call => clickHandler = call.ArgAt<OnComponentClick>(1));
        var screen = new DepositBoxScreen(accessor);
        screen.Initialize(widget);
        screen.OnOpen();

        Assert.IsTrue(clickHandler!(17, ComponentClickType.Option4Click, item.Id, 0));
        widgets.IntInputHandler!(7);

        bank.Received(1).DepositFromInventory(item, 7, out Arg.Any<Hagalaz.Game.Abstractions.Model.Items.IItem>());
        profile.Received(1).SetValue(Hagalaz.Configuration.ProfileConstants.BankSettingsOptionX, 7);
    }
}
