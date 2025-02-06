using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class NpcLoot
    {
        public NpcLoot()
        {
            NpcDefinitionNpcLoots = new HashSet<NpcDefinition>();
            NpcDefinitionNpcPickpocketingLoots = new HashSet<NpcDefinition>();
            NpcLootItems = new HashSet<NpcLootItem>();
            NpcLootChildren = new HashSet<NpcLoot>();
            NpcLootParents = new HashSet<NpcLoot>();
        }

        public ushort Id { get; set; }
        public string Name { get; set; } = null!;
        public int MaximumLootCount { get; set; }
        public byte RandomizeLootCount { get; set; }
        public byte Always { get; set; }

        public virtual ICollection<NpcDefinition> NpcDefinitionNpcLoots { get; set; }
        public virtual ICollection<NpcDefinition> NpcDefinitionNpcPickpocketingLoots { get; set; }
        public virtual ICollection<NpcLootItem> NpcLootItems { get; set; }

        public virtual ICollection<NpcLoot> NpcLootChildren { get; set; }
        public virtual ICollection<NpcLoot> NpcLootParents { get; set; }
    }
}
