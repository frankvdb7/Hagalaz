using System;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class RunCS2ScriptMessage : RaidoMessage
    {
        public required int Id { get; init; }
        public required object[] Parameters { get; init; } = Array.Empty<object>();
    }
}
