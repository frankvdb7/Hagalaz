using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class SkillsSlayerTaskDefinition
    {
        public SkillsSlayerTaskDefinition()
        {
            CharactersSlayerTasks = new HashSet<CharactersSlayerTask>();
        }

        public ushort Id { get; set; }
        public string Name { get; set; } = null!;
        public ushort SlayerMasterId { get; set; }
        /// <summary>
        /// The NPC IDs that can be slayed for slayer experience.
        /// </summary>
        public string NpcIds { get; set; } = null!;
        public int MinimumCount { get; set; }
        public int MaximumCount { get; set; }
        public byte LevelRequirement { get; set; }
        public byte CombatRequirement { get; set; }
        public uint CoinCount { get; set; }

        public virtual SkillsSlayerMasterDefinition SlayerMaster { get; set; } = null!;
        public virtual ICollection<CharactersSlayerTask> CharactersSlayerTasks { get; set; }
    }
}
