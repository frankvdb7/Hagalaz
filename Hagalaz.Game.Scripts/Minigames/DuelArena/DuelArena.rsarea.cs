using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Scripts.Model.Maps;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Minigames.DuelArena
{
    [AreaScriptMetaData([20])]
    public class DuelArena : AreaScript
    {
        /// <summary>
        ///     Happens when character enters this area.
        ///     The new area, is this scripts area.
        ///     By default this method performs some checks on wether some things are allowed in the area. (Familiars)
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void OnCreatureEnterArea(ICreature creature)
        {
            base.OnCreatureEnterArea(creature);
            if (creature is ICharacter character)
            {
                if (!character.HasScript<DuelArenaScript>())
                {
                    character.AddScript<DuelArenaScript>();
                }
            }
        }

        /// <summary>
        ///     Renders the enter area.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void RenderEnterArea(ICreature creature)
        {
            if (creature is ICharacter character)
            {
                character.Configurations.SendGlobalCs2Int(616, Area.MultiCombat ? 1 : 0);
                if (character.Widgets.GetOpenWidget(1362) == null)
                {
                    var defaultScript = character.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
                    character.Widgets.OpenWidget(1362, character.GameClient.IsScreenFixed ? 50 : 69, 1, defaultScript, false);
                }

                var script = character.GetOrAddScript<DuelArenaScript>();
                script.RegisterChallengeOptionHandler();
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
            if (creature is ICharacter character)
            {
                character.TryRemoveScript<DuelArenaScript>();
            }
        }

        /// <summary>
        ///     Renders the exit area.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void RenderExitArea(ICreature creature)
        {
            if (creature is not ICharacter character)
            {
                return;
            }

            if (!Equals(character.Area.Script))
            {
                var toClose = character.Widgets.GetOpenWidget(1362);
                if (toClose != null)
                {
                    character.Widgets.CloseWidget(toClose);
                }
            }
        }

        /// <summary>
        ///     Initializes area script.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}