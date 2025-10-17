using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that supplies a default script for items.
    /// This script is used for any item that does not have a specific, custom script assigned to it.
    /// </summary>
    public interface IDefaultItemScriptProvider
    {
        /// <summary>
        /// Gets the <see cref="Type"/> of the default item script.
        /// </summary>
        /// <returns>The <see cref="Type"/> of the default script implementation.</returns>
        Type GetScriptType();
    }
}