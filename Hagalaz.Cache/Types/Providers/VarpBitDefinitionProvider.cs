using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Cache.Types.Data;

namespace Hagalaz.Cache.Types.Providers
{
    /// <summary>
    /// A provider for <see cref="IVarpBitDefinition"/>.
    /// </summary>
    public class VarpBitDefinitionProvider : TypeProvider<IVarpBitDefinition, VarpBitDefinitionData>, IVarpBitDefinitionProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VarpBitDefinitionProvider"/> class.
        /// </summary>
        /// <param name="cache">The cache API.</param>
        /// <param name="factory">The factory.</param>
        /// <param name="codec">The codec.</param>
        public VarpBitDefinitionProvider(ICacheAPI cache, ITypeFactory<IVarpBitDefinition> factory, ITypeCodec<IVarpBitDefinition> codec) : base(cache, factory, codec)
        {
        }
    }
}
