namespace Hagalaz.Cache.Types.Data
{
    public class AnimationTypeData : TypeData
    {
        public override byte IndexId => 20;

        public override int ArchiveEntryOffset => 7;

        public override int ArchiveEntrySize => 1 << ArchiveEntryOffset;
    }
}
