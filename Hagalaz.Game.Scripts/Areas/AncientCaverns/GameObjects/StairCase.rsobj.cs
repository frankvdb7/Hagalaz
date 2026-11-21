using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.AncientCaverns.GameObjects
{
    [GameObjectScriptMetaData([25339, 25340])]
    public class StairCase : ClimbObjectScript
    {
        protected override (int X, int Y) ClimbDownShift => (0, 3);
    }
}
