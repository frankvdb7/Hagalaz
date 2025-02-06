using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Builders.Projectile
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IProjectileFrom
    {
        /// <summary>
        /// Sets the from creature location.
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        IProjectileTo FromCreature(ICreature creature);
        /// <summary>
        /// Sets the from game object location.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        IProjectileTo FromGameObject(IGameObject gameObject);
        /// <summary>
        /// Sets the from ground item location.
        /// </summary>
        /// <param name="groundItem"></param>
        /// <returns></returns>
        IProjectileTo FromGroundItem(IGroundItem groundItem);
        /// <summary>
        /// Sets the from location.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        IProjectileTo FromLocation(ILocation location);
    }
}