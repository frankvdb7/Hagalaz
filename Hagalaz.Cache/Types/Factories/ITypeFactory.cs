using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types.Factories
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITypeFactory<out T>
    {
        /// <summary>
        /// Creates the type.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns></returns>
        T CreateType(int typeId);
    }
}