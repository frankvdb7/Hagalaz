using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that supplies a default script for character states.
    /// This script is used for any state that does not have a specific, custom script assigned to it.
    /// </summary>
    public interface IDefaultStateScriptProvider
    {
        /// <summary>
        /// Gets the <see cref="Type"/> of the default state script.
        /// </summary>
        /// <returns>The <see cref="Type"/> of the default script implementation.</returns>
        Type GetScriptType();
    }
}
