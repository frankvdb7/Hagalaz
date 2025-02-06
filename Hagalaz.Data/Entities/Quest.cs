using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class Quest
    {
        public Quest()
        {
            CharactersQuests = new HashSet<CharactersQuest>();
        }

        public ushort Id { get; set; }
        public string Name { get; set; } = null!;
        public ushort ReqQuestId1 { get; set; }
        public ushort ReqQuestId2 { get; set; }
        public ushort ReqQuestId3 { get; set; }
        public ushort ReqQuestId4 { get; set; }
        public byte MinSkillId1 { get; set; }
        public byte MinSkillId2 { get; set; }
        public byte MinSkillId3 { get; set; }
        public byte MinSkillId4 { get; set; }
        public byte MinSkillCount1 { get; set; }
        public byte MinSkillCount2 { get; set; }
        public byte MinSkillCount3 { get; set; }
        public byte MinSkillCount4 { get; set; }
        public ushort ReqItemId1 { get; set; }
        public ushort ReqItemId2 { get; set; }
        public ushort ReqItemId3 { get; set; }
        public ushort ReqItemId4 { get; set; }
        public uint ReqItemCount1 { get; set; }
        public uint ReqItemCount2 { get; set; }
        public uint ReqItemCount3 { get; set; }
        public uint ReqItemCount4 { get; set; }
        public ushort ReqNpcId1 { get; set; }
        public ushort ReqNpcId2 { get; set; }
        public ushort ReqNpcId3 { get; set; }
        public ushort ReqNpcId4 { get; set; }
        public uint ReqNpcCount1 { get; set; }
        public uint ReqNpcCount2 { get; set; }
        public uint ReqNpcCount3 { get; set; }
        public uint ReqNpcCount4 { get; set; }
        public byte RewSkillId1 { get; set; }
        public byte RewSkillId2 { get; set; }
        public byte RewSkillId3 { get; set; }
        public byte RewSkillId4 { get; set; }
        public uint RewSkillExp1 { get; set; }
        public uint RewSkillExp2 { get; set; }
        public uint RewSkillExp3 { get; set; }
        public uint RewSkillExp4 { get; set; }
        public ushort RewItemId1 { get; set; }
        public ushort RewItemId2 { get; set; }
        public ushort RewItemId3 { get; set; }
        public ushort RewItemId4 { get; set; }
        public uint RewItemCount1 { get; set; }
        public uint RewItemCount2 { get; set; }
        public uint RewItemCount3 { get; set; }
        public uint RewItemCount4 { get; set; }
        public ushort RewQuestPoints { get; set; }

        public virtual ICollection<CharactersQuest> CharactersQuests { get; set; }
    }
}
