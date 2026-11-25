using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Events.Character;

namespace Hagalaz.Game.Scripts.Equipment.Degraded
{
    /// <summary>
    ///     An task that when fired, will degrade the item to dust.
    /// </summary>
    public class DegradeTask : RsTask
    {
        /// <summary>
        ///     The character.
        /// </summary>
        private readonly ICharacter _character;

        /// <summary>
        ///     The item.
        /// </summary>
        private readonly IItem _item;

        /// <summary>
        ///     The equipment changed handler.
        /// </summary>
        private readonly EventHappened _equipChangedHandler;

        /// <summary>
        ///     The pre-snapshot detach handler.
        /// </summary>
        private readonly EventHappened _detachHandler;

        /// <summary>
        ///     The destroy handler.
        /// </summary>
        private readonly EventHappened _destroyHandler;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DegradeTask" /> class.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="item">The item.</param>
        /// <param name="executeDelay">The executing tick.</param>
        public DegradeTask(ICharacter character, IItem item, int executeDelay)
        {
            _character = character;
            _item = item;
            ExecuteDelay = executeDelay;
            ExecuteHandler = DegradeToDust;

            _equipChangedHandler = _character.RegisterEventHandler(new EventHappened<EquipmentChangedEvent>(e =>
            {
                if (_character.Equipment.GetInstanceSlot(_item) == EquipmentSlot.NoSlot)
                {
                    SaveAndCancel();
                }
                return false;
            }));

            _detachHandler = _character.RegisterEventHandler(new EventHappened<CharacterDetachingEvent>(e =>
            {
                Save();
                return false;
            }));

            _destroyHandler = _character.RegisterEventHandler(new EventHappened<CreatureDestroyedEvent>(e =>
            {
                UnregisterEventHandlers();
                Cancel();
                return false;
            }));
        }

        /// <summary>
        ///     Stores the remaining ticks on the tracked item.
        /// </summary>
        private void Save() => _item.ExtraData[0] = ExecuteDelay;

        /// <summary>
        ///     Stores the remaining ticks and stops the countdown.
        /// </summary>
        private void SaveAndCancel()
        {
            Save();
            UnregisterEventHandlers();
            Cancel();
        }

        /// <summary>
        ///     Unregisters the event handlers.
        /// </summary>
        private void UnregisterEventHandlers()
        {
            _character.UnregisterEventHandler<EquipmentChangedEvent>(_equipChangedHandler);
            _character.UnregisterEventHandler<CharacterDetachingEvent>(_detachHandler);
            _character.UnregisterEventHandler<CreatureDestroyedEvent>(_destroyHandler);
        }

        /// <summary>
        ///     Degrades the item to dust.
        /// </summary>
        private void DegradeToDust()
        {
            UnregisterEventHandlers();
            var slot = _character.Equipment.GetInstanceSlot(_item);
            if (slot == EquipmentSlot.NoSlot)
            {
                return;
            }

            if (_character.Equipment.RemoveEquippedItem(_item, slot) <= 0) return;
            _character.SendChatMessage("Your " + _item.Name + " crumbles into dust.");
        }
    }
}
