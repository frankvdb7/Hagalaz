using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;

namespace Hagalaz.Game.Scripts.Model.Widgets
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class ItemDialogueScript : DialogueScript
    {
        /// <summary>
        /// The item.
        /// </summary>
        protected IItem Item = default!;

        public ItemDialogueScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        /// Happens when interface has been opened for character.
        /// </summary>
        public sealed override void OnOpened() => base.OnOpened();

        /// <summary>
        /// Happens when interface has been closed for character.
        /// </summary>
        public sealed override void OnClosed() => base.OnClosed();

        /// <summary>
        /// Sets the source.
        /// </summary>
        /// <param name="source">The source.</param>
        public override void SetSource(IRuneObject? source)
        {
            if (source is not IItem item)
                throw new Exception("Source is not an item!");
            Item = item;
        }
    }
}