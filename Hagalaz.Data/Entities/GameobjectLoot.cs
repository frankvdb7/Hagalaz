using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class GameobjectLoot
    {
        public GameobjectLoot()
        {
            GameobjectDefinitions = new HashSet<GameobjectDefinition>();
            GameobjectLootItems = new HashSet<GameobjectLootItem>();
            GameobjectLootChildren = new HashSet<GameobjectLoot>();
            GameobjectLootParents = new HashSet<GameobjectLoot>();
        }

        public ushort Id { get; set; }
        public string Name { get; set; } = null!;
        public uint MaximumLootCount { get; set; }
        public byte RandomizeLootCount { get; set; }

        public virtual ICollection<GameobjectDefinition> GameobjectDefinitions { get; set; }
        public virtual ICollection<GameobjectLootItem> GameobjectLootItems { get; set; }

        public virtual ICollection<GameobjectLoot> GameobjectLootChildren { get; set; }
        public virtual ICollection<GameobjectLoot> GameobjectLootParents { get; set; }
    }
}
