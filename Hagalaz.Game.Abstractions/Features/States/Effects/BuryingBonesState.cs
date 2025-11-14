using System;

namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    public class BuryingBonesState : ScriptedState
    {
        public Action OnRemovedCallback { get; set; }
    }
}
