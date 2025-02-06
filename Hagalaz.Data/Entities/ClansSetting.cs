namespace Hagalaz.Data.Entities
{
    public partial class ClansSetting
    {
        public uint ClanId { get; set; }
        public ushort WorldId { get; set; }
        public byte Recruiting { get; set; }
        public string Motto { get; set; } = null!;
        public byte NationalFlag { get; set; }
        public string ThreadId { get; set; } = null!;
        public short TimeZone { get; set; }
        public byte ClanTime { get; set; }
        public byte MottifTop { get; set; }
        public byte MottifBottom { get; set; }
        public short MottifColourLeftTop { get; set; }
        public short MottifColourRightBottom { get; set; }
        public short PrimaryClanColour { get; set; }
        public short SecondaryClanColour { get; set; }
        public sbyte RankToTalk { get; set; }
        public sbyte RankToKick { get; set; }
        public sbyte RankToEnterCc { get; set; }

        public virtual Clan Clan { get; set; } = null!;
    }
}
