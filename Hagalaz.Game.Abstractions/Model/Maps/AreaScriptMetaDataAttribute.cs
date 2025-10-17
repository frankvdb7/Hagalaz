using System;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// An attribute used to associate an area script with one or more area IDs.
    /// This allows for automatic discovery and mapping of scripts to the map areas they handle.
    /// </summary>
    /// <param name="areaIds">An array of area IDs that this script applies to.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class AreaScriptMetaDataAttribute(int[] areaIds) : Attribute
    {
        /// <summary>
        /// Gets the array of area IDs that this script is associated with.
        /// </summary>
        public int[] AreaIds { get; } = areaIds;
    }
}