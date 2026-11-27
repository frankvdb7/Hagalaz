using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Widgets.Tabs;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets.Tabs;

[TestClass]
public sealed class OptionsTabTests
{
    [TestMethod]
    [DataRow(6, ProfileConstants.ReportSettingsRightClick)]
    [DataRow(11, ProfileConstants.ChatSettingsProfanity)]
    [DataRow(12, ProfileConstants.ChatSettingsEffects)]
    [DataRow(14, ProfileConstants.MouseSettingsButtons)]
    [DataRow(15, ProfileConstants.MagicSettingsAcceptAid)]
    public void ToggleButton_PublishesItsProfileActionOnlyOnLeftClick(int buttonId, string profileKey)
    {
        var character = Substitute.For<ICharacter>();
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        var mediator = Substitute.For<IScopedGameMediator>();
        var widget = Substitute.For<IWidget>();
        OnComponentClick? click = null;
        widget.When(x => x.AttachClickHandler(buttonId, Arg.Any<OnComponentClick>()))
            .Do(call => click = call.ArgAt<OnComponentClick>(1));
        var tab = new OptionsTab(accessor, mediator);
        tab.Initialize(widget);
        tab.OnOpen();
        Assert.IsNotNull(click);

        Assert.IsFalse(click(buttonId, ComponentClickType.Option2Click, 41, 42));
        mediator.DidNotReceive().Publish(Arg.Any<ProfileToggleBoolAction>());

        Assert.IsTrue(click(buttonId, ComponentClickType.LeftClick, 41, 42));
        mediator.Received(1).Publish(new ProfileToggleBoolAction(profileKey));
    }
}
