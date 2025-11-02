using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Cache.Types.Factories;
using Hagalaz.Services.GameWorld.Data.Model;

namespace Hagalaz.Services.GameWorld.Factories
{
    /// <summary>
    /// 
    /// </summary>
    public class QuestDefinitionFactory : ITypeFactory<QuestDefinition>
    {
        /// <summary>
        /// Creates the type.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns></returns>
        public QuestDefinition CreateType(int typeId) => new(typeId);
    }
}