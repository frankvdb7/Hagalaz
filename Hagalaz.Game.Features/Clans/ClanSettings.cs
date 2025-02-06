using System;
using Hagalaz.Game.Abstractions.Features.Clans;
using Hagalaz.Utilities;

namespace Hagalaz.Game.Features.Clans
{
    /// <summary>
    /// 
    /// </summary>
    public class ClanSettings : IClanSettings
    {
        /// <summary>
        /// Occurs when [changed].
        /// </summary>
        public event Action OnChanged;

        /// <summary>
        /// The national flag
        /// </summary>
        private int _nationalFlag = 0;
        /// <summary>
        /// The world identifier
        /// </summary>
        private ushort _worldID = 1;
        /// <summary>
        /// The clan time
        /// </summary>
        private bool _clanTime = false;
        /// <summary>
        /// The time zone
        /// </summary>
        private int _timeZone = -1;
        /// <summary>
        /// The motto
        /// </summary>
        private string _motto = string.Empty;
        /// <summary>
        /// The thread identifier
        /// </summary>
        private string _threadID = string.Empty;
        /// <summary>
        /// The recruiting
        /// </summary>
        private bool _recruiting = true;
        /// <summary>
        /// The mottif top
        /// </summary>
        private byte _mottifTop = 0;
        /// <summary>
        /// The mottif bottom
        /// </summary>
        private byte _mottifBottom = 0;
        /// <summary>
        /// The left top mottif colour
        /// </summary>
        private int _mottifColourLeftTop = -1;
        /// <summary>
        /// The right bottom mottif colour
        /// </summary>
        private int _mottifColourRightBottom = -1;
        /// <summary>
        /// The primary mottif colour
        /// </summary>
        private int _primaryClanColour = -1;
        /// <summary>
        /// The secondary mottif colour
        /// </summary>
        private int _secondaryClanColour = -1;
        /// <summary>
        /// Rank required to talk in the chat channel.
        /// </summary>
        /// <value>The rank to talk.</value>
        private ClanRank _rankToTalk = ClanRank.Guest;
        /// <summary>
        /// Rank required to kick in the clan.
        /// </summary>
        /// <value>The rank to kick.</value>
        private ClanRank _rankToKick = ClanRank.Admin;
        /// <summary>
        /// Rank required to enter to enter chat channel.
        /// </summary>
        /// <value>The rank to kick.</value>
        private ClanRank _rankToEnterCc = ClanRank.Guest;

        /// <summary>
        /// Gets or sets the clan identifier.
        /// </summary>
        /// <value>
        /// The clan identifier.
        /// </value>
        public uint ClanId { get; set; }
        /// <summary>
        /// Gets a value indicating whether this instance is clan time.
        /// False is to use the standard game time (UTC)
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is clan time; otherwise, <c>false</c>.
        /// </value>
        public bool ClanTime
        {
            get => _clanTime;
            set
            {
                if (SetPropertyUtility.SetStruct(ref _clanTime, value))
                    OnChanged?.Invoke();
            }
        }
        /// <summary>
        /// Gets the time zone.
        /// </summary>
        /// <value>
        /// The time zone.
        /// </value>
        public int TimeZone
        {
            get => _timeZone;
            set
            {
                if (SetPropertyUtility.SetStruct(ref _timeZone, value))
                    OnChanged?.Invoke();
            }
        }
        /// <summary>
        /// Gets the motto.
        /// </summary>
        /// <value>
        /// The motto.
        /// </value>
        public string Motto
        {
            get => _motto;
            set
            {
                if (SetPropertyUtility.SetClass(ref _motto, value))
                    OnChanged?.Invoke();
            }
        }
        /// <summary>
        /// Gets the thread identifier.
        /// </summary>
        /// <value>
        /// The thread identifier.
        /// </value>
        public string ThreadID
        {
            get => _threadID;
            set
            {
                if (SetPropertyUtility.SetClass(ref _threadID, value))
                    OnChanged?.Invoke();
            }
        }
        /// <summary>
        /// Gets a value indicating whether this <see cref="Clan"/> is recruiting.
        /// </summary>
        /// <value>
        ///   <c>true</c> if recruiting; otherwise, <c>false</c>.
        /// </value>
        public bool Recruiting
        {
            get => _recruiting;
            set
            {
                if (SetPropertyUtility.SetStruct(ref _recruiting, value))
                    OnChanged?.Invoke();
            }
        }
        /// <summary>
        /// Gets the world identifier.
        /// </summary>
        /// <value>
        /// The world identifier.
        /// </value>
        public ushort WorldID
        {
            get => _worldID;
            set
            {
                if (SetPropertyUtility.SetStruct(ref _worldID, value))
                    OnChanged?.Invoke();
            }
        }
        /// <summary>
        /// Gets the clan flag.
        /// </summary>
        /// <value>
        /// The clan flag.
        /// </value>
        public int NationalFlag
        {
            get => _nationalFlag;
            set
            {
                if (SetPropertyUtility.SetStruct(ref _nationalFlag, value))
                    OnChanged?.Invoke();
            }
        }
        /// <summary>
        /// Gets the mottif top.
        /// </summary>
        /// <value>
        /// The mottif top.
        /// </value>
        public byte MottifTop
        {
            get => _mottifTop;
            set
            {
                if (SetPropertyUtility.SetStruct(ref _mottifTop, value))
                    OnChanged?.Invoke();
            }
        }
        /// <summary>
        /// Gets the mottif bottom.
        /// </summary>
        /// <value>
        /// The mottif bottom.
        /// </value>
        public byte MottifBottom
        {
            get => _mottifBottom;
            set
            {
                if (SetPropertyUtility.SetStruct(ref _mottifBottom, value))
                    OnChanged?.Invoke();
            }
        }
        /// <summary>
        /// Gets or sets the mottif colour left top.
        /// </summary>
        /// <value>
        /// The mottif colour left top.
        /// </value>
        public int MottifColourLeftTop
        {
            get => _mottifColourLeftTop;
            set
            {
                if (SetPropertyUtility.SetStruct(ref _mottifColourLeftTop, value))
                    OnChanged?.Invoke();
            }
        }
        /// <summary>
        /// Gets or sets the mottif colour right bottom.
        /// </summary>
        /// <value>
        /// The mottif colour right bottom.
        /// </value>
        public int MottifColourRightBottom
        {
            get => _mottifColourRightBottom;
            set
            {
                if (SetPropertyUtility.SetStruct(ref _mottifColourRightBottom, value))
                    OnChanged?.Invoke();
            }
        }
        /// <summary>
        /// Gets or sets the primary clan colour.
        /// </summary>
        /// <value>
        /// The primary clan colour.
        /// </value>
        public int PrimaryClanColour
        {
            get => _primaryClanColour;
            set
            {
                if (SetPropertyUtility.SetStruct(ref _primaryClanColour, value))
                    OnChanged?.Invoke();
            }
        }
        /// <summary>
        /// Gets or sets the secondary clan colour.
        /// </summary>
        /// <value>
        /// The secondary clan colour.
        /// </value>
        public int SecondaryClanColour
        {
            get => _secondaryClanColour;
            set
            {
                if (SetPropertyUtility.SetStruct(ref _secondaryClanColour, value))
                    OnChanged?.Invoke();
            }
        }
        /// <summary>
        /// Rank required to talk in the chat channel.
        /// </summary>
        /// <value>The rank to talk.</value>
        public ClanRank RankToTalk
        {
            get => _rankToTalk;
            set
            {
                if (SetPropertyUtility.SetStruct(ref _rankToTalk, value))
                    OnChanged?.Invoke();
            }
        }
        /// <summary>
        /// Rank required to kick in the clan.
        /// </summary>
        /// <value>The rank to kick.</value>
        public ClanRank RankToKick
        {
            get => _rankToKick;
            set
            {
                if (SetPropertyUtility.SetStruct(ref _rankToKick, value))
                    OnChanged?.Invoke();
            }
        }
        /// <summary>
        /// Rank required to enter to enter chat channel.
        /// </summary>
        /// <value>The rank to kick.</value>
        public ClanRank RankToEnterCc
        {
            get => _rankToEnterCc;
            set
            {
                if (SetPropertyUtility.SetStruct(ref _rankToEnterCc, value))
                    OnChanged?.Invoke();
            }
        }

        /// <summary>
        /// Inserts the ISqlDataObject into the database via the client.
        /// </summary>
        /// <param name="client">The client to execute query on.</param>
        /// <returns>
        /// Returns true if the client has successfully inserted the object.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        //public bool Insert(ISqlDatabaseClient client)
        //{
        //    StringBuilder query = new StringBuilder()
        //    .Append("INSERT INTO clans_settings (clan_id) VALUES(")
        //    .Append(ClanId)
        //    .Append(");");
        //    client.ExecuteUpdate(query.ToString());
        //    return true;
        //}

        /// <summary>
        /// Updates the ISqlDataObject in the database via the client.
        /// </summary>
        /// <param name="client">The client to execute query on.</param>
        /// <returns>
        /// Returns true if the client has successfully updated the object.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        //public bool Update(ISqlDatabaseClient client)
        //{
        //    StringBuilder settings = new StringBuilder("UPDATE clans_settings SET ")
        //    .Append("world_id=").Append(_worldID).Append(",")
        //    .Append("recruiting=").Append(_recruiting ? 1 : 0).Append(",")
        //    .Append("motto='").Append(_motto == null ? string.Empty : _motto).Append("',")
        //    .Append("national_flag=").Append(_nationalFlag).Append(",")
        //    .Append("thread_id='").Append(_threadID == null ? string.Empty : _threadID).Append("',")
        //    .Append("time_zone=").Append(_timeZone).Append(",")
        //    .Append("clan_time=").Append(_clanTime ? 1 : 0).Append(",")
        //    .Append("mottif_top=").Append(_mottifTop).Append(",")
        //    .Append("mottif_bottom=").Append(_mottifBottom).Append(",")
        //    .Append("mottif_colour_left_top=").Append(_mottifColourLeftTop).Append(",")
        //    .Append("mottif_colour_right_bottom=").Append(_mottifColourRightBottom).Append(",")
        //    .Append("primary_clan_colour=").Append(_primaryClanColour).Append(",")
        //    .Append("secondary_clan_colour=").Append(_secondaryClanColour).Append(",")
        //    .Append("rank_to_talk=").Append((sbyte)_rankToTalk).Append(",")
        //    .Append("rank_to_kick=").Append((sbyte)_rankToKick).Append(",")
        //    .Append("rank_to_enter_cc=").Append((sbyte)_rankToEnterCc)
        //    .Append(" WHERE clan_id=").Append(ClanId).Append(" LIMIT 1;");
        //    client.ExecuteUpdate(settings.ToString());
        //    return true;
        //}

        /// <summary>
        /// Deletes the ISqlDataObject from the database via the client.
        /// </summary>
        /// <param name="client">The client to execute query on.</param>
        /// <returns>
        /// Returns true if the client has successfully deleted the object.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        //public bool Delete(ISqlDatabaseClient client)
        //{
        //    throw new NotImplementedException();
        //}

        /// <summary>
        /// Loads the ISqlDataObject in the database via the client. Async loading is not allowed.
        /// </summary>
        /// <param name="client">The client to execute query on.</param>
        /// <returns>
        /// Returns true if the client has successfully loaded the object.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        //public bool Load(ISqlDatabaseClient client)
        //{
        //    DataRow data = client.ReadDataRow("SELECT * FROM clans_settings WHERE clan_id=@clan_id LIMIT 1;");
        //    if (data == null)
        //        return false;
        //    _worldID = Convert.ToUInt16(data["world_id"]);
        //    _recruiting = Convert.ToByte(data["recruiting"]) == 1;
        //    _motto = Convert.ToString(data["motto"]);
        //    _threadID = Convert.ToString(data["thread_id"]);
        //    _nationalFlag = Convert.ToByte(data["national_flag"]);
        //    _timeZone = Convert.ToInt16(data["time_zone"]);
        //    _clanTime = Convert.ToByte(data["clan_time"]) == 1;
        //    _mottifTop = Convert.ToByte(data["mottif_top"]);
        //    _mottifBottom = Convert.ToByte(data["mottif_bottom"]);
        //    _mottifColourLeftTop = Convert.ToInt16(data["mottif_colour_left_top"]);
        //    _mottifColourRightBottom = Convert.ToInt16(data["mottif_colour_right_bottom"]);
        //    _primaryClanColour = Convert.ToInt16(data["primary_clan_colour"]);
        //    _secondaryClanColour = Convert.ToInt16(data["secondary_clan_colour"]);
        //    _rankToTalk = (ClanRank)Convert.ToSByte(data["rank_to_talk"]);
        //    _rankToKick = (ClanRank)Convert.ToSByte(data["rank_to_kick"]);
        //    _rankToEnterCc = (ClanRank)Convert.ToSByte(data["rank_to_enter_cc"]);
        //    return true;
        //}
    }
}
