using System;

namespace Hagalaz.Game.Abstractions.Model.Widgets
{
    /// <summary>
    /// An attribute used to associate a widget script with one or more widget IDs.
    /// This allows for automatic discovery and mapping of scripts to the widgets they handle.
    /// </summary>
    /// <param name="widgetIds">An array of widget IDs that this script applies to.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class WidgetScriptMetaData(int[] widgetIds) : Attribute
    {
        /// <summary>
        /// Gets the array of widget IDs that this script is associated with.
        /// </summary>
        public int[] WidgetIds { get; } = widgetIds;
    }
}