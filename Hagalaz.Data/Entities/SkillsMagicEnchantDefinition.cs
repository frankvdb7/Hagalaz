using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class SkillsMagicEnchantDefinition
    {
        public SkillsMagicEnchantDefinition()
        {
            SkillsMagicEnchantProducts = new HashSet<SkillsMagicEnchantProduct>();
        }

        public ushort ButtonId { get; set; }
        public string RequiredRunes { get; set; } = null!;
        public string RequiredRunesCounts { get; set; } = null!;
        public byte RequiredLevel { get; set; }
        public decimal Experience { get; set; }
        public ushort GraphicId { get; set; }

        public virtual ICollection<SkillsMagicEnchantProduct> SkillsMagicEnchantProducts { get; set; }
    }
}
