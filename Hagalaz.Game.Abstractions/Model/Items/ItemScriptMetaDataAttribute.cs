using System;

namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// An attribute used to associate an item script with one or more item IDs.
    /// This allows for automatic discovery and mapping of scripts to the items they handle.
    /// </summary>
    /// <param name="itemIds">An array of item IDs that this script applies to.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ItemScriptMetaDataAttribute(int[] itemIds) : Attribute
    {
        /// <summary>
        /// Gets the array of item IDs that this script is associated with.
        /// </summary>
        public int[] ItemIds { get; } = itemIds;
    }
}