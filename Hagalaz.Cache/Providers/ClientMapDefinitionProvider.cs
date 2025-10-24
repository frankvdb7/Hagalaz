using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Providers;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Factories;

namespace Hagalaz.Cache.Providers
{
    public class ClientMapDefinitionProvider : TypeProvider<IClientMapDefinition, ClientMapDefinitionData>, IClientMapDefinitionProvider
    {
        public ClientMapDefinitionProvider(ICacheAPI cache, ITypeFactory<IClientMapDefinition> factory, ITypeCodec<IClientMapDefinition> codec) : base(cache, factory, codec)
        {
        }
    }
}
