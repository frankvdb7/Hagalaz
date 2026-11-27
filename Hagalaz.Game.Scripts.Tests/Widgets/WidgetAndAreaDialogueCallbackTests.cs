using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Azzanadra;
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
