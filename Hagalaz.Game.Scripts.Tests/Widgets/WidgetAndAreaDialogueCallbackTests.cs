using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Azzanadra;
using Hagalaz.Game.Scripts.Minigames.TzHaar;
using Hagalaz.Game.Scripts.Minigames.TzHaar.Cave.Dialogues;
using Hagalaz.Game.Scripts.Widgets.Warning;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets;

[TestClass]
public sealed class WidgetAndAreaDialogueCallbackTests
{
    [TestMethod]
    [DataRow(15, true)]
    [DataRow(16, false)]
    public void WarningButton_InvokesItsOwnActionBeforeClosing(int componentId, bool accepted)
    {
        var (character, accessor) = CreateCharacter();
        var widget = Substitute.For<IWidget>();
        OnComponentClick? click = null;
        widget.When(x => x.AttachClickHandler(componentId, Arg.Any<OnComponentClick>()))
            .Do(call => click = call.ArgAt<OnComponentClick>(1));
        var answers = new List<bool>();
        void RecordAnswer(bool answer)
        {
            character.Widgets.DidNotReceive().CloseWidget(widget);
            answers.Add(answer);
        }

        var script = new WarningInterfaceScript(accessor)
        {
            OnAcceptClicked = () => RecordAnswer(true),
            OnDeclineClicked = () => RecordAnswer(false),
        };
        script.Initialize(widget);
        script.OnOpen();
        Assert.IsNotNull(click);

        Assert.IsTrue(click(componentId, ComponentClickType.LeftClick, 41, 42));

        CollectionAssert.AreEqual(new[] { accepted }, answers);
        character.Widgets.Received(1).CloseWidget(widget);
    }

    [TestMethod]
    public void AzzanadraDialogue_NevermindClosesChatboxWithoutQueuingATeleport()
    {
        var (character, accessor) = CreateCharacter();
        var script = new AzzanadraDialogue(accessor);
        script.Initialize(Substitute.For<IWidget>());
        script.OnOpen();

        script.OnDialogueOptionClick("Nevermind", 41, 42);

        character.Widgets.Received(1).CloseChatboxOverlay();
        character.DidNotReceive().QueueTask(Arg.Any<Hagalaz.Game.Abstractions.Tasks.ITaskItem>());
    }

    [TestMethod]
    [DataRow(true, "You're on your own now, JalYt.<br>Prepare to fight for your life!<br>")]
    [DataRow(false, "Well I suppose you tried...<br>Better luck next time.<br>")]
    public void FightCaveDialogue_ContinuesAndClosesWithNonDefaultClickArguments(bool starting, string expectedText)
    {
        var (character, accessor) = CreateCharacter();
        var npcService = Substitute.For<INpcService>();
        var definition = Substitute.For<INpcDefinition>();
        npcService.FindNpcDefinitionById(TzHaarConstants.TzhaarMejJal).Returns(definition);
        var widget = Substitute.For<IWidget>();
        Hagalaz.Game.Scripts.Model.Widgets.DialogueScript script = starting
            ? new StartDialogue(accessor, npcService)
            : new QuitDialogue(accessor, npcService);
        script.Initialize(widget);
        script.OnOpen();

        Assert.IsTrue(script.OnDialogueContinueClick(0, 41, 42));
        widget.Received(1).DrawString(13, expectedText);
        character.Widgets.DidNotReceive().CloseChatboxOverlay();

        Assert.IsTrue(script.OnDialogueContinueClick(1, 43, 44));
        character.Widgets.Received(1).CloseChatboxOverlay();
    }

    private static (ICharacter Character, ICharacterContextAccessor Accessor) CreateCharacter()
    {
        var character = Substitute.For<ICharacter>();
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        return (character, accessor);
    }
}
