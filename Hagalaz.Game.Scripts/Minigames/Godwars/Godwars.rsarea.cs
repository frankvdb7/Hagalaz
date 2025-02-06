using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Scripts.Model.Maps;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Minigames.Godwars
{
    [AreaScriptMetaData([3, 4, 5, 6, 16, 17])]
    public class Godwars : AreaScript
    {
        /// <summary>
        ///     The godwars area ids
        /// </summary>
        private readonly int[] _godwarsArea = [3, 4, 5, 6, 16, 17, 27, 28];

        /// <summary>
        ///     Initializes area script.
        /// </summary>
        protected override void Initialize()
        {
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

            var overlay = character.Widgets.GetOpenWidget(601);
            if (overlay == null)
            {
                var defaultScript = character.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
                character.Widgets.OpenWidget(601, character.GameClient.IsScreenFixed ? 49 : 68, 1, defaultScript, false);
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
            if (!Equals(creature.Area.Script))
            {
                if (creature is not ICharacter character)
                {
                    return;
                }

                character.TryRemoveScript<GodwarsScript>();

                var toClose = character.Widgets.GetOpenWidget(601);
                if (toClose != null)
                {
                    character.Widgets.CloseWidget(toClose);
                }
            }
        }
    }
}