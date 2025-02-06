using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class CharacterNpcScriptMetaData(int[] npcIds) : Attribute
    {
        public int[] NpcIds { get; } = npcIds;
    }
}