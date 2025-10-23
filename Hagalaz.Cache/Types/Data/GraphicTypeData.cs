namespace Hagalaz.Cache.Types.Data
{
    public class GraphicTypeData : TypeData
    {
        public override byte IndexId => 2;
        public override int ArchiveEntryOffset => 8;
        public override int ArchiveEntrySize => 1 << ArchiveEntryOffset;
    }
}