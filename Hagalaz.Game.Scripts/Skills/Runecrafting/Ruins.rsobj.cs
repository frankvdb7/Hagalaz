using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Runecrafting
{
    /// <summary>
    /// </summary>
    public class Ruins : GameObjectScript
    {
        private readonly IRunecraftingService _runecraftingService;

        public Ruins(IRunecraftingService runecraftingService) => _runecraftingService = runecraftingService;

        /// <summary>
        ///     Uses the item on game object.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public override bool UseItemOnGameObject(IItem used, ICharacter character)
        {
            character.QueueTask(() => TeleportCharacter(character));
            return true;
        }

        private async Task TeleportCharacter(ICharacter character)
        {
            var altar = await _runecraftingService.FindAltarByRuinId(Owner.Id);
            if (altar == null)
            {
                return;
            }
            character.SendChatMessage("You hold the talisman towards the mysterious ruins.");
            character.QueueTask(new RsTask(() =>
            {
                character.SendChatMessage("You feel a powerful force take hold of you.");
                character.Movement.Teleport(Teleport.Create(altar.AltarLocation.Clone()));
            }, 1));
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}