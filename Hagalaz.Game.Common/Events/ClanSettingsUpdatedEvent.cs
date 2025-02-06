using Hagalaz.Game.Abstractions.Features.Clans;

namespace Hagalaz.Game.Common.Events
{
    /// <summary>
    /// 
    /// </summary>
    public class ClanSettingsUpdatedEvent : Event
    {
        /// <summary>
        /// Gets the clan.
        /// </summary>
        /// <value>
        /// The clan.
        /// </value>
        public IClan Clan { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClanSettingsUpdatedEvent"/> class.
        /// </summary>
        /// <param name="clan">The clan.</param>
        public ClanSettingsUpdatedEvent(IClan clan) => Clan = clan;
    }
}