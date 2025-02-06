using System;
using Hagalaz.Cache;
using Hagalaz.Cache.Types;
using Hagalaz.Game.Abstractions.Features.Clans;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Features.Clans;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Clans
{
    /// <summary>
    /// </summary>
    public class ClanDetails : WidgetScript
    {
        /// <summary>
        ///     The clan
        /// </summary>
        private IClan _clan;

        /// <summary>
        ///     The planter
        /// </summary>
        private readonly string _planter;

        /// <summary>
        ///     The cache
        /// </summary>
        private readonly ICacheAPI _cache;

        public ClanDetails(ICharacterContextAccessor characterContextAccessor, ICacheAPI cache) : base(characterContextAccessor) => _cache = cache;

        public ClanDetails(ICharacterContextAccessor characterContextAccessor, Clan clan, string planter) : base(characterContextAccessor)
        {
            _clan = clan;
            _planter = planter;
        }

        /// <summary>
        ///     Raises the Close event.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            if (string.IsNullOrEmpty(_planter))
            {
                _clan = Owner.Clan;
            }

            //this.interfaceInstance.DrawString(88, string.IsNullOrEmpty(this.clan.Settings.Motto) ? "Not set." : this.clan.Settings.Motto);

            var clientMap = ClientMapDefinition.GetClientMapDefinition(_cache, 3686);
            /*if (this.clan.Settings.MottifTop != 0)
                this.interfaceInstance.DrawSprite(96, (short)clientMap.GetIntValue(this.clan.Settings.MottifTop + 1)); // right
            if (this.clan.Settings.MottifBottom != 0)
                this.interfaceInstance.DrawSprite(106, (short)clientMap.GetIntValue(this.clan.Settings.MottifBottom + 1));* // left */

            InterfaceInstance.DrawString(185, DateTime.Now.AddMinutes((_clan.Settings.TimeZone - 72) / 3 * 30).ToString("HH:mm"));
            InterfaceInstance.DrawString(186, DateTime.Now.ToString("HH:mm"));

            if (string.IsNullOrEmpty(_planter))
            {
                InterfaceInstance.SetVisible(90, false);
                InterfaceInstance.SetVisible(170, false);
            }
            else
            {
                InterfaceInstance.DrawString(92, _planter);
            }

            //Owner.Session.SendPacketAsync(new ClanPacketComposer(_clan, false));
        }
    }
}