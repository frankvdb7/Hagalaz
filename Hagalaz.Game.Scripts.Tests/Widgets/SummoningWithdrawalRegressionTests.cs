using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Npcs.Familiars;
using Hagalaz.Game.Scripts.Widgets.Orbs;
using Hagalaz.Game.Scripts.Widgets.Tabs;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets;

[TestClass]
public sealed class SummoningWithdrawalRegressionTests
{
    private const string BusyWithdrawalMessage = "Please finish what you're doing before withdrawing items from your familiar.";

    [TestMethod]
    public void SummoningOrb_BobWithdrawalIsRejectedWhileCharacterIsBusy()
    {
        var fixture = CreateBusyCharacter();
        var mediator = Substitute.For<IScopedGameMediator>();
        var widget = Substitute.For<IWidget>();
        OnComponentClick? takeBob = null;
        OnComponentClick? takeBobLeftClick = null;
        widget.When(x => x.AttachClickHandler(13, Arg.Any<OnComponentClick>()))
            .Do(call => takeBob = call.ArgAt<OnComponentClick>(1));
        widget.When(x => x.AttachClickHandler(22, Arg.Any<OnComponentClick>()))
            .Do(call => takeBobLeftClick = call.ArgAt<OnComponentClick>(1));

        var orb = new SummoningOrb(fixture.ContextAccessor, mediator,
            new LeftClickOptionTab(fixture.ContextAccessor, mediator));
        orb.Initialize(widget);
        orb.OnOpen();

        Assert.IsTrue(takeBob!(13, ComponentClickType.Option6Click, 0, 0));
        Assert.IsTrue(takeBobLeftClick!(22, ComponentClickType.LeftClick, 0, 0));

        fixture.Inventory.DidNotReceive().WithdrawAvailableToInventory();
        fixture.Character.Received(2).SendChatMessage(BusyWithdrawalMessage);
    }

    [TestMethod]
    public void SummoningTab_BobWithdrawalIsRejectedWhileCharacterIsBusy()
    {
        var fixture = CreateBusyCharacter();
        var widget = Substitute.For<IWidget>();
        OnComponentClick? takeBob = null;
        widget.When(x => x.AttachClickHandler(67, Arg.Any<OnComponentClick>()))
            .Do(call => takeBob = call.ArgAt<OnComponentClick>(1));

        var tab = new SummoningTab(fixture.ContextAccessor);
        tab.Initialize(widget);
        tab.OnOpen();

        Assert.IsTrue(takeBob!(67, ComponentClickType.LeftClick, 0, 0));

        fixture.Inventory.DidNotReceive().WithdrawAvailableToInventory();
        fixture.Character.Received(1).SendChatMessage(BusyWithdrawalMessage);
    }

    private static BusyCharacter CreateBusyCharacter()
    {
        var character = Substitute.For<ICharacter>();
        var inventory = Substitute.For<IFamiliarInventoryContainer>();
        var itemContainerFactory = Substitute.For<IItemContainerFactory>();
        itemContainerFactory.Create(character, StorageType.Normal, 30).Returns(inventory);
        var npc = Substitute.For<INpc>();
        npc.Appearance.Returns(Substitute.For<INpcAppearance>());
        var bob = new PackYak(npc, itemContainerFactory, Substitute.For<ISmartPathFinder>(),
            Substitute.For<INpcService>(), Substitute.For<IItemService>(), Substitute.For<IGroundItemBuilder>(),
            new TestItemBuilder(ComposedTestContainer.CreateTestItem), Substitute.For<IWidgetScriptActivator>());
        bob.AttachToSummoner(character, new SummoningDto());

        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(context);
        character.FamiliarScript.Returns(bob);
        character.IsBusy().Returns(true);
        character.Configurations.Returns(Substitute.For<IConfigurations>());

        return new BusyCharacter(character, inventory, contextAccessor);
    }

    private sealed record BusyCharacter(
        ICharacter Character,
        IFamiliarInventoryContainer Inventory,
        ICharacterContextAccessor ContextAccessor);
}
