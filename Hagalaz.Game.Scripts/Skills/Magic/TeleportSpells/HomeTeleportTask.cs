using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells
{
    /// <summary>
    ///     Contains the home teleport task.
    /// </summary>
    public class HomeTeleportTask : RsTickTask
    {
        /// <summary>
        ///     Construct's new home teleport task.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="targetLocation">The target location.</param>
        public HomeTeleportTask(ICharacter caster, ILocation targetLocation)
        {
            _caster = caster;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = caster.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                if (TickCount < 18)
                {
                    _caster.Movement.Unlock(false);
                    return false;
                }
                Cancel();
                return false;
            });
            _targetLocation = targetLocation;
        }

        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened? _interruptEvent;

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private readonly ICharacter _caster;

        /// <summary>
        ///     The target location.
        /// </summary>
        private readonly ILocation _targetLocation;

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            if (TickCount == 1)
            {
                _caster.QueueAnimation(Animation.Create(16385));
                _caster.QueueGraphic(Graphic.Create(3017));
            }
            else if (TickCount == 18)
            {
                _caster.QueueAnimation(Animation.Create(16386));
                _caster.QueueGraphic(Graphic.Create(3018));
                _caster.Movement.Teleport(Teleport.Create(_targetLocation.Translate(0, 1, 0)));
            }
            else if (TickCount == 19)
            {
                _caster.FaceLocation(_targetLocation);
            }
            else if (TickCount == 23)
            {
                _caster.QueueAnimation(Animation.Create(16393));
            }
            else if (TickCount == 25)
            {
                _caster.QueueAnimation(Animation.Create(-1));
                _caster.Movement.Teleport(Teleport.Create(_targetLocation));
                _caster.Movement.Unlock(false);
                Cancel();
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public override void Cancel()
        {
            base.Cancel();
            _caster.UnregisterEventHandler<CreatureInterruptedEvent>(_interruptEvent);
        }
    }
}