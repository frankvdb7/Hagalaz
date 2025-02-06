namespace Hagalaz.Data.Entities
{
    public partial class ItemCombine
    {
        public ushort ReqItemId1 { get; set; }
        public ushort ReqItemId2 { get; set; }
        public ushort RewItemId { get; set; }
        public uint ReqItemCount1 { get; set; }
        public uint ReqItemCount2 { get; set; }
        public uint RewItemCount { get; set; }
        public sbyte ReqSkillId1 { get; set; }
        public sbyte ReqSkillId2 { get; set; }
        public sbyte ReqSkillId3 { get; set; }
        public sbyte RewSkillId1 { get; set; }
        public sbyte RewSkillId2 { get; set; }
        public sbyte RewSkillId3 { get; set; }
        public byte ReqSkillCount1 { get; set; }
        public byte ReqSkillCount2 { get; set; }
        public byte ReqSkillCount3 { get; set; }
        public decimal RewSkillExp1 { get; set; }
        public decimal RewSkillExp2 { get; set; }
        public decimal RewSkillExp3 { get; set; }
        public ushort GraphicId { get; set; }
        public ushort AnimId { get; set; }
    }
}
