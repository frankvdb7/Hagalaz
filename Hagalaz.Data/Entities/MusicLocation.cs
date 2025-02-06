namespace Hagalaz.Data.Entities
{
    public partial class MusicLocation
    {
        public ushort MusicId { get; set; }
        public uint RegionId { get; set; }

        public virtual MusicDefinition Music { get; set; } = null!;
    }
}
