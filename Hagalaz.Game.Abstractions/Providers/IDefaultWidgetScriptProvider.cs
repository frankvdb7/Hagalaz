using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that supplies a default script for widgets (UI components).
    /// This script is used for any widget that does not have a specific, custom script assigned to it.
    /// </summary>
    public interface IDefaultWidgetScriptProvider
    {
        /// <summary>
        /// Gets the <see cref="Type"/> of the default widget script.
        /// </summary>
        /// <returns>The <see cref="Type"/> of the default script implementation.</returns>
        Type GetScriptType();
    }
}