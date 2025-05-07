using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Characters
{
    /// <summary>
    ///     Character following script.
    /// </summary>
    public class FollowingCharacterScript : CharacterScriptBase, IDefaultCharacterScript
    {
        /// <summary>
        ///     Contains current follow target.
        /// </summary>
        private ICharacter? _followTarget;

        /// <summary>
        ///     The path finder
        /// </summary>
        private readonly IPathFinder _pathFinder;

        public FollowingCharacterScript(ICharacterContextAccessor contextAccessor, ISmartPathFinder pathFinder)
            : base(contextAccessor) =>
            _pathFinder = pathFinder;

        /// <summary>
        ///     Happens when script instance is initialized.
        /// </summary>
        protected override void Initialize() {}

        /// <summary>
        ///     Happens when character enter's world.
        /// </summary>
        public override void OnRegistered() =>
            Character.RegisterCharactersOptionHandler(CharacterClickType.Option3Click, "Follow", 65535, false, (target, forceRun) =>
            {
                Character.Interrupt(this);
                if (!Character.Location.WithinDistance(target.Location, CreatureConstants.VisibilityDistance))
                {
                    return;
                }

                _followTarget = target;
                Character.ForceRunMovementType(forceRun);
                Character.FaceCreature(target);
            });

        /// <summary>
        ///     Get's called when character is interrupted.
        ///     By default this method does nothing.
        /// </summary>
        /// <param name="source">
        ///     Object which performed the interruption,
        ///     this parameter can be null , but it is not encouraged to do so.
        ///     Best use would be to set the invoker class instance as source.
        /// </param>
        public override void OnInterrupt(object source)
        {
            if (source == this)
            {
                return;
            }

            base.OnInterrupt(source);
            _followTarget = null;
            Character.ResetFacing();
        }

        /// <summary>
        ///     Tick's character.
        ///     By default, this method does nothing.
        /// </summary>
        public override void Tick()
        {
            if (_followTarget == null)
            {
                return;
            }
            if (_followTarget.IsDestroyed || !Character.Viewport.VisibleCreatures.Contains(_followTarget) || !Character.Location.WithinDistance(_followTarget.Location, CreatureConstants.VisibilityDistance))
            {
                Character.ResetFacing();
                _followTarget = null;
                return;
            }
            var path = _pathFinder.Find(Character, _followTarget, true);
            if (!path.Successful && !path.MovedNear || path.MovedNearDestination)
            {
                Character.ResetFacing();
                _followTarget = null;
                return;
            }

            Character.Movement.AddToQueue(path);
        }
    }
}