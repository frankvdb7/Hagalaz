using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.AncientCaverns.GameObjects
{
    [GameObjectScriptMetaData([25337, 39468])]
    public class StepsTwo : ClimbObjectScript
    {
        protected override (int X, int Y) ClimbDownShift => (0, 4);
    }
}
