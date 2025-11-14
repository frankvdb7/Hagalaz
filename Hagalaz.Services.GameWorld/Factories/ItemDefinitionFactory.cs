using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Services.GameWorld.Data.Model;

namespace Hagalaz.Services.GameWorld.Factories
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="ITypeFactory{ItemDefinition}" />
    public class ItemDefinitionFactory : ITypeFactory<ItemDefinition>
    {
        /// <summary>
        /// Creates the type.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns></returns>
        public ItemDefinition CreateType(int typeId) => new(typeId);
    }
}