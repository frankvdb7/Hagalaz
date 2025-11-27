using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Services.GameWorld.Data.Model;

namespace Hagalaz.Services.GameWorld.Factories
{
    /// <summary>
    /// 
    /// </summary>
    public class NpcDefinitionFactory : ITypeFactory<NpcDefinition>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
        public NpcDefinition CreateType(int typeId) => new(typeId);
    }
}