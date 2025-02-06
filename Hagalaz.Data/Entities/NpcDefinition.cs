using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class NpcDefinition
    {
        public NpcDefinition()
        {
            CharactersFamiliars = new HashSet<CharactersFamiliar>();
        }

        public ushort NpcId { get; set; }
        public string Name { get; set; } = null!;
        public string Examine { get; set; } = null!;
        public uint RespawnTime { get; set; }
        public ushort CombatLevel { get; set; }
        public byte ReactionType { get; set; }
        public byte BoundsType { get; set; }
        public byte Attackable { get; set; }
        public byte WalksRandomly { get; set; }
        public uint AttackSpeed { get; set; }
        public short AttackAnimation { get; set; }
        public short AttackGraphic { get; set; }
        public short DefenceAnimation { get; set; }
        public short DefenceGraphic { get; set; }
        public short DeathAnimation { get; set; }
        public short DeathGraphic { get; set; }
        public byte DeathTicks { get; set; }
        public ushort? NpcLootId { get; set; }
        public ushort? NpcPickpocketingLootId { get; set; }
        public byte SlayerLevelRequired { get; set; }

        public virtual NpcLoot? NpcLoot { get; set; }
        public virtual NpcLoot? NpcPickpocketingLoot { get; set; }
        public virtual ICollection<CharactersFamiliar> CharactersFamiliars { get; set; }
    }
}
