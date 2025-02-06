using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Builders.Projectile
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IProjectileTo
    {
        /// <summary>
        /// Sets the to creature location.
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        IProjectileDuration ToCreature(ICreature creature);
        /// <summary>
        /// Sets the to game object location.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        IProjectileDuration ToGameObject(IGameObject gameObject);
        /// <summary>
        /// Sets the to ground item location.
        /// </summary>
        /// <param name="groundItem"></param>
        /// <returns></returns>
        IProjectileDuration ToGroundItem(IGroundItem groundItem);
        /// <summary>
        /// Sets the to location.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        IProjectileDuration ToLocation(ILocation location);
    }
}