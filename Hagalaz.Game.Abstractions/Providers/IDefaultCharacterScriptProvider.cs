using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that supplies a collection of default scripts that should be attached to all characters.
    /// These scripts typically handle fundamental, universal character behaviors.
    /// </summary>
    public interface IDefaultCharacterScriptProvider
    {
        /// <summary>
        /// Retrieves an enumerable collection of all default character scripts.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="IDefaultCharacterScript"/> instances.</returns>
        IEnumerable<IDefaultCharacterScript> GetAllScripts();
    }
}