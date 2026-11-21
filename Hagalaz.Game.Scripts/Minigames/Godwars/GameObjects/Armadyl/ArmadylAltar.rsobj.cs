using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects.Armadyl
{
    [GameObjectScriptMetaData([26288])]
    public class ArmadylAltar : GodwarsAltarScript<ArmadylAltarPrayedState>
    {
        protected override (int X, int Y) TeleportDestination => (2835, 5294);
    }
}
