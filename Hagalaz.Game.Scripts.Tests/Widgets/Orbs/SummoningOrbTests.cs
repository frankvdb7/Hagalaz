using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Widgets.Orbs;
using Hagalaz.Game.Scripts.Widgets.Tabs;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets.Orbs;

[TestClass]
public sealed class SummoningOrbTests
{
    [TestMethod]
    public void AttackHandlers_QueueTheTargetForBothOrbOptions()
    {
        var familiarNpc = Substitute.For<INpc>();
        var familiarScript = Substitute.For<IFamiliarScript>();
        familiarScript.Familiar.Returns(familiarNpc);

        var character = Substitute.For<ICharacter>();
        character.FamiliarScript.Returns(familiarScript);
        character.EventManager.Returns(Substitute.For<IEventManager>());
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(context);

        var handlers = new Dictionary<int, OnComponentUsedOnCreature>();
        var widget = Substitute.For<IWidget>();
        widget.When(x => x.AttachUseOnCreatureHandler(Arg.Any<int>(), Arg.Any<OnComponentUsedOnCreature>()))
            .Do(call => handlers.Add(call.ArgAt<int>(0), call.ArgAt<OnComponentUsedOnCreature>(1)));

        var mediator = Substitute.For<IScopedGameMediator>();
        var leftClickOptionTab = new LeftClickOptionTab(contextAccessor, mediator);
        var orb = new SummoningOrb(contextAccessor, mediator, leftClickOptionTab);
        orb.Initialize(widget);
        orb.OnOpen();

        var target = Substitute.For<ICreature>();
        Assert.IsTrue(handlers[15](0, target, false, 0, 0));
        Assert.IsTrue(handlers[24](0, target, false, 0, 0));
        familiarNpc.Received(2).QueueTask(Arg.Any<ITaskItem>());
    }
}
