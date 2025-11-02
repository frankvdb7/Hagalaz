using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Providers;

namespace Hagalaz.Cache.Types.Providers
{
    public class EquipmentDefaultsProvider : IEquipmentDefaultsProvider
    {
        private readonly ICacheAPI _cache;
        private readonly ITypeCodec<IEquipmentDefaults> _codec;

        public EquipmentDefaultsProvider(ICacheAPI cache, ITypeCodec<IEquipmentDefaults> codec)
        {
            _cache = cache;
            _codec = codec;
        }

        public IEquipmentDefaults Get()
        {
            using (var container = _cache.ReadContainer(28, 6))
            {
                return _codec.Decode(0, container.Data);
            }
        }
    }
}
