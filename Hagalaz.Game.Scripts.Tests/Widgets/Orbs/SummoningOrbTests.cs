using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Npcs.Familiars;
using Hagalaz.Game.Scripts.Widgets.Orbs;
using Hagalaz.Game.Scripts.Widgets.Tabs;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets.Orbs;

[TestClass]
public sealed class SummoningOrbTests
{
    [TestMethod]
    [DataRow(15)]
    [DataRow(24)]
    [DataRow(65)]
    public void AttackHandlers_RejectSelfAndQueueTheTargetForTheNextTick(int component)
    {
        var familiar = Substitute.For<INpc>();
        var familiarScript = Substitute.For<IFamiliarScript>();
        familiarScript.Familiar.Returns(familiar);
        var character = Substitute.For<ICharacter>();
        character.FamiliarScript.Returns(familiarScript);
        character.EventManager.Returns(Substitute.For<IEventManager>());
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        var mediator = Substitute.For<IScopedGameMediator>();
        var widget = Substitute.For<IWidget>();
        OnComponentUsedOnCreature? attack = null;
        widget.When(x => x.AttachUseOnCreatureHandler(component, Arg.Any<OnComponentUsedOnCreature>()))
            .Do(call => attack = call.ArgAt<OnComponentUsedOnCreature>(1));
        IWidgetScript script = component == 65
            ? new SummoningTab(accessor)
            : new SummoningOrb(accessor, mediator, new LeftClickOptionTab(accessor, mediator));
        script.Initialize(widget);
        script.OnOpen();
        Assert.IsNotNull(attack);

        Assert.IsFalse(attack(component, familiar, true, 41, 42));
        familiar.DidNotReceive().QueueTask(Arg.Any<ITaskItem>());

        var target = Substitute.For<ICreature>();
        Assert.IsTrue(attack(component, target, true, 41, 42));
        familiar.Received(1).QueueTask(Arg.Is<ITaskItem>(task => task is RsTask && ((RsTask)task).ExecuteDelay == 1));
    }

    [TestMethod]
    public void InteractHandlers_OpenTheFamiliarDialogueForBothClickOptions()
    {
        var familiarNpc = Substitute.For<INpc>();
        var familiarScript = Substitute.For<IFamiliarScript>();
        familiarScript.Familiar.Returns(familiarNpc);

        var character = Substitute.For<ICharacter>();
        character.FamiliarScript.Returns(familiarScript);
        character.EventManager.Returns(Substitute.For<IEventManager>());
        familiarScript.Summoner.Returns(character);
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(context);
        var dialogue = new StandardFamiliarDialogue(contextAccessor);
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(StandardFamiliarDialogue)).Returns(dialogue);
        character.ServiceProvider.Returns(serviceProvider);
        var widgets = Substitute.For<IWidgetContainer>();
        character.Widgets.Returns(widgets);

        var handlers = new Dictionary<int, OnComponentClick>();
        var widget = Substitute.For<IWidget>();
        widget.When(x => x.AttachClickHandler(Arg.Any<int>(), Arg.Any<OnComponentClick>()))
            .Do(call => handlers.Add(call.ArgAt<int>(0), call.ArgAt<OnComponentClick>(1)));

        var mediator = Substitute.For<IScopedGameMediator>();
        var leftClickOptionTab = new LeftClickOptionTab(contextAccessor, mediator);
        var orb = new SummoningOrb(contextAccessor, mediator, leftClickOptionTab);
        orb.Initialize(widget);
        orb.OnOpen();

        Assert.IsTrue(handlers[16](16, ComponentClickType.Option6Click, 0, 0));
        Assert.IsTrue(handlers[27](27, ComponentClickType.LeftClick, 0, 0));
        widgets.Received(2).OpenChatboxOverlay((short)DialogueInterfaces.Send2TextChatRight, 0, dialogue, true, familiarNpc);
    }
}
