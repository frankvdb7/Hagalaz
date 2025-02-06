using Hagalaz.Game.Messages.Protocol.Map;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class DrawProjectileMessageEncoder : IRaidoMessageEncoder<DrawProjectileMessage>
    {
        public void EncodeMessage(DrawProjectileMessage message, IRaidoMessageBinaryWriter output)
        {
            // 'advanced' projectile = opcode 141 (21 bytes)
            // 'simple' projectile = opcode 127 (18 bytes)
            output
                .SetOpcode(141)
                .SetSize(RaidoMessageSize.Fixed)
                .WriteByte((byte)(((message.FromLocation.X % 16) << 4) & 0xF | (message.FromLocation.Y % 16) & 0xF));

            var settings = 0;
            settings |= message.AdjustFromFlyingHeight ? 0x1 : 0;
            settings |= message.AdjustToFlyingHeight ? 0x2 : 0;
            settings |= message.FromBodyPartId << 2;

            output
                .WriteByte((byte)settings)
                .WriteByte((byte)(message.ToLocation.X - message.FromLocation.X))
                .WriteByte((byte)(message.ToLocation.Y - message.FromLocation.Y))
                .WriteInt16BigEndian((short)(!message.FromIndex.HasValue ? 0 : message.FromIsCharacter ? -(message.FromIndex + 1) : (message.FromIndex + 1)))
                .WriteInt16BigEndian((short)(!message.ToIndex.HasValue ? 0 : message.ToIsCharacter ? -(message.ToIndex + 1) : (message.ToIndex + 1)))
                .WriteInt16BigEndian((short)message.GraphicId)
                .WriteByte((byte)message.FromHeight)
                .WriteByte((byte)message.ToHeight)
                .WriteInt16BigEndian((short)message.Delay) // delay
                .WriteInt16BigEndian((short)(message.Duration + message.Delay)) // duration
                .WriteByte((byte)message.Slope)
                .WriteInt16BigEndian((short)message.Angle)
                .WriteInt16BigEndian((short)(message.ToIndex ?? 0)); // something to do with sound volume, probably calculating the distance / volume ratio
        }
    }
}
