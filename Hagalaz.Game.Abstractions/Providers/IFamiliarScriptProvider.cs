using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// 
    /// </summary>
    public interface IFamiliarScriptProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="npcId"></param>
        /// <returns></returns>
        Type FindFamiliarScriptTypeById(int npcId);
    }
}