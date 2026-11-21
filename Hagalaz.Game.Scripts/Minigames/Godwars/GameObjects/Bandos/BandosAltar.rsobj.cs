using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects.Bandos
{
    [GameObjectScriptMetaData([26289])]
    public class BandosAltar : GodwarsAltarScript<BandosAltarPrayedState>
    {
        protected override (int X, int Y) TeleportDestination => (2828, 5342);
    }
}
