using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Providers;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Factories;

namespace Hagalaz.Cache.Providers
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
