using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;

namespace Hagalaz.Cache.Providers
{
    public class Cs2DefinitionProvider : TypeProvider<ICs2Definition, Cs2DefinitionData>
    {
        public Cs2DefinitionProvider(ICacheAPI cache, Cs2DefinitionFactory factory, ICs2DefinitionCodec codec) : base(cache, factory, codec)
        {
        }
    }
}
