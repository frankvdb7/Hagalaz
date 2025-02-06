namespace Hagalaz.Data.Entities
{
    public partial class SkillsMagicEnchantProduct
    {
        public ushort ResourceId { get; set; }
        public ushort ProductId { get; set; }
        public ushort ButtonId { get; set; }

        public virtual SkillsMagicEnchantDefinition Button { get; set; } = null!;
    }
}
