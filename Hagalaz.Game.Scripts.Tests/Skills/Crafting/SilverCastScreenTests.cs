using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Skills.Crafting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Crafting;

[TestClass]
public sealed class SilverCastScreenTests
{
    [TestMethod]
    public void Close_ClearsPendingMakeInput()
    {
        var inventory = new ComposedTestContainer(28);
        var definition = new SilverDto
        {
            ChildID = 1,
            MouldID = 200,
            ProductID = 300,
            RequiredLevel = 1,
            Experience = 1
        };
        inventory.SetItem(0, ComposedTestContainer.CreateTestItem(definition.MouldID));

        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        var statistics = Substitute.For<ICharacterStatistics>();
        statistics.GetSkillLevel(StatisticsConstants.Crafting).Returns(99);
        character.Statistics.Returns(statistics);
        character.Configurations.Returns(Substitute.For<IConfigurations>());

        OnIntInput? currentInputHandler = null;
        var widgets = Substitute.For<IWidgetContainer>();
        widgets.IntInputHandler.Returns(_ => currentInputHandler);
        widgets.When(x => x.IntInputHandler = Arg.Any<OnIntInput?>())
            .Do(call => currentInputHandler = call.ArgAt<OnIntInput?>(0));
        character.Widgets.Returns(widgets);

        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(context);

        var craftingService = Substitute.For<ICraftingService>();
        craftingService.FindAllSilver().Returns(Task.FromResult<IReadOnlyList<SilverDto>>([definition]));

        var interfaceInstance = Substitute.For<IWidget>();
        OnComponentClick? makeClick = null;
        interfaceInstance.When(x => x.AttachClickHandler(definition.ChildID, Arg.Any<OnComponentClick>()))
            .Do(call => makeClick = call.ArgAt<OnComponentClick>(1));

        var screen = new SilverCastScreen(contextAccessor, craftingService);
        screen.Initialize(interfaceInstance);
        screen.OnOpen();

        Assert.IsTrue(makeClick!(definition.ChildID, ComponentClickType.Option4Click, 0, 0));
        Assert.IsNotNull(currentInputHandler);

        screen.OnClose();

        Assert.IsNull(currentInputHandler);
        Assert.IsNull(widgets.IntInputHandler);
    }
}
