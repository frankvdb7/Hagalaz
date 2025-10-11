using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Defines a contract for a factory that discovers and provides widget scripts.
    /// These scripts define the behavior for user interface components (widgets).
    /// </summary>
    public interface IWidgetScriptFactory
    {
        /// <summary>
        /// Retrieves an asynchronous enumerable of all discovered widget scripts, pairing each widget ID with its corresponding script type.
        /// </summary>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of tuples, where each tuple contains a widget ID and the <see cref="Type"/> of the <see cref="IWidgetScript"/> that handles it.</returns>
        IAsyncEnumerable<(int widgetId, Type scriptType)> GetScripts();
    }
}