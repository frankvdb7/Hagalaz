using Hagalaz.Game.Abstractions.Collections;
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
        ///     The unEquipHandler.
        /// </summary>
        private readonly EventHappened _equipChangedHandler;

        /// <summary>
        ///     The logout handler.
        /// </summary>
        private readonly EventHappened _logoutHandler;

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
                if (e.ChangedSlots == null)
                {
                    return false;
                }
                foreach (var slot in e.ChangedSlots)
                {
                    var changedItem = _character.Equipment[slot];
                    if (changedItem == null || !_item.Equals(changedItem))
                    {
                        continue;
                    }

                    Save(_item);
                    return false;
                }
                return false;
            }));

            _logoutHandler = _character.RegisterEventHandler(new EventHappened<CreatureDestroyedEvent>(e =>
            {
                Save(_item);
                return false;
            }));
        }

        /// <summary>
        ///     Saves the specified item.
        /// </summary>
        /// <param name="toSave">To save.</param>
        private void Save(IItem toSave)
        {
            toSave.ExtraData[0] = ExecuteDelay;
            UnregisterEventHandlers();
            _character.QueueTask(new RsTask(Cancel, 1));
        }

        /// <summary>
        ///     Unregisters the event handlers.
        /// </summary>
        private void UnregisterEventHandlers()
        {
            if (_equipChangedHandler != null)
            {
                _character.UnregisterEventHandler<EquipmentChangedEvent>(_equipChangedHandler);
            }

            if (_logoutHandler != null)
            {
                _character.UnregisterEventHandler<CreatureDestroyedEvent>(_logoutHandler);
            }
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

            _character.SendChatMessage("Your " + _item.Name + " crumbles into dust.");
            _character.Equipment.Remove(_item, slot);
        }
    }
}