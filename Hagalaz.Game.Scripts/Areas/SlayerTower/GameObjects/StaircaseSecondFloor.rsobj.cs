using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.SlayerTower.GameObjects
{
    [GameObjectScriptMetaData([4495, 4496])]
    public class StaircaseSecondFloor : ClimbObjectScript
    {
        protected override (int X, int Y) ClimbDownShift => (-5, 0);
    }
}
