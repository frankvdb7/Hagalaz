using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Providers;

namespace Hagalaz.Game.Scripts.Model.Widgets
{
    /// <summary>
    /// NPC dialogue script.
    /// </summary>
    public abstract class NpcDialogueScript : DialogueScript
    {
        /// <summary>
        /// The NPC the character is talking to.
        /// </summary>
        protected INpc TalkingTo = default!;

        public NpcDialogueScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        /// Happens when dialogue is opened for character.
        /// </summary>
        public sealed override void OnOpened()
        {
            base.OnOpened();
            Owner.FaceLocation(TalkingTo);
            TalkingTo.FaceLocation(Owner);
        }

        /// <summary>
        /// Happens when dialogue is closed for character.
        /// </summary>
        public sealed override void OnClosed()
        {
            base.OnClosed();
            TalkingTo.ResetFacing();
        }

        /// <summary>
        /// Sets the source.
        /// </summary>
        /// <param name="source">The source.</param>
        public override void SetSource(IRuneObject? source)
        {
            if (source is not INpc npc)
                throw new Exception("Source is not an npc!");
            TalkingTo = npc;
        }
    }
}