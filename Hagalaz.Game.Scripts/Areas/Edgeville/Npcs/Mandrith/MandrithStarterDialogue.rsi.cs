using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.CombatInstructor;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Mandrith
{
    /// <summary>
    /// </summary>
    public class MandrithStarterDialogue : NpcDialogueScript
    {
        public MandrithStarterDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose() => Owner.Movement.Unlock(false);

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            Owner.Movement.Lock(true);
            Setup();
        }

        /// <summary>
        ///     Setups this instance.
        /// </summary>
        private void Setup()
        {
            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Hello " + Owner.DisplayName + "!", "I am Mandrith and if it is within my power,", "I can teleport you to various places!");
                return true;
            });
            AttachDialogueContinueClickHandler(1, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "I can teleport you to the training area if you'd like.", "You are able to return to this place with", "the use of the home teleport, free of charge.");
                return true;
            });
            AttachDialogueContinueClickHandler(2, (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Yes.", "No.");
                return true;
            });
            AttachDialogueOptionClickHandler("Yes.", (extraData1, extraData2) =>
            {
                var allowWalkingEvent = Owner.RegisterEventHandler(new EventHappened<WalkAllowEvent>(e => true));
                new TrainingTeleport().PerformTeleport(Owner);
                Owner.QueueTask(new RsTask(() =>
                    {
                        var npc = Owner.Viewport.VisibleCreatures.OfType<INpc>().FirstOrDefault(n => n.Name.Equals("Combat instructor", StringComparison.OrdinalIgnoreCase));
                        if (npc == null)
                        {
                            return;
                        }
                        var task = new CreatureReachTask(Owner, npc, success =>
                        {
                            if (success)
                            {
                                var script = Owner.ServiceProvider.GetRequiredService<CombatInstructorDialogue>();
                                Owner.Widgets.OpenDialogue(script, false, npc);
                            }

                            Owner.UnregisterEventHandler<WalkAllowEvent>(allowWalkingEvent);
                        }, typeof(ICharacter)); // Prevent any interruption that would have prevented the task from executing.
                        Owner.QueueTask(task);
                    }, 6));
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("No.", (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
        }

        /// <summary>
        ///     Determines whether this instance can be interrupted.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance can be interrupted; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanInterrupt() => false;
    }
}