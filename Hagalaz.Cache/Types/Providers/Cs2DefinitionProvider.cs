using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Factories;

namespace Hagalaz.Cache.Types.Providers
{
    public class Cs2DefinitionProvider : TypeProvider<ICs2Definition, Cs2DefinitionData>
    {
        public Cs2DefinitionProvider(ICacheAPI cache, Cs2DefinitionFactory factory, ICs2DefinitionCodec codec) : base(cache, factory, codec)
        {
        }
    }
}
