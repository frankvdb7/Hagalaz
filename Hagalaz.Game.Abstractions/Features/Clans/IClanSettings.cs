using System;

namespace Hagalaz.Game.Abstractions.Features.Clans
{
    /// <summary>
    /// Defines the contract for a clan's settings, which control various aspects of the clan's appearance, permissions, and behavior.
    /// </summary>
    public interface IClanSettings
    {
        /// <summary>
        /// An event that is raised whenever a setting is changed.
        /// </summary>
        event Action OnChanged;

        /// <summary>
        /// Gets or sets the unique identifier for the clan.
        /// </summary>
        uint ClanId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the clan uses its own time zone (<c>true</c>) or the standard game time (UTC) (<c>false</c>).
        /// </summary>
        bool ClanTime { get; set; }

        /// <summary>
        /// Gets or sets the time zone offset for the clan if <see cref="ClanTime"/> is enabled.
        /// </summary>
        int TimeZone { get; set; }

        /// <summary>
        /// Gets or sets the clan's motto.
        /// </summary>
        string Motto { get; set; }

        /// <summary>
        /// Gets or sets the ID of the clan's forum thread.
        /// </summary>
        string ThreadID { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the clan is currently open for recruitment.
        /// </summary>
        bool Recruiting { get; set; }

        /// <summary>
        /// Gets or sets the ID of the clan's home world.
        /// </summary>
        ushort WorldID { get; set; }

        /// <summary>
        /// Gets or sets the ID of the clan's national flag, used for display on vexillums.
        /// </summary>
        int NationalFlag { get; set; }

        /// <summary>
        /// Gets or sets the ID for the top part of the clan's custom motif.
        /// </summary>
        byte MottifTop { get; set; }

        /// <summary>
        /// Gets or sets the ID for the bottom part of the clan's custom motif.
        /// </summary>
        byte MottifBottom { get; set; }

        /// <summary>
        /// Gets or sets the color for the top-left section of the clan's motif.
        /// </summary>
        int MottifColourLeftTop { get; set; }

        /// <summary>
        /// Gets or sets the color for the bottom-right section of the clan's motif.
        /// </summary>
        int MottifColourRightBottom { get; set; }

        /// <summary>
        /// Gets or sets the primary color used for the clan's cape and vexillum.
        /// </summary>
        int PrimaryClanColour { get; set; }

        /// <summary>
        /// Gets or sets the secondary color used for the clan's cape and vexillum.
        /// </summary>
        int SecondaryClanColour { get; set; }

        /// <summary>
        /// Gets or sets the minimum rank required to kick a member from the clan chat channel.
        /// </summary>
        ClanRank RankToKick { get; set; }

        /// <summary>
        /// Gets or sets the minimum rank required to enter the clan's chat channel.
        /// </summary>
        ClanRank RankToEnterCc { get; set; }

        /// <summary>
        /// Gets or sets the minimum rank required to speak in the clan's chat channel.
        /// </summary>
        ClanRank RankToTalk { get; set; }
    }
}
