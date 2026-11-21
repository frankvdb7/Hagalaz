using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Scripts.Model.GameObjects
{
    /// <summary>
    ///     A ladder, stair or step object: clicking it climbs the character one plane down (when the object offers
    ///     "Climb-down") or up, after a short animation, and optionally shifts the character horizontally.
    /// </summary>
    public abstract class ClimbObjectScript : GameObjectScript
    {
        /// <summary>
        ///     Horizontal shift applied when climbing down; climbing up applies the opposite shift. No shift by default.
        /// </summary>
        protected virtual (int X, int Y) ClimbDownShift => (0, 0);

        /// <inheritdoc />
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType != GameObjectClickType.Option1Click)
            {
                base.OnCharacterClickPerform(clicker, clickType);
                return;
            }

            clicker.Interrupt(this);
            clicker.Movement.Lock(true);

            var climbingDown = Owner.Definition.HasAction("Climb-down");
            var (shiftX, shiftY) = ClimbDownShift;
            var direction = climbingDown ? 1 : -1;
            var planeDelta = -direction;

            clicker.QueueAnimation(Animation.Create(climbingDown ? 827 : 828));
            clicker.QueueTask(new RsTask(() =>
            {
                var location = clicker.Location;
                clicker.Movement.Teleport(Teleport.Create(Location.Create(
                    (short)(location.X + shiftX * direction),
                    (short)(location.Y + shiftY * direction),
                    (byte)(location.Z + planeDelta),
                    location.Dimension)));
                clicker.Movement.Unlock(false);
            }, 2));
        }
    }
}
