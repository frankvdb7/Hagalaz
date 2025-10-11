using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// An attribute used to associate a character-NPC interaction script with one or more NPC IDs.
    /// This allows for automatic discovery and mapping of scripts to the NPCs they handle.
    /// </summary>
    /// <param name="npcIds">An array of NPC IDs that this script applies to.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class CharacterNpcScriptMetaData(int[] npcIds) : Attribute
    {
        /// <summary>
        /// Gets the array of NPC IDs that this script is associated with.
        /// </summary>
        public int[] NpcIds { get; } = npcIds;
    }
}