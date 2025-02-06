using System;
using System.Collections.Generic;
using Hagalaz.Game.Messages.Protocol.Map;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class MapRegionPartUpdateMessageEncoder : IRaidoMessageEncoder<MapRegionPartUpdateMessage>
    {
        private readonly IRaidoCodec<ClientProtocol742> _raidoCodec;
        private static readonly IDictionary<Type, int> _messageIndexMap = new Dictionary<Type, int>()
        {
            { typeof(RemoveGameObjectMessage), 1 },
            { typeof(AddGroundItemMessage), 2 },
            { typeof(SetGameObjectAnimationMessage), 4 },
            { typeof(AddGameObjectMessage), 6 },
            { typeof(DrawProjectileMessage), 7 },
            { typeof(RemoveGroundItemMessage), 9 },
            { typeof(DrawGraphicMessage), 12 },
            { typeof(DrawTileStringMessage), 13 },
            { typeof(SetGroundItemCountMessage), 14 },
        };

        public MapRegionPartUpdateMessageEncoder(IRaidoCodec<ClientProtocol742> raidoCodec)
        {
            _raidoCodec = raidoCodec;
        }

        public void EncodeMessage(MapRegionPartUpdateMessage group, IRaidoMessageBinaryWriter output)
        {
            if (group.Messages == null)
            {
                if (group.FullUpdate)
                {
                    // zone update full follows
                    output
                        .SetOpcode(62)
                        .SetSize(RaidoMessageSize.Fixed)
                        .WriteByteC((byte)group.Z)
                        .WriteByteA((byte)(group.LocalY >> 3))
                        .WriteByteC((byte)(group.LocalX >> 3));
                }
                else
                {
                    // zone update partial follows
                    output
                        .SetOpcode(6)
                        .SetSize(RaidoMessageSize.Fixed)
                        .WriteByte((byte)group.Z)
                        .WriteByteC((byte)(group.LocalY >> 3))
                        .WriteByteC((byte)(group.LocalX >> 3));
                }
            }
            else
            {
                // zone update partial enclosed
                output
                    .SetOpcode(162)
                    .SetSize(RaidoMessageSize.Fixed)
                    .WriteByteC((byte)(group.LocalX >> 3))
                    .WriteByteC((byte)group.Z)
                    .WriteByteS((byte)(group.LocalY >> 3));

                foreach (var message in group.Messages)
                {
                    if (!_messageIndexMap.TryGetValue(message.GetType(), out var index))
                    {
                        throw new NotImplementedException($"MapRegionUpdateMessage type '{message.GetType().Name}' is not mapped.");
                    }
                    output.WriteByte((byte)index);
                    _raidoCodec.TryEncodeMessage(message, output);
                }
            }
        }
    }
}
