namespace Hagalaz.Cache.Types.Data
{
    public class ClientMapDefinitionData : TypeData
    {
        public override byte IndexId => 17;

        public override int ArchiveEntryOffset => 8;

        public override int ArchiveEntrySize => 1 << ArchiveEntryOffset;
    }
}
