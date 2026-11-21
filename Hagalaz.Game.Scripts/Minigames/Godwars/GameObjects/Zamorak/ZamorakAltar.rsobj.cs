using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects.Zamorak
{
    [GameObjectScriptMetaData([26286])]
    public class ZamorakAltar : GodwarsAltarScript<ZamorakAltarPrayedState>
    {
        protected override (int X, int Y) TeleportDestination => (2925, 5333);
    }
}
