namespace Hagalaz.Data.Entities
{
    public partial class CharactersSlayerTask
    {
        public uint MasterId { get; set; }
        public ushort SlayerTaskId { get; set; }
        public uint KillCount { get; set; }

        public virtual Character Master { get; set; } = null!;
        public virtual SkillsSlayerTaskDefinition SlayerTask { get; set; } = null!;
    }
}
