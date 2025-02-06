namespace Hagalaz.Data.Entities
{
    public partial class EquipmentDefinition
    {
        public ushort Id { get; set; }
        /// <summary>
        /// This field is updated automatically by the server.
        /// </summary>
        public string Name { get; set; } = null!;
        public string? Fullbody { get; set; }
        public string? Fullhat { get; set; }
        public string? Fullmask { get; set; }
        public short DefenceAnim { get; set; }
        public short Attackanim1 { get; set; }
        public short Attackanim2 { get; set; }
        public short Attackanim3 { get; set; }
        public short Attackanim4 { get; set; }
        public short Attackgfx1 { get; set; }
        public short Attackgfx2 { get; set; }
        public short Attackgfx3 { get; set; }
        public short Attackgfx4 { get; set; }
        public short AttackDistance { get; set; }
    }
}
