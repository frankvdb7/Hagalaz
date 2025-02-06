using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Scripts.Minigames.Crucible.Characters;
using Hagalaz.Game.Scripts.Model.Maps;

namespace Hagalaz.Game.Scripts.Minigames.Crucible
{
    [AreaScriptMetaData([29, 30, 31, 32, 33])]
    public class CrucibleArea : AreaScript
    {
        /// <summary>
        ///     Get's if character can do standart teleport. (Escape)
        ///     By default , this method returns true.
        /// </summary>
        /// <param name="character">Character which is trying to 'escape'</param>
        /// <returns>
        ///     <c>true</c> if this instance [can do standart teleport] the specified character; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanDoStandardTeleport(ICharacter character)
        {
            character.SendChatMessage("You can't teleport in the Crucible.");
            return false;
        }

        /// <summary>
        ///     Determines whether this instance [can do game object teleport] the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="obj">The obj.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can do game object teleport] the specified character; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanDoGameObjectTeleport(ICharacter character, IGameObject obj)
        {
            character.SendChatMessage("You can't teleport in the Crucible.");
            return false;
        }

        /// <summary>
        ///     Determines whether this instance [can do item teleport] the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="item">The item.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can do item teleport] the specified character; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanDoItemTeleport(ICharacter character, IItem item)
        {
            character.SendChatMessage("You can't teleport in the Crucible.");
            return false;
        }

        /// <summary>
        ///     This method get's called when given character enters
        ///     this area.
        ///     The new area, is this scripts area.
        ///     By default this method does enable/disable multicombat icon
        ///     and adds/removes Attack options on characters if the ('character') is character.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void RenderEnterArea(ICreature creature)
        {
            base.RenderEnterArea(creature);
            if (creature is ICharacter)
            {
                var character = creature as ICharacter;
                if (!character.HasScript<CrucibleScript>())
                {
                    character.AddScript<CrucibleScript>();
                    character.Appearance.Refresh(); // Refresh the combat level range and base combat level.
                }
            }
        }

        /// <summary>
        ///     This method get's called when given character exits
        ///     this area.
        ///     The old area, is this scripts area.
        ///     By default this method does nothing.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void RenderExitArea(ICreature creature)
        {
            if (creature is ICharacter)
            {
                var character = creature as ICharacter;
                if (character.HasScript<CrucibleScript>())
                {
                    character.TryRemoveScript<CrucibleScript>();
                    character.Appearance.Refresh(); // Reset the combat level range and base combat level.
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