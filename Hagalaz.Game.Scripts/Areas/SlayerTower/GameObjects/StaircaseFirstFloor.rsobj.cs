using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.SlayerTower.GameObjects
{
    [GameObjectScriptMetaData([4493, 4494])]
    public class StaircaseFirstFloor : ClimbObjectScript
    {
        protected override (int X, int Y) ClimbDownShift => (5, 0);
    }
}
