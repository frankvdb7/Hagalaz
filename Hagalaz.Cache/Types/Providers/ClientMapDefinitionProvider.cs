using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Cache.Types.Data;

namespace Hagalaz.Cache.Types.Providers
{
    public class ClientMapDefinitionProvider : TypeProvider<IClientMapDefinition, ClientMapDefinitionData>, IClientMapDefinitionProvider
    {
        public ClientMapDefinitionProvider(ICacheAPI cache, ITypeFactory<IClientMapDefinition> factory, ITypeCodec<IClientMapDefinition> codec) : base(cache, factory, codec)
        {
        }
    }
}
