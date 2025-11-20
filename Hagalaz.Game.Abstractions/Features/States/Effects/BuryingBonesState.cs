using System;

namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    [StateMetaData("burying-bones-state")]
    public class BuryingBonesState : State
    {
        public Action OnRemovedCallback { get; set; }
    }
}
