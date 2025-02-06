using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// 
    /// </summary>
    public interface IGameObjectScriptProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectId"></param>
        /// <returns></returns>
        Type GetGameObjectScriptTypeById(int objectId);
    }
}