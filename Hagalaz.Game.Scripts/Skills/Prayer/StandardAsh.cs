using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Prayer
{
    /// <summary>
    ///     Contains ash script.
    /// </summary>
    public class StandardAsh : ItemScript
    {
        private readonly IPrayerService _prayerService;

        public StandardAsh(IPrayerService prayerService)
        {
            _prayerService = prayerService;
        }

        /// <summary>
        ///     Uses the item on game object.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedOn">The used on.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public override bool UseItemOnGameObject(IItem used, IGameObject usedOn, ICharacter character) => usedOn.Id == 13197 ? Prayer.UseOnAltar(character, used, usedOn, _prayerService) : base.UseItemOnGameObject(used, usedOn, character);


        /// <summary>
        ///     Items the clicked in inventory.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType == ComponentClickType.LeftClick)
            {
                character.QueueTask(() => Scatter(character, item));
            }
            else
            {
                base.ItemClickedInInventory(clickType, item, character);
            }
        }

        /// <summary>
        ///     Buries the specified item.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="item">The item.</param>
        private async Task Scatter(ICharacter character, IItem item)
        {
            if (character.HasState(StateType.BuryingBones))
            {
                return;
            }

            character.Interrupt(this);
            var definition = await _prayerService.FindById(item.Id);
            if (definition == null)
            {
                return;
            }

            var slot = character.Inventory.GetInstanceSlot(item);
            if (slot == -1)
            {
                return;
            }

            character.QueueAnimation(Animation.Create(827)); //TODO - Find scatter anim and graphic
            character.AddState(new State(StateType.BuryingBones, 2, () => OnRemovedCallBack(character, item, definition, slot)));
        }

        /// <summary>
        ///     Called when [removed call back].
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="toRemove">To remove.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="slot">The slot.</param>
        private static void OnRemovedCallBack(ICharacter character, IItem toRemove, PrayerDto definition, int slot)
        {
            var removed = character.Inventory.Remove(toRemove, slot);
            if (removed > 0)
            {
                character.SendChatMessage("You scatter the ashes.");
                character.Statistics.AddExperience(StatisticsConstants.Prayer, definition.Experience);
            }
        }
    }
}