using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.AncientCaverns.GameObjects
{
    [GameObjectScriptMetaData([25336, 25338])]
    public class Steps : ClimbObjectScript
    {
        protected override (int X, int Y) ClimbDownShift => (4, 0);
    }
}
