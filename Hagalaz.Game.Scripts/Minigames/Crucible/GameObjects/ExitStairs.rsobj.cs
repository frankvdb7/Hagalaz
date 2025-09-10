using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Crucible.GameObjects
{
    /// <summary>
    /// </summary>
    /// <seealso cref="BaseTeleportObjectScript" />
    [GameObjectScriptMetaData([67053])]
    public class ExitStairs : BaseTeleportObjectScript
    {
        /// <summary>
        ///     Gets the destination.
        /// </summary>
        /// <value>
        ///     The destination.
        /// </value>
        protected override ILocation Destination => Location.Create(3120, 3519, 0);

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}