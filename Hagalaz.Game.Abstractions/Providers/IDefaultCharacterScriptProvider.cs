using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDefaultCharacterScriptProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerable<IDefaultCharacterScript> GetAllScripts();
    }
}