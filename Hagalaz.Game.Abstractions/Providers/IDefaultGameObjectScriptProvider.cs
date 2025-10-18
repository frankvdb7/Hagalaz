using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that supplies a default script for game objects.
    /// This script is used for any game object that does not have a specific, custom script assigned to it.
    /// </summary>
    public interface IDefaultGameObjectScriptProvider
    {
        /// <summary>
        /// Gets the <see cref="Type"/> of the default game object script.
        /// </summary>
        /// <returns>The <see cref="Type"/> of the default script implementation.</returns>
        Type GetScriptType();
    }
}