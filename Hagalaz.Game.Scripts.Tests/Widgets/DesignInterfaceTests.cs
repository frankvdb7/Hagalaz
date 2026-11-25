using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Widgets.CharacterDesign;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets;

[TestClass]
public sealed class DesignInterfaceTests
{
    [TestMethod]
    public void Open_RegistersAppearanceAndNameSubmissionButtonsInStepOrder()
    {
        var character = Substitute.For<ICharacter>();
        var appearance = Substitute.For<ICharacterAppearance>();
        character.Appearance.Returns(appearance);
        character.Name.Returns("#new");
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        var widget = Substitute.For<IWidget>();
        var clicks = new Dictionary<int, OnComponentClick>();
        widget.When(x => x.AttachClickHandler(Arg.Any<int>(), Arg.Any<OnComponentClick>()))
            .Do(call =>
            {
                var click = call.ArgAt<OnComponentClick>(1);
                Assert.IsNotNull(click);
                clicks.Add(call.ArgAt<int>(0), click);
            });
        var script = new DesignInterface(accessor, Substitute.For<IGameMessageService>(),
            Substitute.For<ICharacterCreateInfoRepository>(), Substitute.For<IItemBuilder>());

        script.Initialize(widget);
        script.OnOpen();

        appearance.Received(1).DrawCharacter();
        Assert.IsFalse(clicks[138](138, ComponentClickType.LeftClick, 0, 0));
        Assert.IsTrue(clicks[139](139, ComponentClickType.LeftClick, 0, 0));
        widget.Received(1).SetVisible(138, true);
        Assert.IsTrue(clicks[138](138, ComponentClickType.LeftClick, 0, 0));
        character.Configurations.Received(1).SendCs2Script(3943, Arg.Is<object[]>(args => args != null && args.Length == 0));
        Assert.IsTrue(clicks[182](182, ComponentClickType.LeftClick, 0, 0));
        Assert.IsNotNull(character.Widgets.StringInputHandler);
    }
}
