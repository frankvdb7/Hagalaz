namespace Hagalaz.Cache.Types.Data
{
    /// <summary>
    /// 
    /// </summary>
    public class NpcTypeData : TypeData
    {
        /// <summary>
        /// 
        /// </summary>
        public override byte IndexId => 18;
        /// <summary>
        /// 
        /// </summary>
        public override int ArchiveEntryOffset => 7;
        /// <summary>
        /// 
        /// </summary>
        public override int ArchiveEntrySize => 1 << ArchiveEntryOffset;
    }
}