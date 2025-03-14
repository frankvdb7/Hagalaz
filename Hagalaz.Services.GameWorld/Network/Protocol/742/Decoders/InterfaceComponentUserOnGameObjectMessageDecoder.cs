﻿using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class InterfaceComponentUserOnGameObjectMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadLittleEndian(out int interfaceHash))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadBigEndian(out int gameObjectId))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadLittleEndian(out short extraData1))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadLittleEndianA(out short absY))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadS(out bool forceRun))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadBigEndian(out short extraData2))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadLittleEndian(out short absX))
            {
                message = default;
                return false;
            }
            message = new InterfaceComponentUseOnGameObjectMessage
            {
                InterfaceId = interfaceHash >> 16,
                ComponentId = interfaceHash & 0xFFFF,
                ExtraData1 = extraData1,
                ExtraData2 = extraData2,
                ForceRun = forceRun,
                AbsX = absX,
                AbsY = absY,
                GameObjectId = gameObjectId
            };
            return true;
        }
    }
}
