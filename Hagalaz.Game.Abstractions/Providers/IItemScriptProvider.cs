using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// 
    /// </summary>
    public interface IItemScriptProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        IItemScript FindItemScriptById(int itemId);
    }
}