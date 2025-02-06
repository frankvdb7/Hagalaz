using System;
using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Factories
{
    public interface IWidgetScriptFactory
    {
        IAsyncEnumerable<(int widgetId, Type scriptType)> GetScripts();
    }
}