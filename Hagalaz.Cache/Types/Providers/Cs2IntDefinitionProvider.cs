using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Cache.Types.Data;

namespace Hagalaz.Cache.Types.Providers
{
    public class Cs2IntDefinitionProvider : TypeProvider<ICs2IntDefinition, Cs2IntDefinitionData>, ICs2IntDefinitionProvider
    {
        public Cs2IntDefinitionProvider(ICacheAPI cache, ITypeFactory<ICs2IntDefinition> factory, ICs2IntDefinitionCodec codec)
            : base(cache, factory, codec)
        {
        }

        public ICs2IntDefinition GetCs2IntDefinition(int id) => Get(id);
    }
}
