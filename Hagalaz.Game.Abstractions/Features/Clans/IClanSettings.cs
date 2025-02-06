using System;

namespace Hagalaz.Game.Abstractions.Features.Clans
{
    /// <summary>
    /// 
    /// </summary>
    public interface IClanSettings
    {
        /// <summary>
        /// Occurs when [changed].
        /// </summary>
        event Action OnChanged;
        /// <summary>
        /// Gets or sets the clan identifier.
        /// </summary>
        /// <value>
        /// The clan identifier.
        /// </value>
        uint ClanId { get; set; }
        /// <summary>
        /// Gets a value indicating whether this instance is clan time.
        /// False is to use the standard game time (UTC)
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is clan time; otherwise, <c>false</c>.
        /// </value>
        bool ClanTime { get; set; }
        /// <summary>
        /// Gets the time zone.
        /// </summary>
        /// <value>
        /// The time zone.
        /// </value>
        int TimeZone { get; set; }
        /// <summary>
        /// Gets the motto.
        /// </summary>
        /// <value>
        /// The motto.
        /// </value>
        string Motto { get; set; }
        /// <summary>
        /// Gets the thread identifier.
        /// </summary>
        /// <value>
        /// The thread identifier.
        /// </value>
        string ThreadID { get; set; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="Clan"/> is recruiting.
        /// </summary>
        /// <value>
        ///   <c>true</c> if recruiting; otherwise, <c>false</c>.
        /// </value>
        bool Recruiting { get; set; }
        /// <summary>
        /// Gets the world identifier.
        /// </summary>
        /// <value>
        /// The world identifier.
        /// </value>
        ushort WorldID { get; set; }
        /// <summary>
        /// Gets the clan flag.
        /// </summary>
        /// <value>
        /// The clan flag.
        /// </value>
        int NationalFlag { get; set; }
        /// <summary>
        /// Gets the mottif top.
        /// </summary>
        /// <value>
        /// The mottif top.
        /// </value>
        byte MottifTop { get; set; }
        /// <summary>
        /// Gets the mottif bottom.
        /// </summary>
        /// <value>
        /// The mottif bottom.
        /// </value>
        byte MottifBottom { get; set; }
        /// <summary>
        /// Gets or sets the mottif colour left top.
        /// </summary>
        /// <value>
        /// The mottif colour left top.
        /// </value>
        int MottifColourLeftTop { get; set; }
        /// <summary>
        /// Gets or sets the mottif colour right bottom.
        /// </summary>
        /// <value>
        /// The mottif colour right bottom.
        /// </value>
        int MottifColourRightBottom { get; set; }
        /// <summary>
        /// Gets or sets the primary clan colour.
        /// </summary>
        /// <value>
        /// The primary clan colour.
        /// </value>
        int PrimaryClanColour { get; set; }
        /// <summary>
        /// Gets or sets the secondary clan colour.
        /// </summary>
        /// <value>
        /// The secondary clan colour.
        /// </value>
        int SecondaryClanColour { get; set; }
        /// <summary>
        /// Rank required to kick in the clan.
        /// </summary>
        /// <value>The rank to kick.</value>
        ClanRank RankToKick { get; set; }
        /// <summary>
        /// Rank required to enter to enter chat channel.
        /// </summary>
        /// <value>The rank to kick.</value>
        ClanRank RankToEnterCc { get; set; }
        /// <summary>
        /// Rank required to talk in the chat channel.
        /// </summary>
        /// <value>The rank to talk.</value>
        ClanRank RankToTalk { get; set; }
    }
}
