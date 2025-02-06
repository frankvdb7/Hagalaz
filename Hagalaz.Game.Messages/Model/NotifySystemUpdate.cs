using System;

namespace Hagalaz.Game.Messages.Model
{
    public class NotifySystemUpdate
    {
        public DateTimeOffset ScheduledUpdateTime { get; init; }
    }
}