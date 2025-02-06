using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Scripts.Model.Maps;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Minigames.Barrows
{
    /// <summary>
    /// </summary>
    [AreaScriptMetaData([11, 12, 14])]
    public class Barrows : AreaScript
    {
        /// <summary>
        ///     Wether this area is the crypts.
        /// </summary>
        private bool _crypts;

        /// <summary>
        ///     Happens when character enters this area.
        ///     The new area, is this scripts area.
        ///     By default this method performs some checks on wether some things are allowed in the area. (Familiars)
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void OnCreatureEnterArea(ICreature creature)
        {
            base.OnCreatureEnterArea(creature);
            if (creature is not ICharacter character)
            {
                return;
            }

            if (_crypts)
            {
                character.SendChatMessage("You feel your prayer draining as you entered the crypts.");
            }

            var script = character.GetOrAddScript<BarrowsScript>();
            script.InCrypts = _crypts;
        }

        /// <summary>
        ///     This method get's called when given character enters
        ///     this area.
        ///     By default this method does enable/disable multicombat icon
        ///     and adds/removes Attack options on characters if the ('character') is character.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void RenderEnterArea(ICreature creature)
        {
            base.RenderEnterArea(creature);
            if (creature is not ICharacter character)
            {
                return;
            }

            if (character.Widgets.GetOpenWidget(24) == null)
            {
                var script = character.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
                character.Widgets.OpenWidget(24, character.GameClient.IsScreenFixed ? 5 : 50, 1, script, false);
            }

            if (_crypts)
            {
                character.Configurations.SendMinimapConfiguration(MinimapType.BlackMinimap);
            }
        }

        /// <summary>
        ///     This method get's called when given character exits
        ///     this area.
        ///     By default this method does nothing.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void RenderExitArea(ICreature creature)
        {
            base.RenderExitArea(creature);
            if (creature is not ICharacter character)
            {
                return;
            }

            if (!Equals(creature.Area.Script))
            {
                var toClose = character.Widgets.GetOpenWidget(24);
                if (toClose != null)
                {
                    character.Widgets.CloseWidget(toClose);
                }
            }

            if (_crypts)
            {
                character.Configurations.SendMinimapConfiguration(MinimapType.Standard);
            }
        }

        /// <summary>
        ///     Happens when character exits this area.
        ///     The old area, is this scripts area.
        ///     By default this method does nothing.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void OnCreatureExitArea(ICreature creature)
        {
            base.OnCreatureExitArea(creature);
            if (creature is not ICharacter character)
            {
                return;
            }

            if (!Equals(character.Area.Script))
            {
                character.TryRemoveScript<BarrowsScript>();
            }
        }

        /// <summary>
        ///     Initializes area script.
        /// </summary>
        protected override void Initialize() => _crypts = Area.Name.Contains("Crypts");
    }
}