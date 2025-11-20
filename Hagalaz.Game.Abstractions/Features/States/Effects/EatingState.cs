using Hagalaz.Game.Abstractions.Features.States;
using System;

namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    [StateMetaData("eating-state")]
    public class EatingState : State
    {
        public Action OnRemovedCallback { get; set; }
    }
}
