namespace Hagalaz.Cache.Types.Data
{
    public class ObjectTypeData : TypeData
    {
        public override byte IndexId => 16;

        public override int ArchiveEntryOffset => 8;

        public override int ArchiveEntrySize => 1 << ArchiveEntryOffset;
    }
}
