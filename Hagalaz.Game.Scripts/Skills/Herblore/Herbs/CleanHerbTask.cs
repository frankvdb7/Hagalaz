using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Herbs
{
    /// <summary>
    /// </summary>
    public class CleanHerbTask : RsTickTask
    {
        /// <summary>
        ///     Construct's new cooking task.
        /// </summary>
        /// <param name="performer">The performer.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="totalCleanCount">The total clean count.</param>
        /// <param name="tickDelay">The tick delay.</param>
        public CleanHerbTask(ICharacter performer, HerbDto definition, int totalCleanCount, int tickDelay)
        {
            Performer = performer;
            Definition = definition;
            TickDelay = tickDelay;
            TotalCleanCount = totalCleanCount;

            TickActionMethod = PerformTickImpl;
            _interruptEvent = performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
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
        private HerbDto Definition { get; }

        /// <summary>
        ///     The times performed.
        /// </summary>
        private int CleanCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        private int TotalCleanCount { get; }

        /// <summary>
        ///     Gets the tick delay.
        /// </summary>
        private int TickDelay { get; }

        /// <summary>
        ///     Performs the animation.
        /// </summary>
        private static void PerformAnimation()
        {
        }

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
                Performer.Inventory.Replace(slot, new Item(Definition.CleanHerbId));
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