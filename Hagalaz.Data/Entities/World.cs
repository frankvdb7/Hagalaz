namespace Hagalaz.Data.Entities
{
    public partial class World
    {
        public ushort Id { get; set; }
        public string Name { get; set; } = null!;
        public byte MembersOnly { get; set; }
        public byte QuickChatAllowed { get; set; }
        public byte HighRisk { get; set; }
        public byte SkillReq { get; set; }
        public byte LootShareAllowed { get; set; }
        public byte Highlight { get; set; }
        public byte GameAdminsOnly { get; set; }
        public byte LobbyAdminsOnly { get; set; }
        public byte AccountCreationEnabled { get; set; }
        public byte DirectLoginEnabled { get; set; }
        public string Region { get; set; } = null!;
        public ushort Country { get; set; }
    }
}
