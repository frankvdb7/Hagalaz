using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types.Data
{
    /// <summary>
    /// Provides the cache location for config definitions.
    /// </summary>
    public class ConfigDefinitionData : TypeData
    {
        /// <inheritdoc />
        public override byte IndexId => 2;

        /// <inheritdoc />
        public override int ArchiveEntryOffset => 16;

        /// <inheritdoc />
        public override int ArchiveEntrySize => 1;
    }
}
