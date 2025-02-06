using Hagalaz.Game.Abstractions.Builders.Region;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Skills.Prayer
{
    /// <summary>
    /// </summary>
    public class OfferTask : RsTickTask
    {
        /// <summary>
        ///     Construct's new cooking task.
        /// </summary>
        /// <param name="performer">The performer.</param>
        /// <param name="altar">The altar.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="totalOfferCount">The total clean count.</param>
        /// <param name="tickDelay">The tick delay.</param>
        public OfferTask(ICharacter performer, IGameObject altar, PrayerDto definition, int totalOfferCount, int tickDelay)
        {
            Performer = performer;
            TickDelay = tickDelay;
            Altar = altar;
            TotalOfferCount = totalOfferCount;
            Definition = definition;
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
        ///     Contains the altar.
        /// </summary>
        private IGameObject Altar { get; }

        /// <summary>
        ///     Gets the definition.
        /// </summary>
        /// <value>
        ///     The definition.
        /// </value>
        private PrayerDto Definition { get; }

        /// <summary>
        ///     The times performed.
        /// </summary>
        private int OfferCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        private int TotalOfferCount { get; }

        /// <summary>
        ///     Gets the tick delay.
        /// </summary>
        private int TickDelay { get; }

        /// <summary>
        ///     Performs the animation.
        /// </summary>
        private void PerformAnimation()
        {
            Performer.QueueAnimation(Animation.Create(713));
            var regionUpdateBuilder = Performer.ServiceProvider.GetRequiredService<IRegionUpdateBuilder>();
            var update = regionUpdateBuilder.Create().WithLocation(Altar.Location).WithGraphic(Graphic.Create(624)).Build();
            Altar.Region.QueueUpdate(update);
        }

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            if (OfferCount == TotalOfferCount)
            {
                Cancel();
                return;
            }

            if (TickCount % TickDelay == 0)
            {
                PerformAnimation();
                OfferCount++;

                var item = Performer.Inventory.GetById(Definition.ItemId);
                if (item == null)
                {
                    Performer.SendChatMessage("You do not have any offerings left.");
                    Cancel();
                    return;
                }

                var slot = Performer.Inventory.GetInstanceSlot(item);
                if (slot == -1)
                {
                    return;
                }

                var removed = Performer.Inventory.Remove(item, slot);
                if (removed <= 0)
                {
                    return;
                }

                Performer.SendChatMessage("The gods are very pleased with your offering.");
                Performer.Statistics.AddExperience(StatisticsConstants.Prayer, Definition.Experience * 3.5);
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