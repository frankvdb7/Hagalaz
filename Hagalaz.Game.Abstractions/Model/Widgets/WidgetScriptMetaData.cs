using System;

namespace Hagalaz.Game.Abstractions.Model.Widgets
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class WidgetScriptMetaData(int[] widgetIds) : Attribute
    {
        public int[] WidgetIds { get; } = widgetIds;
    }
}