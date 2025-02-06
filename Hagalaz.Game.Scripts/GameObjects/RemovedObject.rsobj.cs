using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.GameObjects
{
    [GameObjectScriptMetaData([1596, 1597, 31825, 31827, 31841, 31844, 38453, 38447, 55349, 77085, 77086, 77098])]
    public class RemovedObject : GameObjectScript
    {
        /// <summary>
        ///     Happens when object is spawned.
        /// </summary>
        public override void OnSpawn() => Owner.Region.Remove(Owner);

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize() { }
    }
}