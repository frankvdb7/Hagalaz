using Hagalaz.Cache.Abstractions.Types.Factories;

namespace Hagalaz.Cache.Types.Factories
{
    /// <summary>
    /// 
    /// </summary>
    public class NpcTypeFactory : ITypeFactory<NpcType>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
        public NpcType CreateType(int typeId) => new(typeId);
    }
}