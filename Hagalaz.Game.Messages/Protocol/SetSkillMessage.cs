using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetSkillMessage : RaidoMessage
    {
        public required int Id { get; init; }
        public required int Level { get; init; }
        public required int Experience { get; init; }
    }
}
