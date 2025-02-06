using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Hagalaz.Data.Entities
{
    public partial class Character : IdentityUser<uint>
    {
        public Character()
        {
            Aspnetuserclaims = new HashSet<Aspnetuserclaim>();
            Aspnetuserlogins = new HashSet<Aspnetuserlogin>();
            Aspnetusertokens = new HashSet<Aspnetusertoken>();
            Aspnetuserroles = new HashSet<Aspnetuserrole>();
            Blacklists = new HashSet<Blacklist>();
            CharactersAppeals = new HashSet<CharactersAppeal>();
            CharactersContactContacts = new HashSet<CharactersContact>();
            CharactersContactMasters = new HashSet<CharactersContact>();
            CharactersFarmingPatches = new HashSet<CharactersFarmingPatch>();
            CharactersItems = new HashSet<CharactersItem>();
            CharactersItemsLooks = new HashSet<CharactersItemsLook>();
            CharactersNotes = new HashSet<CharactersNote>();
            CharactersOffenceMasters = new HashSet<CharactersOffence>();
            CharactersOffenceModerators = new HashSet<CharactersOffence>();
            CharactersPermissions = new HashSet<CharactersPermission>();
            CharactersReportReporteds = new HashSet<CharactersReport>();
            CharactersReportReporters = new HashSet<CharactersReport>();
            CharactersRewards = new HashSet<CharactersReward>();
            CharactersStates = new HashSet<CharactersState>();
            ClansMemberRecruiters = new HashSet<ClansMember>();
            Clans = new HashSet<Clan>();
            Roles = new HashSet<Aspnetrole>();
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Character(string userName)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            : base(userName)
        {
            
        }
        
        public string? Password { get; set; }
        public string DisplayName { get; set; } = null!;
        public string? PreviousDisplayName { get; set; }
        public DateTime? DisplayNameLastChanged { get; set; }
        public string RegisterIp { get; set; } = null!;
        public DateTime? RegisterDate { get; set; }
        public string? LastIp { get; set; }
        public DateTimeOffset? LastLobbyLogin { get; set; }
        public DateTimeOffset? LastGameLogin { get; set; }
        public short Birthyear { get; set; }
        public short CoordX { get; set; }
        public short CoordY { get; set; }
        public byte CoordZ { get; set; }

        public virtual CharactersProfile CharacterProfile { get; set; } = null!;
        public virtual CharactersFamiliar CharactersFamiliar { get; set; } = null!;
        public virtual CharactersLook CharactersLook { get; set; } = null!;
        public virtual CharactersMusic CharactersMusic { get; set; } = null!;
        public virtual CharactersMusicPlaylist CharactersMusicPlaylist { get; set; } = null!;
        public virtual CharactersQuest CharactersQuest { get; set; } = null!;
        public virtual CharactersSlayerTask CharactersSlayerTask { get; set; } = null!;
        public virtual CharactersStatistic CharactersStatistic { get; set; } = null!;
        public virtual ClansMember ClansMemberMaster { get; set; } = null!;
        public virtual MinigamesBarrow MinigamesBarrow { get; set; } = null!;
        public virtual MinigamesDuelArena MinigamesDuelArena { get; set; } = null!;
        public virtual MinigamesGodwar MinigamesGodwar { get; set; } = null!;
        public virtual MinigamesTzhaarCave MinigamesTzhaarCave { get; set; } = null!;
        public virtual ICollection<Aspnetuserclaim> Aspnetuserclaims { get; set; }
        public virtual ICollection<Aspnetuserlogin> Aspnetuserlogins { get; set; }
        public virtual ICollection<Aspnetusertoken> Aspnetusertokens { get; set; }
        public virtual ICollection<Aspnetuserrole> Aspnetuserroles { get; set; }
        public virtual ICollection<Blacklist> Blacklists { get; set; }
        public virtual ICollection<CharactersAppeal> CharactersAppeals { get; set; }
        public virtual ICollection<CharactersContact> CharactersContactContacts { get; set; }
        public virtual ICollection<CharactersContact> CharactersContactMasters { get; set; }
        public virtual ICollection<CharactersFarmingPatch> CharactersFarmingPatches { get; set; }
        public virtual ICollection<CharactersItem> CharactersItems { get; set; }
        public virtual ICollection<CharactersItemsLook> CharactersItemsLooks { get; set; }
        public virtual ICollection<CharactersNote> CharactersNotes { get; set; }
        public virtual ICollection<CharactersOffence> CharactersOffenceMasters { get; set; }
        public virtual ICollection<CharactersOffence> CharactersOffenceModerators { get; set; }
        public virtual ICollection<CharactersPermission> CharactersPermissions { get; set; }
        public virtual ICollection<CharactersReport> CharactersReportReporteds { get; set; }
        public virtual ICollection<CharactersReport> CharactersReportReporters { get; set; }
        public virtual ICollection<CharactersReward> CharactersRewards { get; set; }
        public virtual ICollection<CharactersState> CharactersStates { get; set; }
        public virtual ICollection<ClansMember> ClansMemberRecruiters { get; set; }

        public virtual ICollection<Clan> Clans { get; set; }
        public virtual ICollection<Aspnetrole> Roles { get; set; }
    }
}
