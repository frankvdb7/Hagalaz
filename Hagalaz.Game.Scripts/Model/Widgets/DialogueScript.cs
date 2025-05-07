using System.Collections.Generic;
using System.Text;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Resources;

namespace Hagalaz.Game.Scripts.Model.Widgets
{
    /// <summary>
    /// Base dialogue script class.
    /// </summary>
    public abstract class DialogueScript : WidgetScript, IDialogueScript
    {
        /// <summary>
        /// Contains handlers for dialogue component clicking.
        /// </summary>
        private readonly Dictionary<object, List<OnDialogueClick>> _dialogueClickHandlers = new();

        /// <summary>
        /// Contains the stage of the script the character was previously on.
        /// </summary>
        public int PreviousStage { get; private set; }

        /// <summary>
        /// Contains the stage of the script the character is currently on.
        /// </summary>
        public int Stage { get; private set; }

        public DialogueScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        /// Called when [dialogue continue click].
        /// </summary>
        public bool PerformDialogueContinueClick() => OnDialogueContinueClick(Stage, -1, -1);

        /// <summary>
        /// Called when [dialogue continue click].
        /// </summary>
        /// <param name="extraInfo1">The extra info1.</param>
        /// <param name="extraInfo2">The extra info2.</param>
        public bool PerformDialogueContinueClick(int extraInfo1, int extraInfo2) => OnDialogueContinueClick(Stage, extraInfo1, extraInfo2);

        /// <summary>
        /// Called when [dialogue continue click].
        /// </summary>
        /// <param name="stage">The stage.</param>
        /// <param name="extraInfo1">The extra info1.</param>
        /// <param name="extraInfo2">The extra info2.</param>
        public bool OnDialogueContinueClick(int stage, int extraInfo1, int extraInfo2)
        {
            if (!_dialogueClickHandlers.TryGetValue(stage, out var handler))
            {
                if (Owner.Permissions.HasAtLeastXPermission(Permission.GameAdministrator))
                    Owner.SendChatMessage("on_dialogue_continue_click[id=" + InterfaceInstance.Id + ",stage=" + Stage + "]", ChatMessageType.ConsoleText);
                return false;
            }

            foreach (var h in handler)
            {
                if (h.Invoke(extraInfo1, extraInfo2))
                {
                    NextStage();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Performs the dialogue option click.
        /// </summary>
        /// <param name="option">The option.</param>
        public void PerformDialogueOptionClick(string option) => OnDialogueOptionClick(option, -1, -1);

        /// <summary>
        /// Called when [dialogue option click].
        /// </summary>
        /// <param name="option">The option.</param>
        /// <param name="extraInfo1">The extra info1.</param>
        /// <param name="extraInfo2">The extra info2.</param>
        public void OnDialogueOptionClick(string option, int extraInfo1, int extraInfo2)
        {
            if (!_dialogueClickHandlers.TryGetValue(option, out var handler))
            {
                if (Owner.Permissions.HasAtLeastXPermission(Permission.GameAdministrator))
                    Owner.SendChatMessage("on_dialogue_option_click[id=" + InterfaceInstance.Id + ",option=" + option + "]", ChatMessageType.ConsoleText);
                return;
            }

            foreach (var h in handler)
            {
                if (h.Invoke(extraInfo1, extraInfo2))
                {
                    NextStage();
                    return;
                }
            }
        }

        /// <summary>
        /// Attaches the dialogue continue click handlers.
        /// </summary>
        public void AttachDialogueContinueClickHandlers()
        {
            bool ContinueClicked(int componentID, ComponentClickType clickType, int extraData1, int extraData2)
            {
                if (clickType == ComponentClickType.SpecialClick) return PerformDialogueContinueClick(extraData1, extraData2);
                return false;
            }

            for (var i = 0; i <= 18; i++)
            {
                InterfaceInstance.AttachClickHandler(i, ContinueClicked);
            }
        }

        /// <summary>
        /// Attaches the dialogue option click handler.
        /// </summary>
        /// <param name="stage">The stage.</param>
        /// <param name="handler">The handler.</param>
        public void AttachDialogueContinueClickHandler(int stage, OnDialogueClick handler)
        {
            if (_dialogueClickHandlers.TryGetValue(stage, out var clickHandler))
            {
                clickHandler.Add(handler);
            }
            else
            {
                var hList = new List<OnDialogueClick>
                {
                    handler
                };
                _dialogueClickHandlers.Add(stage, hList);
            }
        }

        /// <summary>
        /// Attaches the dialogue option click handler.
        /// </summary>
        /// <param name="option">The option.</param>
        /// <param name="handler">The handler.</param>
        public void AttachDialogueOptionClickHandler(string option, OnDialogueClick handler)
        {
            if (_dialogueClickHandlers.TryGetValue(option, out var clickHandler))
            {
                clickHandler.Add(handler);
            }
            else
            {
                var hList = new List<OnDialogueClick>
                {
                    handler
                };
                _dialogueClickHandlers.Add(option, hList);
            }
        }

        /// <summary>
        /// Standarts the dialogue.
        /// </summary>
        /// <param name="texts">The texts.</param>
        public void StandardDialogue(params string[] texts)
        {
            Owner.Widgets.SetWidgetId(InterfaceInstance, (int)DialogueInterfaces.StandardDialogue);
            var builder = new StringBuilder();
            foreach (var text in texts)
            {
                builder.Append(text);
                builder.Append("<br>");
            }

            InterfaceInstance.DrawString(1, builder.ToString());
        }

        /// <summary>
        /// Standarts the NPC dialogue.
        /// </summary>
        /// <param name="npc">The NPC.</param>
        /// <param name="animation">The animation.</param>
        /// <param name="texts">The texts.</param>
        public void StandardNpcDialogue(INpc npc, DialogueAnimations animation, params string[] texts) => StandardNpcDialogue(npc.Definition, animation, texts);

        /// <summary>
        /// Standarts the NPC dialogue.
        /// </summary>
        /// <param name="definition">The definition.</param>
        /// <param name="animation">The animation.</param>
        /// <param name="texts">The texts.</param>
        public void StandardNpcDialogue(INpcDefinition definition, DialogueAnimations animation, params string[] texts) =>
            StandardNpcDialogue(definition.Id, definition.DisplayName, animation, texts);

        /// <summary>
        /// Standarts the NPC dialogue.
        /// </summary>
        /// <param name="compositeID">The composite identifier.</param>
        /// <param name="title">The title.</param>
        /// <param name="animation">The animation.</param>
        /// <param name="texts">The texts.</param>
        public void StandardNpcDialogue(int compositeID, string title, DialogueAnimations animation, params string[] texts)
        {
            Owner.Widgets.SetWidgetId(InterfaceInstance, (int)DialogueInterfaces.StandardNpcDialogue);
            var builder = new StringBuilder();
            foreach (var text in texts)
            {
                builder.Append(text);
                builder.Append("<br>");
            }

            InterfaceInstance.DrawString(17, title);
            InterfaceInstance.DrawString(13, builder.ToString());
            InterfaceInstance.DrawNpcHead(11, compositeID);
            InterfaceInstance.SetAnimation(11, (int)animation);
        }

        /// <summary>
        /// A npc - character dialogue where the npc is talking and with 1 or more text lines.
        /// </summary>
        /// <param name="toSwitchTo">To switch to.</param>
        /// <param name="npc">The NPC.</param>
        /// <param name="animation">The animation.</param>
        /// <param name="text1">The text1.</param>
        /// <param name="text2">The text2.</param>
        /// <param name="text3">The text3.</param>
        /// <param name="text4">The text4.</param>
        public void NpcDialogue(
            DialogueInterfaces toSwitchTo, INpc npc, DialogueAnimations animation, string text1, string? text2 = null, string? text3 = null,
            string? text4 = null) =>
            NpcDialogue(toSwitchTo, npc.Appearance.CompositeID, animation, npc.Name, text1, text2, text3, text4);

        /// <summary>
        /// A npc - character dialogue where the npc is talking and with 1 or more text lines.
        /// </summary>
        /// <param name="toSwitchTo">To switch to.</param>
        /// <param name="compositeID">The composite Id.</param>
        /// <param name="animation">The animation.</param>
        /// <param name="title">The title.</param>
        /// <param name="text1">The text1.</param>
        /// <param name="text2">The text2.</param>
        /// <param name="text3">The text3.</param>
        /// <param name="text4">The text4.</param>
        public void NpcDialogue(
            DialogueInterfaces toSwitchTo, int compositeID, DialogueAnimations animation, string title, string text1, string? text2 = null,
            string? text3 = null, string? text4 = null)
        {
            Owner.Widgets.SetWidgetId(InterfaceInstance, (int)toSwitchTo);
            InterfaceInstance.DrawString(3, title);
            InterfaceInstance.DrawString(4, text1);
            if (text2 != null) InterfaceInstance.DrawString(5, text2);
            if (text3 != null) InterfaceInstance.DrawString(6, text3);
            if (text4 != null) InterfaceInstance.DrawString(7, text4);
            InterfaceInstance.SetAnimation(2, (int)animation);
            InterfaceInstance.DrawNpcHead(2, compositeID);
        }

        /// <summary>
        /// Standarts the character dialogue.
        /// </summary>
        /// <param name="animation">The animation.</param>
        /// <param name="texts">The texts.</param>
        public void DefaultCharacterDialogue(DialogueAnimations animation, params string[] texts) =>
            StandardCharacterDialogue(animation, Owner.DisplayName, texts);

        /// <summary>
        /// Standarts the character dialogue.
        /// </summary>
        /// <param name="animation">The animation.</param>
        /// <param name="title">The title.</param>
        /// <param name="texts">The texts.</param>
        public void StandardCharacterDialogue(DialogueAnimations animation, string title, params string[] texts)
        {
            Owner.Widgets.SetWidgetId(InterfaceInstance, (int)DialogueInterfaces.StandardCharacterDialogue);
            var builder = new StringBuilder();
            foreach (var text in texts)
            {
                builder.Append(text);
                builder.Append("<br>"); // new line
            }

            InterfaceInstance.DrawString(8, title);
            InterfaceInstance.DrawString(17, builder.ToString());
            InterfaceInstance.DrawCharacterHead(15);
            InterfaceInstance.SetAnimation(15, (int)animation);
        }

        /// <summary>
        /// A character - npc dialogue where the player is talking and with 1 or more text lines.
        /// </summary>
        /// <param name="toSwitchTo">To switch to.</param>
        /// <param name="animation">The animation.</param>
        /// <param name="text1">The text1.</param>
        /// <param name="text2">The text2.</param>
        /// <param name="text3">The text3.</param>
        /// <param name="text4">The text4.</param>
        public void CharacterDialogue(
            DialogueInterfaces toSwitchTo, DialogueAnimations animation, string text1, string? text2 = null, string? text3 = null, string? text4 = null)
        {
            Owner.Widgets.SetWidgetId(InterfaceInstance, (int)toSwitchTo);
            InterfaceInstance.DrawString(3, Owner.Name);
            InterfaceInstance.DrawString(4, text1);
            if (text2 != null) InterfaceInstance.DrawString(5, text2);
            if (text3 != null) InterfaceInstance.DrawString(6, text3);
            if (text4 != null) InterfaceInstance.DrawString(7, text4);
            InterfaceInstance.SetAnimation(2, (int)animation);
            InterfaceInstance.DrawCharacterHead(2);
        }

        /// <summary>
        /// Standarts the option dialogue.
        /// </summary>
        /// <param name="options">The options.</param>
        public void DefaultOptionDialogue(params string[] options) => StandardOptionDialogue(GameStrings.SelectAnOption, options);

        /// <summary>
        /// Standarts the option dialogue.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="options">The options.</param>
        public void StandardOptionDialogue(string title, params string[] options)
        {
            Owner.Widgets.SetWidgetId(InterfaceInstance, (int)DialogueInterfaces.StandardOptionsDialogue);
            InterfaceInstance.DrawString(20, title);
            var length = options.Length;
            if (length > 5) length = 5;
            var parameters = new object[length + 1];
            parameters[0] = length;
            for (var i = 0; i < length; i++)
            {
                var option = options[i];
                var childID = (short)(i + 11);
                if (i > 0) childID += 1;
                InterfaceInstance.DetachClickHandlers(childID);
                InterfaceInstance.AttachClickHandler(childID,
                    (componentID, clickType, extraData1, extraData2) =>
                    {
                        if (clickType == ComponentClickType.SpecialClick)
                        {
                            OnDialogueOptionClick(option, extraData1, extraData2);
                            return true;
                        }

                        return false;
                    });

                parameters[i + 1] = option;
            }

            Owner.Configurations.SendCs2Script(5589, parameters);
        }

        /// <summary>
        /// A dialogue where the character can select various options.
        /// </summary>
        /// <param name="toSwitchTo">To switch to.</param>
        /// <param name="title">The title.</param>
        /// <param name="option1">The option1.</param>
        /// <param name="option2">The option2.</param>
        /// <param name="option3">The option3.</param>
        /// <param name="option4">The option4.</param>
        /// <param name="option5">The option5.</param>
        public void OptionDialogue(
            DialogueInterfaces toSwitchTo, string title, string option1, string? option2 = null, string? option3 = null, string? option4 = null,
            string? option5 = null)
        {
            Owner.Widgets.SetWidgetId(InterfaceInstance, (int)toSwitchTo);
            InterfaceInstance.DrawString(1, title);
            InterfaceInstance.DetachClickHandlers(1);
            InterfaceInstance.AttachClickHandler(1,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType == ComponentClickType.SpecialClick)
                    {
                        OnDialogueOptionClick(title, extraData1, extraData2);
                        return true;
                    }

                    return false;
                });
            InterfaceInstance.DrawString(2, option1);
            InterfaceInstance.DetachClickHandlers(2);
            InterfaceInstance.AttachClickHandler(2,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType == ComponentClickType.SpecialClick)
                    {
                        OnDialogueOptionClick(option1, extraData1, extraData2);
                        return true;
                    }

                    return false;
                });
            if (option2 != null)
            {
                InterfaceInstance.DrawString(3, option2);
                InterfaceInstance.DetachClickHandlers(3);
                InterfaceInstance.AttachClickHandler(3,
                    (componentID, clickType, extraData1, extraData2) =>
                    {
                        if (clickType == ComponentClickType.SpecialClick)
                        {
                            OnDialogueOptionClick(option2, extraData1, extraData2);
                            return true;
                        }

                        return false;
                    });
            }

            if (option3 != null)
            {
                InterfaceInstance.DrawString(4, option3);
                InterfaceInstance.DetachClickHandlers(4);
                InterfaceInstance.AttachClickHandler(4,
                    (componentID, clickType, extraData1, extraData2) =>
                    {
                        if (clickType == ComponentClickType.SpecialClick)
                        {
                            OnDialogueOptionClick(option3, extraData1, extraData2);
                            return true;
                        }

                        return false;
                    });
            }

            if (option4 != null)
            {
                InterfaceInstance.DrawString(5, option4);
                InterfaceInstance.DetachClickHandlers(5);
                InterfaceInstance.AttachClickHandler(5,
                    (componentID, clickType, extraData1, extraData2) =>
                    {
                        if (clickType == ComponentClickType.SpecialClick)
                        {
                            OnDialogueOptionClick(option4, extraData1, extraData2);
                            return true;
                        }

                        return false;
                    });
            }

            if (option5 != null)
            {
                InterfaceInstance.DrawString(6, option5);
                InterfaceInstance.DetachClickHandlers(6);
                InterfaceInstance.AttachClickHandler(6,
                    (componentID, clickType, extraData1, extraData2) =>
                    {
                        if (clickType == ComponentClickType.SpecialClick)
                        {
                            OnDialogueOptionClick(option5, extraData1, extraData2);
                            return true;
                        }

                        return false;
                    });
            }
        }

        /// <summary>
        /// Sends an info dialogue to the character.
        /// </summary>
        /// <param name="toSwitchTo">To switch to.</param>
        /// <param name="title">The title.</param>
        /// <param name="text1">The text1.</param>
        /// <param name="text2">The text2.</param>
        /// <param name="text3">The text3.</param>
        /// <param name="text4">The text4.</param>
        /// <param name="text5">The text5.</param>
        public void InfoDialogue(
            DialogueInterfaces toSwitchTo, string title, string? text1 = null, string? text2 = null, string? text3 = null, string? text4 = null,
            string? text5 = null)
        {
            Owner.Widgets.SetWidgetId(InterfaceInstance, (int)toSwitchTo);
            InterfaceInstance.DrawString(1, title);
            if (text1 != null) InterfaceInstance.DrawString(2, text1);
            if (text2 != null) InterfaceInstance.DrawString(3, text2);
            if (text3 != null) InterfaceInstance.DrawString(4, text3);
            if (text4 != null) InterfaceInstance.DrawString(5, text4);
            if (text5 != null) InterfaceInstance.DrawString(6, text5);
        }

        /// <summary>
        /// Happens when interface has been opened for character.
        /// By default, this method attachs continue click handlers and process the dialogue to stage 0.
        /// </summary>
        public override void OnOpened()
        {
            AttachDialogueContinueClickHandlers();
            PerformDialogueContinueClick();
        }

        /// <summary>
        /// Sets the stage.
        /// </summary>
        /// <param name="stage">The stage.</param>
        public void SetStage(short stage)
        {
            PreviousStage = Stage;
            Stage = stage;
        }

        /// <summary>
        /// Processes the script to the next stage.
        /// </summary>
        public void NextStage()
        {
            PreviousStage = Stage;
            Stage++;
        }

        /// <summary>
        /// Sets the source that the character interacted with.
        /// </summary>
        public virtual void SetSource(IRuneObject? source) { }
    }
}