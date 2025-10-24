using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Providers;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Factories;

namespace Hagalaz.Cache.Providers
{
    /// <summary>
    /// Provides access to config definitions.
    /// </summary>
    public class ConfigDefinitionProvider : TypeProvider<IConfigDefinition, ConfigDefinitionData>, IConfigDefinitionProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigDefinitionProvider"/> class.
        /// </summary>
        /// <param name="cacheApi">The cache API.</param>
        /// <param name="factory">The definition factory.</param>
        /// <param name="codec">The definition codec.</param>
        public ConfigDefinitionProvider(ICacheAPI cacheApi, ITypeFactory<IConfigDefinition> factory, ITypeCodec<IConfigDefinition> codec)
            : base(cacheApi, factory, codec)
        {
        }
    }
}
