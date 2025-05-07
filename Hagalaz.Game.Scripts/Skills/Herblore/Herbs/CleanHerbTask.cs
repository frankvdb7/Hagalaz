using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Herbs
{
    /// <summary>
    /// </summary>
    public class CleanHerbTask : RsTickTask
    {
        private readonly IItemBuilder _itemBuilder;

        public CleanHerbTask(ICharacterContextAccessor characterContextAccessor, IItemBuilder itemBuilder)
        {
            _itemBuilder = itemBuilder;
            Performer = characterContextAccessor.Context.Character;
            ;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = Performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
        }

        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened? _interruptEvent;

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private ICharacter Performer { get; }

        /// <summary>
        ///     The definition.
        /// </summary>
        public HerbDto Definition { get; set; } = null!;

        /// <summary>
        ///     The times performed.
        /// </summary>
        private int CleanCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        public int TotalCleanCount { get; set; }

        /// <summary>
        ///     Gets the tick delay.
        /// </summary>
        public int TickDelay { get; set; }

        /// <summary>
        ///     Performs the animation.
        /// </summary>
        private static void PerformAnimation() { }

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            if (CleanCount == TotalCleanCount)
            {
                Cancel();
                return;
            }

            if (TickCount == 1)
            {
                PerformAnimation();
            }

            if (TickCount % TickDelay == 0)
            {
                CleanCount++;
                var item = Performer.Inventory.GetById(Definition.GrimyHerbId);
                if (item == null)
                {
                    Performer.SendChatMessage("You do not have any grimy herbs left.");
                    Cancel();
                    return;
                }

                var slot = Performer.Inventory.GetInstanceSlot(item);
                if (slot == -1)
                {
                    return;
                }

                Performer.SendChatMessage("You clean the drift from the " + item.Name.ToLower());
                Performer.Inventory.Replace(slot, _itemBuilder.Create().WithId(Definition.CleanHerbId).Build());
                Performer.Statistics.AddExperience(StatisticsConstants.Herblore, Definition.CleanExperience);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Cancel()
        {
            base.Cancel();
            Performer.UnregisterEventHandler<CreatureInterruptedEvent>(_interruptEvent!);
        }
    }
}