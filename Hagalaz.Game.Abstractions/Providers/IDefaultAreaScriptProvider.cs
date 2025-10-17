using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that supplies a default area script.
    /// This script is used for any map area that does not have a specific, custom script assigned to it.
    /// </summary>
    public interface IDefaultAreaScriptProvider
    {
        /// <summary>
        /// Gets the <see cref="Type"/> of the default area script.
        /// </summary>
        /// <returns>The <see cref="Type"/> of the default script implementation.</returns>
        Type GetScriptType();
    }
}