using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// 
    /// </summary>
    public interface INpcScriptProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="npcId"></param>
        /// <returns></returns>
        Type GetNpcScriptTypeById(int npcId);
    }
}