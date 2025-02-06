using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class ItemLoot
    {
        public ItemLoot()
        {
            ItemLootItems = new HashSet<ItemLootItem>();
            ItemLootChildren = new HashSet<ItemLoot>();
            ItemLootParents = new HashSet<ItemLoot>();
        }

        public ushort Id { get; set; }
        public string Name { get; set; } = null!;
        public uint MaximumLootCount { get; set; }
        public byte RandomizeLootCount { get; set; }

        public virtual ICollection<ItemLootItem> ItemLootItems { get; set; }

        public virtual ICollection<ItemLoot> ItemLootChildren { get; set; }
        public virtual ICollection<ItemLoot> ItemLootParents { get; set; }
    }
}
