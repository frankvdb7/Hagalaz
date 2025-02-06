using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Minigames.DuelArena
{
    [AreaScriptMetaData([21, 22, 23, 24, 25, 26])]
    public class DuelArenaPit : DuelArena
    {
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
            character.SendChatMessage("You can not teleport during a duel!");
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
            character.SendChatMessage("You can not teleport during a duel!");
            return false;
        }

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
            character.SendChatMessage("You can not teleport during a duel!");
            return false;
        }

        /// <summary>
        ///     Happens when character enters this area.
        ///     The new area, is this scripts area.
        ///     By default this method performs some checks on wether some things are allowed in the area. (Familiars)
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void OnCreatureEnterArea(ICreature creature)
        {
        }

        /// <summary>
        ///     Happens when character exits this area.
        ///     The old area, is this scripts area.
        ///     By default this method does nothing.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void OnCreatureExitArea(ICreature creature)
        {
            if (creature is not ICharacter character)
            {
                return;
            }

            if (character.HasScript<DuelArenaCombatScript>())
            {
                character.TryRemoveScript<DuelArenaCombatScript>();
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
                character.RegisterCharactersOptionHandler(CharacterClickType.Option1Click, "Fight", 65535, true, (target, forceRun) =>
                {
                    character.Interrupt(this);
                    character.ForceRunMovementType(forceRun);
                    character.FaceLocation(target);
                    character.Combat.SetTarget(target);
                });
            }
        }

        /// <summary>
        ///     Renders the exit area.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void RenderExitArea(ICreature creature)
        {
            if (creature is ICharacter character)
            {
                character.UnregisterCharactersOptionHandler(CharacterClickType.Option2Click);
            }
        }

        /// <summary>
        ///     Get's respawn location of specific character in this area.
        ///     By default this method does return World.SpawnPoint.
        /// </summary>
        /// <param name="character">Character which needs respawning.</param>
        /// <returns>
        ///     Location.
        /// </returns>
        public override ILocation GetRespawnLocation(ICharacter character) => DuelData.GetDuelLobbyLocation(character.ServiceProvider.GetRequiredService<IMapRegionService>());

        /// <summary>
        ///     Initializes area script.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}