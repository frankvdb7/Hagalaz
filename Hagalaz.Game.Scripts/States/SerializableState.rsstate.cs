using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Scripts.Model.States;

namespace Hagalaz.Game.Scripts.States
{
    /// <summary>
    /// </summary>
    [StateScriptMetaData([
        StateType.ResistPoison, StateType.ResistFreeze, StateType.ArmadylAltarPrayed, StateType.BandosAltarPrayed, StateType.SaradominAltarPrayed,
        StateType.ZamorakAltarPrayed, StateType.HasGodWarsHoleRope, StateType.HasSaradominFirstRockRope, StateType.HasSaradominLastRockRope,
        StateType.RecoverSpecialPotion, StateType.CantCastVengeance
    ])]
    public class SerializableState : StateScriptBase
    {
        /// <summary>
        ///     Determines whether this instance is serializable.
        /// </summary>
        /// <returns></returns>
        public override bool IsSerializable() => true;
    }
}