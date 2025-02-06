using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Scripts.Model.Maps;

namespace Hagalaz.Game.Scripts.Minigames.TzHaar.Cave
{
    /// <summary>
    /// </summary>
    [AreaScriptMetaData([18])]
    public class TzHaarFightCaves : AreaScript
    {
        /// <summary>
        ///     Called when [creature enter area].
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void OnCreatureEnterArea(ICreature creature)
        {
            base.OnCreatureEnterArea(creature);
            if (creature is not ICharacter character)
            {
                return;
            }

            if (!character.HasScript<TzHaarCaveScript>())
            {
                character.AddScript<TzHaarCaveScript>();
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

            if (!Equals(creature.Area.Script))
            {
                character.TryRemoveScript<TzHaarCaveScript>();
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
                var toClose = character.Widgets.GetOpenWidget(316);
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