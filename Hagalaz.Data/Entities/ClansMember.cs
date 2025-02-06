namespace Hagalaz.Data.Entities
{
    public partial class ClansMember
    {
        public uint MasterId { get; set; }
        public uint ClanId { get; set; }
        public uint? RecruiterId { get; set; }
        public sbyte Rank { get; set; }

        public virtual Clan Clan { get; set; } = null!;
        public virtual Character Master { get; set; } = null!;
        public virtual Character? Recruiter { get; set; }
    }
}
