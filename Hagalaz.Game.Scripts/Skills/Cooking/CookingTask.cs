using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Skills.Cooking
{
    /// <summary>
    /// </summary>
    public class CookingTask : RsTickTask
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened _interruptEvent;

        public CookingTask(ICharacterContextAccessor characterContextAccessor, IItemService itemService, IItemBuilder itemBuilder)
        {
            Performer = characterContextAccessor.Context.Character;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = Performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
            _itemService = itemService;
            _itemBuilder = itemBuilder;
        }

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private ICharacter Performer { get; }

        /// <summary>
        ///     Gets the obj.
        /// </summary>
        /// <value>
        ///     The obj.
        /// </value>
        public IGameObject GameObject { get; set; } = null!;

        /// <summary>
        ///     The definition.
        /// </summary>
        public RawFoodDto RawDto { get; set; } = null!;

        /// <summary>
        ///     The times performed.
        /// </summary>
        private int CookCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        public int TotalCookCount { get; set; }

        /// <summary>
        ///     The item manager
        /// </summary>
        private readonly IItemService _itemService;

        private readonly IItemBuilder _itemBuilder;

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            if (GameObject.IsDestroyed)
            {
                Cancel();
                return;
            }

            if (CookCount == TotalCookCount)
            {
                Cancel();
                return;
            }

            if (TickCount == 1 || TickCount % 6 == 0)
            {
                Performer.QueueAnimation(Animation.Create(897));
                Performer.SendChatMessage("You attempt to cook the " + _itemService.FindItemDefinitionById(RawDto.ItemId).Name.ToLower() + ".");
                return;
            }

            if (TickCount % 3 == 0)
            {
                CookCount++;
                var rawItem = Performer.Inventory.GetById(RawDto.ItemId);
                if (rawItem == null)
                {
                    Cancel();
                    return;
                }

                var slot = Performer.Inventory.GetInstanceSlot(rawItem);
                if (slot == -1)
                {
                    Cancel();
                    return;
                }

                var burned = IsBurned();
                Performer.SendChatMessage(burned
                    ? "Oops! You accidentally burnt the " + rawItem.Name.ToLower()
                    : "You successfully cook the " + rawItem.Name.ToLower());
                Performer.Inventory.Replace(slot, _itemBuilder.Create().WithId(burned ? RawDto.BurntItemId : RawDto.CookedItemId).Build());
                if (!burned)
                {
                    Performer.Statistics.AddExperience(StatisticsConstants.Cooking, RawDto.Experience); // Stop cooking if leveled up.
                }
            }
        }

        /// <summary>
        ///     Determines whether the item is burned.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the item is burned; otherwise, <c>false</c>.
        /// </returns>
        private bool IsBurned()
        {
            var level = Performer.Statistics.GetSkillLevel(StatisticsConstants.Cooking);
            if (level >= RawDto.StopBurningLevel)
            {
                return false;
            }

            var gloves = Performer.Equipment[EquipmentSlot.Hands];
            if (gloves?.Id == 775)
            {
                if (level >= RawDto.StopBurningLevel - (RawDto.CookedItemId == 391 ? 0 : 6))
                {
                    return false;
                }
            }

            var levelsToStopBurn = RawDto.StopBurningLevel - level;
            if (levelsToStopBurn > 20)
            {
                levelsToStopBurn = 20;
            }

            return RandomStatic.Generator.Next(45) <= levelsToStopBurn;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Cancel()
        {
            base.Cancel();
            Performer.UnregisterEventHandler<CreatureInterruptedEvent>(_interruptEvent);
        }
    }
}