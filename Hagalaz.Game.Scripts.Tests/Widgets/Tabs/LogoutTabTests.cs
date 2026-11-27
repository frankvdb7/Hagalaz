using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Widgets.Tabs;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets.Tabs;

[TestClass]
public sealed class LogoutTabTests
{
    [TestMethod]
    [DataRow(6, true)]
    [DataRow(13, false)]
    public void LogoutButton_UsesItsOwnDestinationOnlyOnLeftClick(int componentId, bool toLobby)
    {
        var character = Substitute.For<ICharacter>();
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        var widget = Substitute.For<IWidget>();
        OnComponentClick? click = null;
        widget.When(x => x.AttachClickHandler(componentId, Arg.Any<OnComponentClick>()))
            .Do(call => click = call.ArgAt<OnComponentClick>(1));
        var tab = new LogoutTab(accessor);
        tab.Initialize(widget);
        tab.OnOpen();
        Assert.IsNotNull(click);

        Assert.IsFalse(click(componentId, ComponentClickType.Option2Click, 41, 42));
        character.DidNotReceive().TryLogout(Arg.Any<bool>());

        Assert.IsTrue(click(componentId, ComponentClickType.LeftClick, 41, 42));
        character.Received(1).TryLogout(toLobby);
        character.DidNotReceive().TryLogout(!toLobby);
    }
}
