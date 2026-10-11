using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Dialogues.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests;

[TestClass]
public sealed class YesNoDialogueScriptTests
{
    [TestMethod]
    public void YesClick_ConsumesCallbackBeforeInvokingAndIgnoresRepeatedClicks()
    {
        var dialogue = CreateDialogue();
        var callbackCount = 0;
        var callbackWasClearedBeforeInvoke = false;
        dialogue.Callback = _ =>
        {
            callbackWasClearedBeforeInvoke = dialogue.Callback == null;
            callbackCount++;
        };
        dialogue.OnOpen();

        dialogue.OnDialogueOptionClick("Yes.", -1, -1);
        dialogue.OnDialogueOptionClick("Yes.", -1, -1);

        Assert.IsTrue(callbackWasClearedBeforeInvoke);
        Assert.AreEqual(1, callbackCount);
        Assert.IsNull(dialogue.Callback);
    }

    [TestMethod]
    public void NoClick_InvokesCancellationOnceAndIgnoresLaterYesClick()
    {
        var dialogue = CreateDialogue();
        bool? answer = null;
        dialogue.Callback = accepted => answer = accepted;
        dialogue.OnOpen();

        dialogue.OnDialogueOptionClick("No.", -1, -1);
        dialogue.OnDialogueOptionClick("Yes.", -1, -1);

        Assert.AreEqual(false, answer);
        Assert.IsNull(dialogue.Callback);
    }

    [TestMethod]
    public void ClosedDialogue_ClearsOldCallbackAndCanBeReused()
    {
        var dialogue = CreateDialogue();
        var oldCallbackCount = 0;
        var newCallbackCount = 0;
        dialogue.Callback = _ => oldCallbackCount++;
        dialogue.OnOpen();

        dialogue.OnClose();
        dialogue.OnDialogueOptionClick("Yes.", -1, -1);
        Assert.AreEqual(0, oldCallbackCount);
        Assert.IsNull(dialogue.Callback);

        dialogue.Callback = _ => newCallbackCount++;
        dialogue.OnOpen();
        dialogue.OnDialogueOptionClick("Yes.", -1, -1);

        Assert.AreEqual(0, oldCallbackCount);
        Assert.AreEqual(1, newCallbackCount);
        Assert.IsNull(dialogue.Callback);
    }

    private static YesNoDialogueScript CreateDialogue()
    {
        var character = Substitute.For<ICharacter>();
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(context);
        character.Widgets.Returns(Substitute.For<IWidgetContainer>());
        character.Configurations.Returns(Substitute.For<IConfigurations>());

        var dialogue = new YesNoDialogueScript(contextAccessor)
        {
            Question = "Continue?"
        };
        dialogue.Initialize(Substitute.For<IWidget>());
        return dialogue;
    }
}
