using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Types.Factories
{
    /// <summary>
    /// A factory for creating <see cref="IMapType"/> instances.
    /// </summary>
    public class MapTypeFactory : ITypeFactory<IMapType>
    {
        /// <inheritdoc />
        public IMapType CreateType(int id)
        {
            return new MapType { Id = id };
        }
    }
}
