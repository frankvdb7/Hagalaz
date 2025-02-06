using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// 
    /// </summary>
    public interface IAreaScriptProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="areaId"></param>
        /// <returns></returns>
        IAreaScript FindAreaScriptById(int areaId);
    }
}