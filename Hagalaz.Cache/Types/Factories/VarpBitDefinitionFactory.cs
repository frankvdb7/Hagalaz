using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;

namespace Hagalaz.Cache.Types.Factories
{
    /// <summary>
    /// A factory for creating <see cref="IVarpBitDefinition"/> instances.
    /// </summary>
    public class VarpBitDefinitionFactory : ITypeFactory<IVarpBitDefinition>
    {
        /// <inheritdoc />
        public IVarpBitDefinition CreateType(int id) => new VarpBitDefinition(id);
    }
}
