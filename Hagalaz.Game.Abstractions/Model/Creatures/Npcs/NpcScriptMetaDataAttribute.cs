using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class NpcScriptMetaDataAttribute(int[] npcIds) : Attribute
    {
        public int[] NpcIds { get; } = npcIds;
    }
}
