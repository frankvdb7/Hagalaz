using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Mandrith;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Sage
{
    /// <summary>
    /// </summary>
    public class SageStarterDialogue : NpcDialogueScript
    {
        public SageStarterDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose() => Owner.Movement.Unlock(false);

        /// <summary>
        ///     Called when [dialogue open].
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
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Welcome to Hagalaz " + Owner.DisplayName + "!");
                return true;
            });
            AttachDialogueContinueClickHandler(1, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "I will provide you with some basic information that will get you started!");
                return true;
            });
            AttachDialogueContinueClickHandler(2, (extraData1, extraData2) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "Sure, I would like to hear what you want to say!");
                return true;
            });
            AttachDialogueContinueClickHandler(3, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "You can buy items and equipment from the shopkeepers in the north-west of Edgeville.");
                return true;
            });
            AttachDialogueContinueClickHandler(4, (extraData1, extraData2) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "Where can I train myself to become even stronger?");
                return true;
            });
            AttachDialogueContinueClickHandler(5, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "If you want to become stronger and earn some gold, than you can talk to Mandrith.", "He is able to teleport you to the training area for example.");
                return true;
            });
            AttachDialogueContinueClickHandler(6, (extraData1, extraData2) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "And where can I earn some serious cash or high level equipment?");
                return true;
            });
            AttachDialogueContinueClickHandler(7, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "I heard that experienced adventurers made some gold by training their skills and selling the items they made, earned or looted, to other adventurers like you.");
                return true;
            });
            AttachDialogueContinueClickHandler(8, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "You can also sell the items you gathered to the General Store, the shop is located in the north west of Edgeville.");
                return true;
            });
            AttachDialogueContinueClickHandler(9, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "You can also travel to various dangerous places like GodWars and the Forinthry Dungeon.", "The monsters there often drop valuable loot.");
                return true;
            });
            AttachDialogueContinueClickHandler(10, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "If you need more information about a certain skill, than feel free to ask the skill masters who wander in Edgeville.");
                return true;
            });
            AttachDialogueContinueClickHandler(11, (extraData1, extraData2) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.Laughing, "Sweet! I am anxious to get started!");
                return true;
            });
            AttachDialogueContinueClickHandler(12, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "That is great news!", "If you have any questions or if you need assistance, than try asking me or other players for help.");
                return true;
            });
            AttachDialogueContinueClickHandler(13, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "I am looking forward to hearing from you again!", "Have a great time playing Hagalaz!");
                return true;
            });
            AttachDialogueContinueClickHandler(14, (extraData1, extraData2) =>
            {
                var allowWalkingEvent = Owner.RegisterEventHandler(new EventHappened<WalkAllowEvent>(e => true));
                var npc = Owner.Viewport.VisibleCreatures.OfType<INpc>().FirstOrDefault(n => n.Name.Equals("Mandrith", StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    //this.owner.Session.SendPacket(new SetCameraPositionPacketComposer())
                    var task = new CreatureReachTask(Owner, npc, success =>
                    {
                        if (success)
                        {
                            var dialogue = Owner.ServiceProvider.GetRequiredService<MandrithStarterDialogue>();
                            Owner.Widgets.OpenDialogue(dialogue, false, npc);
                        }

                        Owner.UnregisterEventHandler<WalkAllowEvent>(allowWalkingEvent); // allow walking again
                    }, typeof(ICharacter)); // Prevent any interruption that would have prevented the task from executing.
                    Owner.QueueTask(task);
                }

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