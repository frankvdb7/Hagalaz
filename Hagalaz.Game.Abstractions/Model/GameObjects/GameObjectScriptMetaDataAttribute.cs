using System;

namespace Hagalaz.Game.Abstractions.Model.GameObjects
{
    /// <summary>
    /// An attribute used to associate a game object script with one or more object IDs.
    /// This allows for automatic discovery and mapping of scripts to the objects they handle.
    /// </summary>
    /// <param name="objectIds">An array of object IDs that this script applies to.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class GameObjectScriptMetaDataAttribute(int[] objectIds) : Attribute
    {
        /// <summary>
        /// Gets the array of object IDs that this script is associated with.
        /// </summary>
        public int[] ObjectIds { get; } = objectIds;
    }
}