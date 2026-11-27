using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Skills.Slayer;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets;

[TestClass]
public sealed class RewardsAssignmentScreenTests
{
    [TestMethod]
    [DataRow(23)]
    [DataRow(26)]
    public void SkipComponents_UseSingleSlayerOwnerAndRefreshPoints(int component)
    {
        var slayer = Substitute.For<ISlayer>();
        slayer.CurrentTaskId.Returns(1);
        slayer.TrySkipAssignment(30).Returns(true);
        var profile = Substitute.For<IProfile>();
        profile.GetValue(ProfileConstants.SlayerRewardPoints, Arg.Any<int>()).Returns(100);
        var character = Substitute.For<ICharacter>();
        character.Slayer.Returns(slayer);
        character.Profile.Returns(profile);
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        var widget = Substitute.For<IWidget>();
        var handlers = new Dictionary<int, OnComponentClick>();
        widget.When(x => x.AttachClickHandler(Arg.Any<int>(), Arg.Any<OnComponentClick>()))
            .Do(call => handlers[call.ArgAt<int>(0)] = call.ArgAt<OnComponentClick>(1));
        var screen = new RewardsAssignmentScreen(accessor);
        screen.Initialize(widget);
        screen.OnOpen();

        Assert.IsTrue(handlers[component](component, ComponentClickType.LeftClick, 0, 0));

        slayer.Received(1).TrySkipAssignment(30);
        widget.Received().DrawString(19, "70");
    }
}
