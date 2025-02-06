using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICharacterNpcScriptProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="npcId"></param>
        /// <returns></returns>
        Type GetCharacterNpcScriptTypeById(int npcId);
    }
}