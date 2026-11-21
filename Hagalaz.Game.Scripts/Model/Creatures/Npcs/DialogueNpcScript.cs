using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Scripts.Model.Creatures.Npcs
{
    /// <summary>
    ///     An NPC that talks: clicking it with Option 1 opens a <typeparamref name="TDialogue"/>. Other clicks behave as for
    ///     any NPC.
    /// </summary>
    /// <typeparam name="TDialogue">The dialogue the NPC opens.</typeparam>
    /// <param name="interrupt">Whether opening the dialogue interrupts what the character is doing.</param>
    public abstract class DialogueNpcScript<TDialogue>(
        INpc owner,
        INpcService npcService,
        IPathFinder pathFinder,
        IWidgetScriptActivator widgetScriptActivator,
        bool interrupt = true) : NpcScriptBase(owner, npcService, pathFinder, widgetScriptActivator)
        where TDialogue : class, IWidgetScript, IDialogueScript
    {
        /// <summary>
        ///     Prepares a newly created dialogue before it opens. Does nothing by default.
        /// </summary>
        protected virtual void Configure(TDialogue dialogue)
        {
        }

        /// <inheritdoc />
        public override void OnCharacterClickPerform(ICharacter clicker, NpcClickType clickType)
        {
            if (clickType == NpcClickType.Option1Click)
            {
                var dialogue = CreateWidgetScript<TDialogue>(clicker);
                Configure(dialogue);
                clicker.Widgets.OpenDialogue(dialogue, interrupt, Owner);
            }
            else
            {
                base.OnCharacterClickPerform(clicker, clickType);
            }
        }
    }
}
