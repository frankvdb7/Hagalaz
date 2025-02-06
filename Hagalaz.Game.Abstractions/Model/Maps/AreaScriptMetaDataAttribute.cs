using System;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class AreaScriptMetaDataAttribute(int[] areaIds) : Attribute
    {
        public int[] AreaIds { get; } = areaIds;
    }
}