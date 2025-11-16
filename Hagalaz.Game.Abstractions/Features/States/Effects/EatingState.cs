using System;

namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    public class EatingState : State
    {
        public Action OnRemovedCallback { get; set; }
    }
}
