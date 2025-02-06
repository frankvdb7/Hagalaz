using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// 
    /// </summary>
    public interface IWidgetScriptProvider
    {
        /// <summary>
        /// Finds the script type by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Type FindScriptTypeById(int id);

        /// <summary>
        /// Gets the interfaces count.
        /// </summary>
        /// <returns></returns>
        int GetInterfacesCount();
    }
}