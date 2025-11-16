using System;

namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    public class BuryingBonesState : State
    {
        public Action OnRemovedCallback { get; set; }
    }
}
