using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.Clans;

namespace Hagalaz.Services.GameWorld.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class ClanService : IClanService
    {

        /// <summary>
        /// Contains the clans.
        /// </summary>
        private readonly Dictionary<string, IClan> _clans = new();

        /// <summary>
        /// Contains the clan handlers.
        /// </summary>
        private readonly Dictionary<string, (IClanSettings Settings, Action Handler)> _clanHandlers = new();

        /// <summary>
        /// Puts the clan.
        /// </summary>
        /// <param name="clan">The clan.</param>
        public void PutClan(IClan clan)
        {
            if (_clans.TryGetValue(clan.Name, out var existingClan))
            {
                UnregisterEventHandlers(existingClan);
            }

            _clans[clan.Name] = clan;
            RegisterEventHandlers(clan);
        }

        /// <summary>
        /// Puts the clan settings.
        /// </summary>
        /// <param name="clan">The clan.</param>
        /// <param name="settings">The settings.</param>
        public void PutClanSettings(IClan clan, IClanSettings settings)
        {
            clan.Settings = settings;
            RegisterEventHandlers(clan);
        }

        /// <summary>
        /// Removes the clan.
        /// </summary>
        /// <param name="clanName">Name of the clan.</param>
        /// <returns></returns>
        public bool RemoveClan(string clanName)
        {
            if (_clans.ContainsKey(clanName))
            {
                var clan = _clans[clanName];
                UnregisterEventHandlers(clan);
            }

            return _clans.Remove(clanName);
        }

        /// <summary>
        /// Gets the clan by identifier.
        /// </summary>
        /// <param name="clanName">The name.</param>
        /// <returns></returns>
        public IClan? GetClanByName(string clanName) => _clans.TryGetValue(clanName, out var clan) ? clan : null;

        /// <summary>
        /// Registers the event handlers.
        /// </summary>
        /// <param name="clan">The clan.</param>
        private void RegisterEventHandlers(IClan clan)
        {
            UnregisterEventHandlers(clan);
            if (clan.Settings == null)
            {
                return;
            }

            var handler = () => OnClanSettingsChanged(clan);
            _clanHandlers[clan.Name] = (clan.Settings, handler);
            clan.Settings.OnChanged += handler;
        }

        /// <summary>
        /// Unregisters the event handlers.
        /// </summary>
        /// <param name="clan">The clan.</param>
        private void UnregisterEventHandlers(IClan clan)
        {
            if (_clanHandlers.Remove(clan.Name, out var entry))
            {
                entry.Settings.OnChanged -= entry.Handler;
            }
        }

        /// <summary>
        /// Called when [clan settings changed].
        /// </summary>
        /// <param name="clan">The clan.</param>
        private void OnClanSettingsChanged(IClan clan)
        {
            // TODO
            //_masterConnectionAdapter.SendPacketAsync(new SetClanSettingsRequestPacketComposer(new ClanSettingsDto()));
        }
    }
}