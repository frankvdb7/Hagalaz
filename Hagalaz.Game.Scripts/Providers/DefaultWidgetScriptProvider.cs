using System;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Providers
{
    public class DefaultWidgetScriptProvider : IDefaultWidgetScriptProvider
    {
        public Type GetScriptType() => typeof(DefaultWidgetScript);
    }
}