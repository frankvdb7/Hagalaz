using System;
using Hagalaz.Game.Abstractions.Features.States;

namespace Hagalaz.Game.Abstractions.Model
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class StateScriptMetaData(StateType[] stateTypes) : Attribute
    {
        public StateType[] StateTypes { get; } = stateTypes;
    }
}