using System;

namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// 
    /// </summary>
    public interface IStateScriptProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        Type FindByType(StateType type);
    }
}