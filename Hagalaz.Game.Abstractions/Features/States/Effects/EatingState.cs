using System;

namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    public class EatingState : ScriptedState
    {
        public Action OnRemovedCallback { get; set; }
    }
}
