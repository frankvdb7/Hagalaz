using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;
using System.Linq;
using Hagalaz.Collections.Extensions;
using Hagalaz.Game.Messages.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetWorldInfoMessageEncoder : IRaidoMessageEncoder<SetWorldInfoMessage>
    {
        public void EncodeMessage(SetWorldInfoMessage message, IRaidoMessageBinaryWriter output)
        {
            output
                .SetOpcode(114)
                .SetSize(RaidoMessageSize.VariableShort)
                .WriteByte(1) // 0 - buffer only, 1 - parse
                .WriteByte(2); // whether to skip the update

            var minWorldId = message.WorldInfos.DefaultIfEmpty().Min(x => x?.Id) ?? 0;
            var maxWorldId = message.WorldInfos.DefaultIfEmpty().Max(x => x?.Id) ?? 0;

            output.WriteByte((byte)(message.FullUpdate ? 1 : 0));
            if (message.FullUpdate)
            {
                output.WriteInt16BigEndianSmart((short)message.LocationInfos.Count);
                foreach (var location in message.LocationInfos)
                {
                    output
                        .WriteInt16BigEndianSmart((short)location.Flag)
                        .WriteString(location.Name, true);
                }
                output
                    .WriteInt16BigEndianSmart((short)minWorldId) 
                    .WriteInt16BigEndianSmart((short)maxWorldId) 
                    .WriteInt16BigEndianSmart((short)message.WorldInfos.Count);

                foreach (var world in message.WorldInfos)
                {
                    var flags = 0;
                    if (world.Settings.IsMembersOnly)
                    {
                        flags |= 0x1;
                    }

                    if (world.Settings.IsQuickChatEnabled)
                    {
                        flags |= 0x2;
                    }

                    if (world.Settings.IsPvP)
                    {
                        flags |= 0x4;
                    }

                    if (world.Settings.IsLootShareEnabled)
                    {
                        flags |= 0x8;
                    }

                    if (world.Settings.IsHighlighted)
                    {
                        flags |= 0x16;
                    }

                    output
                        .WriteInt16BigEndianSmart((short)(world.Id - minWorldId)) // world index
                        .WriteByte((byte)message.LocationInfos.IndexOf(location => location == world.Location))
                        .WriteInt32BigEndian(flags)
                        .WriteString(world.Name, true)
                        .WriteString(world.IpAddress, true);
                }

                output.WriteInt32BigEndian(message.Checksum);
            }

            foreach (var world in message.CharacterInfos)
            {
                output
                    .WriteInt16BigEndianSmart((short)(world.Id - minWorldId)) // world index
                    .WriteInt16BigEndian((short)(world.Online ? world.CharacterCount : -1));
            }

        }
    }
}
