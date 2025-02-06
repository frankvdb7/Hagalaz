using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Runecrafting
{
    /// <summary>
    /// </summary>
    public class Portal : GameObjectScript
    {
        private readonly IRunecraftingService _runecraftingService;

        public Portal(IRunecraftingService runecraftingService)
        {
            _runecraftingService = runecraftingService;
        }

        /// <summary>
        ///     Happens when character click's this object and then walks to it
        ///     and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacterClick is overrided
        ///     than this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                clicker.QueueTask(() => TeleportToRuin(clicker));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        private async Task TeleportToRuin(ICharacter character)
        {
            var altar = await _runecraftingService.FindAltarByPortalId(Owner.Id);
            if (altar == null)
            {
                return;
            }
            character.Movement.Teleport(Teleport.Create(altar.RuinLocation.Clone()));
            character.SendChatMessage("You step through the portal.");
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}