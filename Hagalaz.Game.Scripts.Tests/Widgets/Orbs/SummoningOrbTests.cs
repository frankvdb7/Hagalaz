using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Npcs.Familiars;
using Hagalaz.Game.Scripts.Widgets.Orbs;
using Hagalaz.Game.Scripts.Widgets.Tabs;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets.Orbs;

[TestClass]
public sealed class SummoningOrbTests
{
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
