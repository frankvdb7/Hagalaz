using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types.Factories
{
    public class ObjectTypeFactory : ITypeFactory<IObjectType>
    {
        /// <summary>
        /// Creates the type.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns></returns>
        public IObjectType CreateType(int typeId) => new ObjectType(typeId);
    }
}
