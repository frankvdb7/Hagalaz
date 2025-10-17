using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that supplies a default script for equipment items.
    /// This script is used for any piece of equipment that does not have a specific, custom script assigned to it.
    /// </summary>
    public interface IDefaultEquipmentScriptProvider
    {
        /// <summary>
        /// Gets the <see cref="Type"/> of the default equipment script.
        /// </summary>
        /// <returns>The <see cref="Type"/> of the default script implementation.</returns>
        Type GetScriptType();
    }
}