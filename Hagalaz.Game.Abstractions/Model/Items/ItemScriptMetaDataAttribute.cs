using System;

namespace Hagalaz.Game.Abstractions.Model.Items
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ItemScriptMetaDataAttribute(int[] itemIds) : Attribute
    {
        public int[] ItemIds { get; } = itemIds;
    }
}
