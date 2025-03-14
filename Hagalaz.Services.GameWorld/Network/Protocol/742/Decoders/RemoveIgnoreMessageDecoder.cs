﻿using Raido.Common.Protocol;
using Raido.Server.Extensions;
using System.Buffers;
using Hagalaz.Game.Messages.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class RemoveIgnoreMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryRead(out string displayName))
            {
                message = default;
                return false;
            }
            message = new RemoveIgnoreMessage
            {
                DisplayName = displayName
            };
            return true;
        }
    }
}
