using System;

namespace Hagalaz.Game.Abstractions.Model.GameObjects
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class GameObjectScriptMetaDataAttribute(int[] objectIds) : Attribute
    {
        public int[] ObjectIds { get; } = objectIds;
    }
}
